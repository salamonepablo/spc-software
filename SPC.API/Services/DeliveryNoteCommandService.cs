using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SPC.API.Contracts.DeliveryNotes;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

public sealed class DeliveryNoteCommandService(SPCDbContext db) : IDeliveryNoteCommandService
{
    public async Task<(DeliveryNoteResponse Note, bool IsReplay)> CreateAsync(CreateDeliveryNoteRequest request, string? idempotencyKey = null)
    {
        idempotencyKey = idempotencyKey ?? request.IdempotencyKey ?? string.Empty;
        Validate(request, idempotencyKey);
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        if (request.BranchId <= 0 && request.InvoiceId.HasValue)
            request.BranchId = await db.Invoices.Where(i => i.Id == request.InvoiceId.Value).Select(i => i.BranchId).SingleOrDefaultAsync();
        if (request.BranchId <= 0) throw new InvalidOperationException("La sucursal es obligatoria");
        var fingerprint = Fingerprint(request);

        var existing = await db.DeliveryNotes.Where(n => n.BranchId == request.BranchId && n.IdempotencyKey == idempotencyKey)
            .Select(n => new { n.Id, n.RequestFingerprint }).SingleOrDefaultAsync();
        if (existing != null)
        {
            if (existing.RequestFingerprint != fingerprint) throw new DeliveryNoteIdempotencyConflictException();
            var replay = await LoadResponse(existing.Id);
            if (transaction != null) await transaction.CommitAsync();
            return (replay, true);
        }

        var branch = await db.Branches.SingleOrDefaultAsync(b => b.Id == request.BranchId && b.IsActive)
            ?? throw new InvalidOperationException("La sucursal no existe o está inactiva");
        Invoice? invoice = null;
        if (request.InvoiceId.HasValue)
        {
            invoice = await db.Invoices.Include(i => i.Details).SingleOrDefaultAsync(i => i.Id == request.InvoiceId.Value)
                ?? throw new InvalidOperationException("La factura de origen no existe");
            if (invoice.IsVoided) throw new InvalidOperationException("No se puede crear un remito desde una factura anulada");
            request.CustomerId = invoice.CustomerId;
            if (request.Details.Count != 0) throw new InvalidOperationException("Las líneas se copian íntegramente desde la factura");
            if (invoice.Details.Count == 0) throw new InvalidOperationException("La factura no tiene líneas para copiar");
        }
        if (!await db.Customers.AnyAsync(c => c.Id == request.CustomerId && c.IsActive))
            throw new InvalidOperationException("El cliente no existe o está inactivo");
        if (request.SalesRepId.HasValue && !await db.SalesReps.AnyAsync(s => s.Id == request.SalesRepId && s.IsActive))
            throw new InvalidOperationException("El vendedor no existe o está inactivo");

        var details = invoice != null
            ? invoice.Details.Select(d => new DeliveryNoteDetail { ItemNumber = d.ItemNumber, ProductId = d.ProductId, Quantity = d.Quantity }).ToList()
            : request.Details.Select((d, i) => new DeliveryNoteDetail { ItemNumber = i + 1, ProductId = d.ProductId, Quantity = d.Quantity }).ToList();
        var productIds = details.Select(d => d.ProductId).Distinct().ToList();
        if (details.Any(d => d.Quantity <= 0) || await db.Products.CountAsync(p => productIds.Contains(p.Id) && p.IsActive) != productIds.Count)
            throw new InvalidOperationException("Hay productos inválidos o cantidades no positivas");
        if (details.Count == 0) throw new InvalidOperationException("El remito debe incluir al menos una línea");

        var note = new DeliveryNote {
            BranchId = request.BranchId, PointOfSale = request.PointOfSale ?? branch.PointOfSale,
            DeliveryNoteNumber = await AllocateNumber(request.BranchId, request.RequestedDeliveryNoteNumber), DeliveryNoteDate = request.DeliveryNoteDate.Date,
            CustomerId = request.CustomerId, SalesRepId = invoice?.SalesRepId ?? request.SalesRepId,
            DeliveryAddress = Normalize(request.DeliveryAddress), DeliveryCity = Normalize(request.DeliveryCity),
            BusinessUnit = Normalize(request.BusinessUnit), Clarification = Normalize(request.Clarification), Notes = Normalize(request.Notes),
            InvoiceId = invoice?.Id, InvoiceType = invoice?.InvoiceType, IsInvoiced = invoice != null, Details = details,
            IdempotencyKey = idempotencyKey, RequestFingerprint = fingerprint
        };
        if (invoice == null && request.AdjustStock)
        {
            if (!note.SalesRepId.HasValue) throw new InvalidOperationException("Se requiere vendedor para ajustar stock");
            var warehouseId = await db.Warehouses.Where(w => w.AssociatedSalesRepId == note.SalesRepId && w.IsActive)
                .Select(w => (int?)w.Id).SingleOrDefaultAsync() ?? throw new InvalidOperationException("El vendedor no tiene depósito activo asociado");
            foreach (var line in details.GroupBy(d => d.ProductId).Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) }))
            {
                var stock = await db.Stocks.SingleOrDefaultAsync(s => s.ProductId == line.ProductId && s.WarehouseId == warehouseId)
                    ?? throw new InvalidOperationException($"No existe stock para producto {line.ProductId} en depósito {warehouseId}");
                stock.Quantity -= line.Quantity;
            }
        }
        db.DeliveryNotes.Add(note);
        await db.SaveChangesAsync();
        var response = await LoadResponse(note.Id);
        if (transaction != null) await transaction.CommitAsync();
        return (response, false);
    }

    public async Task<DeliveryNoteNextNumberResponse> GetNextNumberAsync(int branchId)
    {
        if (!await db.Branches.AnyAsync(b => b.Id == branchId && b.IsActive)) throw new InvalidOperationException("La sucursal no existe o está inactiva");
        var current = await db.BranchDeliveryNoteSequences.Where(s => s.BranchId == branchId).Select(s => (long?)s.NextDeliveryNoteNumber).SingleOrDefaultAsync();
        var max = await db.DeliveryNotes.Where(n => n.BranchId == branchId).Select(n => (long?)n.DeliveryNoteNumber).MaxAsync() ?? 0;
        return new() { BranchId = branchId, NextNumber = Math.Max(current ?? 1, max + 1) };
    }

    private async Task<long> AllocateNumber(int branchId, long? requestedNumber)
    {
        var maximum = await db.DeliveryNotes.Where(n => n.BranchId == branchId)
            .Select(n => (long?)n.DeliveryNoteNumber).MaxAsync() ?? 0L;
        var floor = maximum + 1;
        if (requestedNumber.HasValue && requestedNumber.Value <= 0)
            throw new InvalidOperationException("El número de remito debe ser positivo");
        if (requestedNumber.HasValue && await db.DeliveryNotes.AnyAsync(n => n.BranchId == branchId && n.DeliveryNoteNumber == requestedNumber.Value))
            throw new DeliveryNoteNumberConflictException();

        if (!db.Database.IsRelational())
        {
            var sequence = await db.BranchDeliveryNoteSequences.FindAsync(branchId);
            if (sequence == null)
            {
                sequence = new() { BranchId = branchId, NextDeliveryNoteNumber = floor };
                db.BranchDeliveryNoteSequences.Add(sequence);
            }
            sequence.NextDeliveryNoteNumber = Math.Max(sequence.NextDeliveryNoteNumber, floor);
            if (requestedNumber.HasValue)
            {
                sequence.NextDeliveryNoteNumber = Math.Max(sequence.NextDeliveryNoteNumber, requestedNumber.Value + 1);
                return requestedNumber.Value;
            }
            var number = sequence.NextDeliveryNoteNumber++;
            return number;
        }

        await db.Database.ExecuteSqlInterpolatedAsync($"IF NOT EXISTS (SELECT 1 FROM dbo.BranchDeliveryNoteSequences WITH (UPDLOCK,HOLDLOCK) WHERE BranchId={branchId}) INSERT INTO dbo.BranchDeliveryNoteSequences (BranchId,NextDeliveryNoteNumber) SELECT {branchId},ISNULL(MAX(DeliveryNoteNumber),0)+1 FROM dbo.DeliveryNotes WITH (UPDLOCK,HOLDLOCK) WHERE BranchId={branchId}");
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = requestedNumber.HasValue
            ? "UPDATE dbo.BranchDeliveryNoteSequences SET NextDeliveryNoteNumber=CASE WHEN NextDeliveryNoteNumber < @requestedNext THEN @requestedNext ELSE NextDeliveryNoteNumber END WHERE BranchId=@branchId"
            : "UPDATE dbo.BranchDeliveryNoteSequences SET NextDeliveryNoteNumber=CASE WHEN NextDeliveryNoteNumber < @floor THEN @floor ELSE NextDeliveryNoteNumber END WHERE BranchId=@branchId; UPDATE dbo.BranchDeliveryNoteSequences SET NextDeliveryNoteNumber=NextDeliveryNoteNumber+1 OUTPUT INSERTED.NextDeliveryNoteNumber-1 WHERE BranchId=@branchId";
        AddParameter(command, "@branchId", branchId);
        if (requestedNumber.HasValue)
        {
            AddParameter(command, "@requestedNext", requestedNumber.Value + 1);
            await command.ExecuteNonQueryAsync();
            return requestedNumber.Value;
        }
        AddParameter(command, "@floor", floor);
        return Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private async Task<DeliveryNoteResponse> LoadResponse(int id)
    {
        var n = await db.DeliveryNotes.Include(x => x.Details).Include(x => x.Invoice).SingleAsync(x => x.Id == id);
        return new() { Id = n.Id, BranchId = n.BranchId, OriginalInvoiceBranchId = n.Invoice?.BranchId ?? n.BranchId,
            InvoiceId = n.InvoiceId, DeliveryNoteNumber = n.DeliveryNoteNumber, InvoiceNumber = n.Invoice?.InvoiceNumber,
            InvoiceType = n.Invoice?.InvoiceType, CustomerId = n.CustomerId, SalesRepId = n.SalesRepId, DeliveryNoteDate = n.DeliveryNoteDate,
            Details = n.Details.OrderBy(d => d.ItemNumber).Select(d => new CreateDeliveryNoteDetailRequest { ProductId = d.ProductId, Quantity = d.Quantity }).ToList() };
    }
    private static void Validate(CreateDeliveryNoteRequest r, string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 128) throw new InvalidOperationException("Idempotency-Key es requerido y admite hasta 128 caracteres");
        if ((!r.InvoiceId.HasValue && r.BranchId <= 0) || (!r.InvoiceId.HasValue && r.CustomerId <= 0) || r.DeliveryNoteDate == default) throw new InvalidOperationException("Sucursal, cliente y fecha son obligatorios");
        if (r.Details.Any(d => d.Quantity <= 0)) throw new InvalidOperationException("Las cantidades deben ser positivas");
    }
    private static string Fingerprint(CreateDeliveryNoteRequest r)
    {
        var canonical = string.Join("\n", r.BranchId, r.PointOfSale, r.DeliveryNoteDate.Date.ToString("O"), r.CustomerId,
            r.SalesRepId, r.DeliveryAddress, r.DeliveryCity, r.BusinessUnit, r.Clarification, r.Notes, r.InvoiceId, r.AdjustStock, r.RequestedDeliveryNoteNumber,
            string.Join(";", r.Details.Select(d => $"{d.ProductId}:{d.Quantity.ToString(CultureInfo.InvariantCulture)}")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class DeliveryNoteIdempotencyConflictException() : InvalidOperationException("Idempotency-Key ya fue usado con una solicitud diferente");
public sealed class DeliveryNoteNumberConflictException() : InvalidOperationException("El número de remito ya existe para la sucursal seleccionada");
