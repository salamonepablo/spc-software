using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.DebitNotes;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Query-side implementation for debit note read operations (CQRS-lite).
/// Only depends on SPCDbContext for data access (read-only).
/// No business logic — just querying and mapping.
/// </summary>
public class DebitNoteQueryService : IDebitNoteQueryService
{
    private readonly SPCDbContext _db;

    public DebitNoteQueryService(SPCDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<DebitNoteResponse>> GetAllAsync(int skip = 0, int take = 50)
    {
        var notes = await _db.DebitNotes
            .Include(n => n.Customer)
            .Include(n => n.SalesRep)
            .Include(n => n.Branch)
            .Include(n => n.Details)
            .OrderByDescending(n => n.DebitNoteDate)
            .ThenByDescending(n => n.DebitNoteNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return notes.Select(MapToResponse);
    }

    public async Task<DebitNoteCompletaResponse?> GetByIdAsync(int id)
    {
        var note = await _db.DebitNotes
            .Include(n => n.Customer)
            .Include(n => n.SalesRep)
            .Include(n => n.Branch)
            .Include(n => n.Details)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (note == null) return null;

        return MapToCompleteResponse(note);
    }

    public async Task<DebitNoteCompletaResponse?> GetByNumberAsync(long debitNoteNumber, int? customerId = null, string? voucherType = null, int? pointOfSale = null)
    {
        var query = _db.DebitNotes
            .Include(n => n.Customer)
            .Include(n => n.SalesRep)
            .Include(n => n.Branch)
            .Include(n => n.Details)
                .ThenInclude(d => d.Product)
            .Where(n => n.DebitNoteNumber == debitNoteNumber);

        if (customerId.HasValue)
        {
            query = query.Where(n => n.CustomerId == customerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(voucherType))
        {
            var normalizedVoucherType = voucherType.Trim().ToUpperInvariant() == "A"
                ? VoucherType.DebitNoteA
                : VoucherType.DebitNoteB;
            query = query.Where(n => n.VoucherType == normalizedVoucherType);
        }

        if (pointOfSale.HasValue)
        {
            query = query.Where(n => n.PointOfSale == pointOfSale.Value);
        }

        var note = await query
            .OrderByDescending(n => n.DebitNoteDate)
            .ThenByDescending(n => n.Id)
            .FirstOrDefaultAsync();

        return note == null ? null : MapToCompleteResponse(note);
    }

    public async Task<IEnumerable<DebitNoteResponse>> GetByCustomerAsync(int customerId)
    {
        var notes = await _db.DebitNotes
            .Include(n => n.Customer)
            .Include(n => n.SalesRep)
            .Include(n => n.Branch)
            .Include(n => n.Details)
            .Where(n => n.CustomerId == customerId)
            .OrderByDescending(n => n.DebitNoteDate)
            .ToListAsync();

        return notes.Select(MapToResponse);
    }

    public async Task<IEnumerable<DebitNoteResponse>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var notes = await _db.DebitNotes
            .Include(n => n.Customer)
            .Include(n => n.SalesRep)
            .Include(n => n.Branch)
            .Include(n => n.Details)
            .Where(n => n.DebitNoteDate >= from && n.DebitNoteDate <= to)
            .OrderByDescending(n => n.DebitNoteDate)
            .ToListAsync();

        return notes.Select(MapToResponse);
    }

    public async Task<IEnumerable<DebitNoteResponse>> SearchAsync(string term)
    {
        var trimmedTerm = term.Trim();
        var document = OfficialDocumentSearchParser.Parse(trimmedTerm);
        var query = _db.DebitNotes
            .Include(n => n.Customer)
            .Include(n => n.SalesRep)
            .Include(n => n.Branch)
            .Include(n => n.Details)
            .AsQueryable();

        if (document.Number.HasValue)
        {
            query = query.Where(n => n.DebitNoteNumber == document.Number.Value);

            if (!string.IsNullOrWhiteSpace(document.Type))
            {
                var voucherType = document.Type == "A" ? VoucherType.DebitNoteA : VoucherType.DebitNoteB;
                query = query.Where(n => n.VoucherType == voucherType);
            }

            if (document.PointOfSale.HasValue)
            {
                query = query.Where(n => n.PointOfSale == document.PointOfSale.Value);
            }
        }
        else
        {
            query = query.Where(n => n.Customer!.CompanyName.Contains(trimmedTerm) ||
                                     (n.Customer!.CUIT != null && n.Customer.CUIT.Contains(trimmedTerm)));
        }

        var notes = await query
            .OrderByDescending(n => n.DebitNoteDate)
            .Take(100)
            .ToListAsync();

        return notes.Select(MapToResponse);
    }

    public async Task<int> GetCountAsync()
    {
        return await _db.DebitNotes.CountAsync();
    }

    // ===========================================
    // MAPPING
    // ===========================================

    private static DebitNoteResponse MapToResponse(DebitNote note)
    {
        return new DebitNoteResponse
        {
            Id = note.Id,
            VoucherType = note.VoucherType == VoucherType.DebitNoteA ? "A" : "B",
            PointOfSale = note.PointOfSale,
            DebitNoteNumber = note.DebitNoteNumber,
            DebitNoteDate = note.DebitNoteDate,
            CustomerId = note.CustomerId,
            CustomerName = note.Customer?.CompanyName ?? "",
            CustomerCUIT = note.Customer?.CUIT,
            SalesRepId = note.SalesRepId,
            SalesRepName = note.SalesRep?.FirstName,
            Subtotal = note.Subtotal,
            VATPercent = note.VATPercent,
            VATAmount = note.VATAmount,
            IIBBPercent = note.IIBBPercent,
            IIBBAmount = note.IIBBAmount,
            DiscountPercent = note.DiscountPercent,
            DiscountAmount = note.DiscountAmount,
            Total = note.Total,
            CAE = note.CAE,
            CAEExpirationDate = note.CAEExpirationDate,
            IsVoided = note.IsVoided,
            ItemCount = note.Details.Count
        };
    }

    private static DebitNoteCompletaResponse MapToCompleteResponse(DebitNote note)
    {
        return new DebitNoteCompletaResponse
        {
            Id = note.Id,
            VoucherType = note.VoucherType == VoucherType.DebitNoteA ? "A" : "B",
            PointOfSale = note.PointOfSale,
            DebitNoteNumber = note.DebitNoteNumber,
            DebitNoteDate = note.DebitNoteDate,
            CustomerId = note.CustomerId,
            CustomerName = note.Customer?.CompanyName ?? "",
            CustomerCUIT = note.Customer?.CUIT,
            SalesRepId = note.SalesRepId,
            SalesRepName = note.SalesRep?.FirstName,
            Subtotal = note.Subtotal,
            VATPercent = note.VATPercent,
            VATAmount = note.VATAmount,
            IIBBPercent = note.IIBBPercent,
            IIBBAmount = note.IIBBAmount,
            DiscountPercent = note.DiscountPercent,
            DiscountAmount = note.DiscountAmount,
            Total = note.Total,
            CAE = note.CAE,
            CAEExpirationDate = note.CAEExpirationDate,
            IsVoided = note.IsVoided,
            ItemCount = note.Details.Count,
            SalesCondition = note.SalesCondition,
            Notes = note.Notes,
            Details = note.Details.Select(d => new DebitNoteDetalleResponse
            {
                Id = d.Id,
                ItemNumber = d.ItemNumber,
                ProductId = d.ProductId,
                ProductCode = d.Product?.Code ?? "",
                ProductDescription = d.Product?.Description ?? "",
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                DiscountPercent = d.DiscountPercent,
                DiscountAmount = d.DiscountAmount,
                Subtotal = d.Subtotal
            }).OrderBy(d => d.ItemNumber).ToList()
        };
    }
}
