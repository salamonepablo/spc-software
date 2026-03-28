using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.CreditNotes;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Command-side implementation for credit note write operations (CQRS-lite).
/// Contains all business logic for creating and voiding credit notes.
/// Depends on ITaxConfigurationService, IPricingService for calculation rules,
/// ICreditNoteQueryService for post-creation retrieval,
/// and ICurrentAccountService for recording billing movements.
/// </summary>
public class CreditNoteCommandService : ICreditNoteCommandService
{
    private readonly SPCDbContext _db;
    private readonly ITaxConfigurationService _taxService;
    private readonly IPricingService _pricingService;
    private readonly ICreditNoteQueryService _queryService;
    private readonly ICurrentAccountService _currentAccountService;

    public CreditNoteCommandService(
        SPCDbContext db,
        ITaxConfigurationService taxService,
        IPricingService pricingService,
        ICreditNoteQueryService queryService,
        ICurrentAccountService currentAccountService)
    {
        _db = db;
        _taxService = taxService;
        _pricingService = pricingService;
        _queryService = queryService;
        _currentAccountService = currentAccountService;
    }

    public async Task<CreditNoteCompletaResponse> CreateAsync(CreateCreditNoteRequest request)
    {
        // 1. Validate and load customer
        var customer = await _db.Customers
            .Include(c => c.TaxCondition)
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId)
            ?? throw new InvalidOperationException($"Customer {request.CustomerId} no encontrado");

        // 2. Validate and load branch
        var branch = await _db.Branches.FindAsync(request.BranchId)
            ?? throw new InvalidOperationException($"Sucursal {request.BranchId} no encontrada");

        // 3. Validate invoice if provided
        if (request.InvoiceId.HasValue)
        {
            var invoice = await _db.Invoices.FindAsync(request.InvoiceId.Value);
            if (invoice == null)
                throw new InvalidOperationException($"Invoice {request.InvoiceId} no encontrada");
            if (invoice.CustomerId != request.CustomerId)
                throw new InvalidOperationException("La factura no pertenece al cliente especificado");
        }

        // 4. Load all products
        var productIds = request.Details.Select(d => d.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var detail in request.Details)
        {
            if (!products.ContainsKey(detail.ProductId))
                throw new InvalidOperationException($"Product {detail.ProductId} no encontrado");
        }

        // 5. Get VAT rate from configuration
        var vatRate = await _taxService.GetDefaultVATRateAsync();

        // 6. Resolve document discount
        var documentDiscount = _pricingService.ResolveDiscount(
            request.DiscountPercent,
            customer.DiscountPercent);

        // 7. Parse voucher type
        var voucherType = request.VoucherType.ToUpperInvariant() == "A"
            ? VoucherType.CreditNoteA
            : VoucherType.CreditNoteB;

        // 8. Calculate line items
        var lineResults = new List<(CreateCreditNoteDetalleRequest detail, LineCalculationResult calc, Product product)>();
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

        // 14. Record movement in current account (Billing line - L1)
        // Credit notes reduce customer debt, so billingAmount is NEGATIVE
        var docType = voucherType == VoucherType.CreditNoteA ? DocumentType.CreditNoteA : DocumentType.CreditNoteB;
        var voucherLetter = voucherType == VoucherType.CreditNoteA ? "A" : "B";
        await _currentAccountService.RecordMovementAsync(
            customerId: request.CustomerId,
            documentType: docType,
            documentNumber: creditNote.CreditNoteNumber,
            billingAmount: -creditNote.Total,
            budgetAmount: 0,
            description: $"Nota de Crédito {voucherLetter} {branch.PointOfSale:D4}-{creditNote.CreditNoteNumber:D8}");

        // 15. Return complete response via query service
        return (await _queryService.GetByIdAsync(creditNote.Id))!;
    }

    public async Task<bool> VoidAsync(int id, string reason)
    {
        var note = await _db.CreditNotes
            .Include(n => n.Branch)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (note == null || note.IsVoided)
            return false;

        note.IsVoided = true;
        note.Notes = string.IsNullOrEmpty(note.Notes)
            ? $"IsVoided: {reason}"
            : $"{note.Notes} | IsVoided: {reason}";

        await _db.SaveChangesAsync();

        // Reverse the impact on Current Account (positive amount to reverse the credit)
        var docType = note.VoucherType == VoucherType.CreditNoteA ? DocumentType.CreditNoteA : DocumentType.CreditNoteB;
        var voucherLetter = note.VoucherType == VoucherType.CreditNoteA ? "A" : "B";
        await _currentAccountService.RecordMovementAsync(
            customerId: note.CustomerId,
            documentType: docType,
            documentNumber: note.CreditNoteNumber,
            billingAmount: note.Total,
            budgetAmount: 0,
            description: $"Anulación NC {voucherLetter} {note.PointOfSale:D4}-{note.CreditNoteNumber:D8}: {reason}");

        return true;
    }

    private async Task<long> GetNextCreditNoteNumberAsync(VoucherType voucherType, int pointOfSale)
    {
        var lastNumber = await _db.CreditNotes
            .Where(n => n.VoucherType == voucherType && n.PointOfSale == pointOfSale)
            .MaxAsync(n => (long?)n.CreditNoteNumber) ?? 0;

        return lastNumber + 1;
    }
}
