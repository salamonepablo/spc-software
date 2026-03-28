using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.Quotes;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Command-side implementation for quote write operations (CQRS-lite).
/// Contains business logic for creating and voiding quotes,
/// including current account impact management.
/// </summary>
public class QuoteCommandService : IQuoteCommandService
{
    private readonly SPCDbContext _db;
    private readonly IPricingService _pricingService;
    private readonly ICurrentAccountService _currentAccountService;
    private readonly IQuoteQueryService _queryService;

    public QuoteCommandService(
        SPCDbContext db,
        IPricingService pricingService,
        ICurrentAccountService currentAccountService,
        IQuoteQueryService queryService)
    {
        _db = db;
        _pricingService = pricingService;
        _currentAccountService = currentAccountService;
        _queryService = queryService;
    }

    public async Task<QuoteCompletoResponse> CreateAsync(CreateQuoteRequest request)
    {
        // 1. Validate and load customer
        var customer = await _db.Customers.FindAsync(request.CustomerId)
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

        // 4. Resolve document discount
        var documentDiscount = _pricingService.ResolveDiscount(
            request.DiscountPercent,
            customer.DiscountPercent);

        // 5. Calculate line items (quotes use QuotePrice, no VAT calculation)
        var lineResults = new List<(CreateQuoteDetalleRequest detail, LineCalculationResult calc, Product product)>();
        foreach (var detail in request.Details)
        {
            var product = products[detail.ProductId];
            var unitPrice = detail.UnitPrice ?? product.QuotePrice;

            var lineCalc = _pricingService.CalculateLine(
                unitPrice,
                detail.Quantity,
                detail.DiscountPercent,
                0); // No separate VAT for quotes

            lineResults.Add((detail, lineCalc, product));
        }

        // 6. Calculate document totals (no VAT, no IIBB for quotes)
        var docCalc = _pricingService.CalculateDocument(
            lineResults.Select(l => l.calc),
            documentDiscount,
            0, // No VAT
            0); // No IIBB

        if (docCalc.Total <= 0)
        {
            throw new InvalidOperationException("Quote total must be greater than zero");
        }

        // 7. Get next quote number
        var nextNumber = await GetNextQuoteNumberAsync(request.BranchId);

        // 8. Create quote entity
        var quote = new Quote
        {
            BranchId = request.BranchId,
            QuoteNumber = nextNumber,
            QuoteDate = DateTime.Now,
            CustomerId = request.CustomerId,
            SalesRepId = request.SalesRepId,
            Subtotal = docCalc.LinesSubtotal,
            DiscountPercent = documentDiscount,
            DiscountAmount = docCalc.DocumentDiscountAmount,
            Total = docCalc.Total,
            BusinessUnit = request.BusinessUnit,
            Notes = request.Notes,
            IsVoided = false
        };

        // 9. Create detail lines
        int itemNumber = 1;
        foreach (var (detail, calc, product) in lineResults)
        {
            quote.Details.Add(new QuoteDetail
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

        // 10. Save
        _db.Quotes.Add(quote);
        await _db.SaveChangesAsync();

        // 11. Impact on Current Account (Budget line - L2)
        await _currentAccountService.RecordMovementAsync(
            customerId: request.CustomerId,
            documentType: DocumentType.Quote,
            documentNumber: quote.QuoteNumber,
            billingAmount: 0,
            budgetAmount: quote.Total,
            description: $"Presupuesto {branch.Code}-{quote.QuoteNumber:D8}");

        // 12. Return
        return (await _queryService.GetByIdAsync(quote.Id))!;
    }

    public async Task<bool> VoidAsync(int id, string reason)
    {
        var quote = await _db.Quotes
            .Include(q => q.Branch)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quote == null || quote.IsVoided)
            return false;

        quote.IsVoided = true;
        quote.Notes = string.IsNullOrEmpty(quote.Notes)
            ? $"IsVoided: {reason}"
            : $"{quote.Notes} | IsVoided: {reason}";

        await _db.SaveChangesAsync();

        // Reverse the impact on Current Account
        await _currentAccountService.RecordMovementAsync(
            customerId: quote.CustomerId,
            documentType: DocumentType.QuoteVoid,
            documentNumber: quote.QuoteNumber,
            billingAmount: 0,
            budgetAmount: -quote.Total,
            description: $"Anulación Presupuesto {quote.Branch?.Code ?? "?"}-{quote.QuoteNumber:D8}: {reason}");

        return true;
    }

    private async Task<long> GetNextQuoteNumberAsync(int branchId)
    {
        var lastNumber = await _db.Quotes
            .Where(q => q.BranchId == branchId)
            .MaxAsync(q => (long?)q.QuoteNumber) ?? 0;

        return lastNumber + 1;
    }
}
