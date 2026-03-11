using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.DebitNotes;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Debit Notes service implementation
/// </summary>
public class DebitNotesService : IDebitNotesService
{
    private readonly SPCDbContext _db;
    private readonly ITaxConfigurationService _taxService;
    private readonly IPricingService _pricingService;

    public DebitNotesService(
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
        long.TryParse(term.Replace("-", ""), out var noteNumber);

        var notes = await _db.DebitNotes
            .Include(n => n.Customer)
            .Include(n => n.SalesRep)
            .Include(n => n.Branch)
            .Include(n => n.Details)
            .Where(n => n.DebitNoteNumber == noteNumber ||
                       n.Customer!.RazonSocial.Contains(term) ||
                       (n.Customer!.CUIT != null && n.Customer.CUIT.Contains(term)))
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
    // COMMANDS
    // ===========================================

    public async Task<DebitNoteCompletaResponse> CreateAsync(CreateDebitNoteRequest request)
    {
        // 1. Validate and load customer
        var customer = await _db.Customers
            .Include(c => c.TaxCondition)
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId)
            ?? throw new InvalidOperationException($"Customer {request.CustomerId} no encontrado");

        // 2. Validate and load branch
        var branch = await _db.Branches.FindAsync(request.BranchId)
            ?? throw new InvalidOperationException($"Sucursal {request.BranchId} no encontrada");

        // 3. Load all products
        var productIds = request.Details.Select(d => d.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var detail in request.Details)
        {
            if (!products.ContainsKey(detail.ProductId))
                throw new InvalidOperationException($"Product {detail.ProductId} no encontrado");
        }

        // 4. Get VAT rate from configuration
        var vatRate = await _taxService.GetDefaultVATRateAsync();

        // 5. Resolve document discount
        var documentDiscount = _pricingService.ResolveDiscount(
            request.DiscountPercent,
            customer.PorcentajeDescuento);

        // 6. Parse voucher type
        var voucherType = request.VoucherType.ToUpperInvariant() == "A"
            ? VoucherType.DebitNoteA
            : VoucherType.DebitNoteB;

        // 7. Calculate line items
        var lineResults = new List<(CreateDebitNoteDetalleRequest detail, LineCalculationResult calc, Product product)>();
        foreach (var detail in request.Details)
        {
            var product = products[detail.ProductId];
            var unitPrice = detail.UnitPrice ?? product.PrecioInvoice;
            
            var lineCalc = _pricingService.CalculateLine(
                unitPrice,
                detail.Quantity,
                detail.DiscountPercent,
                product.PorcentajeIVA);
            
            lineResults.Add((detail, lineCalc, product));
        }

        // 8. Calculate document totals
        var docCalc = _pricingService.CalculateDocument(
            lineResults.Select(l => l.calc),
            documentDiscount,
            vatRate,
            request.IIBBPercent);

        // 9. Get next debit note number
        var nextNumber = await GetNextDebitNoteNumberAsync(voucherType, branch.PointOfSale);

        // 10. Create debit note entity
        var debitNote = new DebitNote
        {
            VoucherType = voucherType,
            BranchId = request.BranchId,
            PointOfSale = branch.PointOfSale,
            DebitNoteNumber = nextNumber,
            DebitNoteDate = DateTime.Now,
            CustomerId = request.CustomerId,
            SalesRepId = request.SalesRepId,
            
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

        // 11. Create detail lines
        int itemNumber = 1;
        foreach (var (detail, calc, product) in lineResults)
        {
            debitNote.Details.Add(new DebitNoteDetail
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

        // 12. Save
        _db.DebitNotes.Add(debitNote);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(debitNote.Id))!;
    }

    public async Task<bool> AnularAsync(int id, string reason)
    {
        var note = await _db.DebitNotes.FindAsync(id);
        if (note == null || note.IsVoided)
            return false;

        note.IsVoided = true;
        note.Notes = string.IsNullOrEmpty(note.Notes)
            ? $"ANULADA: {reason}"
            : $"{note.Notes} | ANULADA: {reason}";

        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<long> GetNextDebitNoteNumberAsync(VoucherType voucherType, int pointOfSale)
    {
        var lastNumber = await _db.DebitNotes
            .Where(n => n.VoucherType == voucherType && n.PointOfSale == pointOfSale)
            .MaxAsync(n => (long?)n.DebitNoteNumber) ?? 0;

        return lastNumber + 1;
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
            CustomerName = note.Customer?.RazonSocial ?? "",
            CustomerCUIT = note.Customer?.CUIT,
            SalesRepId = note.SalesRepId,
            SalesRepName = note.SalesRep?.Nombre,
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
            CustomerName = note.Customer?.RazonSocial ?? "",
            CustomerCUIT = note.Customer?.CUIT,
            SalesRepId = note.SalesRepId,
            SalesRepName = note.SalesRep?.Nombre,
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
