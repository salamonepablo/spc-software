using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.DebitNotes;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Command-side implementation for debit note write operations (CQRS-lite).
/// Contains all business logic for creating and voiding debit notes.
/// Depends on ITaxConfigurationService, IPricingService for calculation rules,
/// IDebitNoteQueryService for post-creation retrieval,
/// and ICurrentAccountService for recording billing movements.
/// </summary>
public class DebitNoteCommandService : IDebitNoteCommandService
{
    private readonly SPCDbContext _db;
    private readonly ITaxConfigurationService _taxService;
    private readonly IPricingService _pricingService;
    private readonly IDebitNoteQueryService _queryService;
    private readonly ICurrentAccountService _currentAccountService;

    public DebitNoteCommandService(
        SPCDbContext db,
        ITaxConfigurationService taxService,
        IPricingService pricingService,
        IDebitNoteQueryService queryService,
        ICurrentAccountService currentAccountService)
    {
        _db = db;
        _taxService = taxService;
        _pricingService = pricingService;
        _queryService = queryService;
        _currentAccountService = currentAccountService;
    }

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
            customer.DiscountPercent);

        // 6. Parse voucher type
        var voucherType = request.VoucherType.ToUpperInvariant() == "A"
            ? VoucherType.DebitNoteA
            : VoucherType.DebitNoteB;

        // 7. Calculate line items
        var lineResults = new List<(CreateDebitNoteDetalleRequest detail, LineCalculationResult calc, Product product)>();
        foreach (var detail in request.Details)
        {
            var product = products[detail.ProductId];
            var unitPrice = detail.UnitPrice ?? product.InvoicePrice;

            var lineCalc = _pricingService.CalculateLine(
                unitPrice,
                detail.Quantity,
                detail.DiscountPercent,
                product.VATPercent);

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

        // 13. Record movement in current account (Billing line - L1)
        // Debit notes increase customer debt, so billingAmount is POSITIVE
        var docType = voucherType == VoucherType.DebitNoteA ? DocumentType.DebitNoteA : DocumentType.DebitNoteB;
        var voucherLetter = voucherType == VoucherType.DebitNoteA ? "A" : "B";
        await _currentAccountService.RecordMovementAsync(
            customerId: request.CustomerId,
            documentType: docType,
            documentNumber: debitNote.DebitNoteNumber,
            billingAmount: debitNote.Total,
            budgetAmount: 0,
            description: $"Nota de Débito {voucherLetter} {branch.PointOfSale:D4}-{debitNote.DebitNoteNumber:D8}");

        // 14. Return complete response via query service
        return (await _queryService.GetByIdAsync(debitNote.Id))!;
    }

    public async Task<bool> VoidAsync(int id, string reason)
    {
        var note = await _db.DebitNotes
            .Include(n => n.Branch)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (note == null || note.IsVoided)
            return false;

        note.IsVoided = true;
        note.Notes = string.IsNullOrEmpty(note.Notes)
            ? $"IsVoided: {reason}"
            : $"{note.Notes} | IsVoided: {reason}";

        await _db.SaveChangesAsync();

        // Reverse the impact on Current Account (negative amount to reverse the debit)
        var docType = note.VoucherType == VoucherType.DebitNoteA ? DocumentType.DebitNoteA : DocumentType.DebitNoteB;
        var voucherLetter = note.VoucherType == VoucherType.DebitNoteA ? "A" : "B";
        await _currentAccountService.RecordMovementAsync(
            customerId: note.CustomerId,
            documentType: docType,
            documentNumber: note.DebitNoteNumber,
            billingAmount: -note.Total,
            budgetAmount: 0,
            description: $"Anulación ND {voucherLetter} {note.PointOfSale:D4}-{note.DebitNoteNumber:D8}: {reason}");

        return true;
    }

    private async Task<long> GetNextDebitNoteNumberAsync(VoucherType voucherType, int pointOfSale)
    {
        var lastNumber = await _db.DebitNotes
            .Where(n => n.VoucherType == voucherType && n.PointOfSale == pointOfSale)
            .MaxAsync(n => (long?)n.DebitNoteNumber) ?? 0;

        return lastNumber + 1;
    }
}
