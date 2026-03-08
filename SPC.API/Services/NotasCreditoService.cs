using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.NotasCredito;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Credit Notes service implementation
/// </summary>
public class NotasCreditoService : INotasCreditoService
{
    private readonly SPCDbContext _db;
    private readonly ITaxConfigurationService _taxService;
    private readonly IPricingService _pricingService;

    public NotasCreditoService(
        SPCDbContext db,
        ITaxConfigurationService taxService,
        IPricingService pricingService)
    {
        _db = db;
        _taxService = taxService;
        _pricingService = pricingService;
    }

    // ===========================================
    // QUERIES
    // ===========================================

    public async Task<IEnumerable<NotaCreditoResponse>> GetAllAsync(int skip = 0, int take = 50)
    {
        var notes = await _db.CreditNotes
            .Include(n => n.Customer)
            .Include(n => n.SalesRep)
            .Include(n => n.Branch)
            .Include(n => n.Invoice)
            .Include(n => n.Details)
            .OrderByDescending(n => n.CreditNoteDate)
            .ThenByDescending(n => n.CreditNoteNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return notes.Select(MapToResponse);
    }

    public async Task<NotaCreditoCompletaResponse?> GetByIdAsync(int id)
    {
        var note = await _db.CreditNotes
            .Include(n => n.Customer)
            .Include(n => n.SalesRep)
            .Include(n => n.Branch)
            .Include(n => n.Invoice)
            .Include(n => n.Details)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (note == null) return null;

        return MapToCompleteResponse(note);
    }

    public async Task<IEnumerable<NotaCreditoResponse>> GetByCustomerAsync(int customerId)
    {
        var notes = await _db.CreditNotes
            .Include(n => n.Customer)
            .Include(n => n.SalesRep)
            .Include(n => n.Branch)
            .Include(n => n.Invoice)
            .Include(n => n.Details)
            .Where(n => n.CustomerId == customerId)
            .OrderByDescending(n => n.CreditNoteDate)
            .ToListAsync();

        return notes.Select(MapToResponse);
    }

    public async Task<IEnumerable<NotaCreditoResponse>> GetByInvoiceAsync(int invoiceId)
    {
        var notes = await _db.CreditNotes
            .Include(n => n.Customer)
            .Include(n => n.SalesRep)
            .Include(n => n.Branch)
            .Include(n => n.Invoice)
            .Include(n => n.Details)
            .Where(n => n.InvoiceId == invoiceId)
            .OrderByDescending(n => n.CreditNoteDate)
            .ToListAsync();

        return notes.Select(MapToResponse);
    }

    public async Task<IEnumerable<NotaCreditoResponse>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var notes = await _db.CreditNotes
            .Include(n => n.Customer)
            .Include(n => n.SalesRep)
            .Include(n => n.Branch)
            .Include(n => n.Invoice)
            .Include(n => n.Details)
            .Where(n => n.CreditNoteDate >= from && n.CreditNoteDate <= to)
            .OrderByDescending(n => n.CreditNoteDate)
            .ToListAsync();

        return notes.Select(MapToResponse);
    }

    public async Task<IEnumerable<NotaCreditoResponse>> SearchAsync(string term)
    {
        long.TryParse(term.Replace("-", ""), out var noteNumber);

        var notes = await _db.CreditNotes
            .Include(n => n.Customer)
            .Include(n => n.SalesRep)
            .Include(n => n.Branch)
            .Include(n => n.Invoice)
            .Include(n => n.Details)
            .Where(n => n.CreditNoteNumber == noteNumber ||
                       n.Customer!.RazonSocial.Contains(term) ||
                       (n.Customer!.CUIT != null && n.Customer.CUIT.Contains(term)))
            .OrderByDescending(n => n.CreditNoteDate)
            .Take(100)
            .ToListAsync();

        return notes.Select(MapToResponse);
    }

    public async Task<int> GetCountAsync()
    {
        return await _db.CreditNotes.CountAsync();
    }

    // ===========================================
    // COMMANDS
    // ===========================================

    public async Task<NotaCreditoCompletaResponse> CreateAsync(CreateNotaCreditoRequest request)
    {
        // 1. Validate and load customer
        var customer = await _db.Clientes
            .Include(c => c.CondicionIva)
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId)
            ?? throw new InvalidOperationException($"Cliente {request.CustomerId} no encontrado");

        // 2. Validate and load branch
        var branch = await _db.Branches.FindAsync(request.BranchId)
            ?? throw new InvalidOperationException($"Sucursal {request.BranchId} no encontrada");

        // 3. Validate invoice if provided
        if (request.InvoiceId.HasValue)
        {
            var invoice = await _db.Facturas.FindAsync(request.InvoiceId.Value);
            if (invoice == null)
                throw new InvalidOperationException($"Factura {request.InvoiceId} no encontrada");
            if (invoice.ClienteId != request.CustomerId)
                throw new InvalidOperationException("La factura no pertenece al cliente especificado");
        }

        // 4. Load all products
        var productIds = request.Details.Select(d => d.ProductId).Distinct().ToList();
        var products = await _db.Productos
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var detail in request.Details)
        {
            if (!products.ContainsKey(detail.ProductId))
                throw new InvalidOperationException($"Producto {detail.ProductId} no encontrado");
        }

        // 5. Get VAT rate from configuration
        var vatRate = await _taxService.GetDefaultVATRateAsync();

        // 6. Resolve document discount
        var documentDiscount = _pricingService.ResolveDiscount(
            request.DiscountPercent,
            customer.PorcentajeDescuento);

        // 7. Parse voucher type
        var voucherType = request.VoucherType.ToUpperInvariant() == "A"
            ? VoucherType.CreditNoteA
            : VoucherType.CreditNoteB;

        // 8. Calculate line items
        var lineResults = new List<(CreateNotaCreditoDetalleRequest detail, LineCalculationResult calc, Producto product)>();
        foreach (var detail in request.Details)
        {
            var product = products[detail.ProductId];
            var unitPrice = detail.UnitPrice ?? product.PrecioFactura;
            
            var lineCalc = _pricingService.CalculateLine(
                unitPrice,
                detail.Quantity,
                detail.DiscountPercent,
                product.PorcentajeIVA);
            
            lineResults.Add((detail, lineCalc, product));
        }

        // 9. Calculate document totals
        var docCalc = _pricingService.CalculateDocument(
            lineResults.Select(l => l.calc),
            documentDiscount,
            vatRate,
            request.IIBBPercent);

        // 10. Get next credit note number
        var nextNumber = await GetNextCreditNoteNumberAsync(voucherType, branch.PointOfSale);

        // 11. Create credit note entity
        var creditNote = new CreditNote
        {
            VoucherType = voucherType,
            BranchId = request.BranchId,
            PointOfSale = branch.PointOfSale,
            CreditNoteNumber = nextNumber,
            CreditNoteDate = DateTime.Now,
            CustomerId = request.CustomerId,
            SalesRepId = request.SalesRepId,
            InvoiceId = request.InvoiceId,
            
            // Store VAT for historical immutability
            VATPercent = vatRate,
            
            Subtotal = docCalc.NetSubtotal,
            VATAmount = docCalc.VATAmount,
            IIBBPercent = request.IIBBPercent,
            IIBBAmount = docCalc.IIBBAmount,
            DiscountPercent = documentDiscount,
            DiscountAmount = docCalc.DocumentDiscountAmount,
            Total = docCalc.Total,
            
            SalesCondition = request.SalesCondition,
            Notes = request.Notes,
            IsVoided = false
        };

        // 12. Create detail lines
        int itemNumber = 1;
        foreach (var (detail, calc, product) in lineResults)
        {
            creditNote.Details.Add(new CreditNoteDetail
            {
                ItemNumber = itemNumber++,
                ProductId = detail.ProductId,
                Quantity = detail.Quantity,
                UnitPrice = calc.UnitPrice,
                DiscountPercent = calc.DiscountPercent,
                DiscountAmount = calc.DiscountAmount,
                Subtotal = calc.Subtotal
            });
        }

        // 13. Save
        _db.CreditNotes.Add(creditNote);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(creditNote.Id))!;
    }

    public async Task<bool> AnularAsync(int id, string reason)
    {
        var note = await _db.CreditNotes.FindAsync(id);
        if (note == null || note.IsVoided)
            return false;

        note.IsVoided = true;
        note.Notes = string.IsNullOrEmpty(note.Notes)
            ? $"ANULADA: {reason}"
            : $"{note.Notes} | ANULADA: {reason}";

        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<long> GetNextCreditNoteNumberAsync(VoucherType voucherType, int pointOfSale)
    {
        var lastNumber = await _db.CreditNotes
            .Where(n => n.VoucherType == voucherType && n.PointOfSale == pointOfSale)
            .MaxAsync(n => (long?)n.CreditNoteNumber) ?? 0;

        return lastNumber + 1;
    }

    // ===========================================
    // MAPPING
    // ===========================================

    private static NotaCreditoResponse MapToResponse(CreditNote note)
    {
        return new NotaCreditoResponse
        {
            Id = note.Id,
            VoucherType = note.VoucherType == VoucherType.CreditNoteA ? "A" : "B",
            PointOfSale = note.PointOfSale,
            CreditNoteNumber = note.CreditNoteNumber,
            CreditNoteDate = note.CreditNoteDate,
            CustomerId = note.CustomerId,
            CustomerName = note.Customer?.RazonSocial ?? "",
            CustomerCUIT = note.Customer?.CUIT,
            SalesRepId = note.SalesRepId,
            SalesRepName = note.SalesRep?.Nombre,
            InvoiceId = note.InvoiceId,
            InvoiceNumber = note.Invoice != null
                ? $"{note.Invoice.TipoFactura} {note.Invoice.PuntoVenta:D4}-{note.Invoice.NumeroFactura:D8}"
                : null,
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

    private static NotaCreditoCompletaResponse MapToCompleteResponse(CreditNote note)
    {
        return new NotaCreditoCompletaResponse
        {
            Id = note.Id,
            VoucherType = note.VoucherType == VoucherType.CreditNoteA ? "A" : "B",
            PointOfSale = note.PointOfSale,
            CreditNoteNumber = note.CreditNoteNumber,
            CreditNoteDate = note.CreditNoteDate,
            CustomerId = note.CustomerId,
            CustomerName = note.Customer?.RazonSocial ?? "",
            CustomerCUIT = note.Customer?.CUIT,
            SalesRepId = note.SalesRepId,
            SalesRepName = note.SalesRep?.Nombre,
            InvoiceId = note.InvoiceId,
            InvoiceNumber = note.Invoice != null
                ? $"{note.Invoice.TipoFactura} {note.Invoice.PuntoVenta:D4}-{note.Invoice.NumeroFactura:D8}"
                : null,
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
            Details = note.Details.Select(d => new NotaCreditoDetalleResponse
            {
                Id = d.Id,
                ItemNumber = d.ItemNumber,
                ProductId = d.ProductId,
                ProductCode = d.Product?.Codigo ?? "",
                ProductDescription = d.Product?.Descripcion ?? "",
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                DiscountPercent = d.DiscountPercent,
                DiscountAmount = d.DiscountAmount,
                Subtotal = d.Subtotal
            }).OrderBy(d => d.ItemNumber).ToList()
        };
    }
}
