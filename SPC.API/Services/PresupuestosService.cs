using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.Quotes;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Quotes (Quotes) service implementation
/// </summary>
public class QuotesService : IQuotesService
{
    private readonly SPCDbContext _db;
    private readonly IPricingService _pricingService;

    public QuotesService(SPCDbContext db, IPricingService pricingService)
    {
        _db = db;
        _pricingService = pricingService;
    }

    // ===========================================
    // QUERIES
    // ===========================================

    public async Task<IEnumerable<QuoteResponse>> GetAllAsync(int skip = 0, int take = 50)
    {
        var quotes = await _db.Quotes
            .Include(q => q.Customer)
            .Include(q => q.SalesRep)
            .Include(q => q.Branch)
            .Include(q => q.Details)
            .OrderByDescending(q => q.QuoteDate)
            .ThenByDescending(q => q.QuoteNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return quotes.Select(MapToResponse);
    }

    public async Task<QuoteCompletoResponse?> GetByIdAsync(int id)
    {
        var quote = await _db.Quotes
            .Include(q => q.Customer)
            .Include(q => q.SalesRep)
            .Include(q => q.Branch)
            .Include(q => q.Details)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quote == null) return null;

        return MapToCompleteResponse(quote);
    }

    public async Task<IEnumerable<QuoteResponse>> GetByCustomerAsync(int customerId)
    {
        var quotes = await _db.Quotes
            .Include(q => q.Customer)
            .Include(q => q.SalesRep)
            .Include(q => q.Branch)
            .Include(q => q.Details)
            .Where(q => q.CustomerId == customerId)
            .OrderByDescending(q => q.QuoteDate)
            .ToListAsync();

        return quotes.Select(MapToResponse);
    }

    public async Task<IEnumerable<QuoteResponse>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var quotes = await _db.Quotes
            .Include(q => q.Customer)
            .Include(q => q.SalesRep)
            .Include(q => q.Branch)
            .Include(q => q.Details)
            .Where(q => q.QuoteDate >= from && q.QuoteDate <= to)
            .OrderByDescending(q => q.QuoteDate)
            .ToListAsync();

        return quotes.Select(MapToResponse);
    }

    public async Task<IEnumerable<QuoteResponse>> SearchAsync(string term)
    {
        long.TryParse(term, out var quoteNumber);

        var quotes = await _db.Quotes
            .Include(q => q.Customer)
            .Include(q => q.SalesRep)
            .Include(q => q.Branch)
            .Include(q => q.Details)
            .Where(q => q.QuoteNumber == quoteNumber ||
                       q.Customer!.RazonSocial.Contains(term) ||
                       (q.Customer!.NombreFantasia != null && q.Customer.NombreFantasia.Contains(term)))
            .OrderByDescending(q => q.QuoteDate)
            .Take(100)
            .ToListAsync();

        return quotes.Select(MapToResponse);
    }

    public async Task<int> GetCountAsync()
    {
        return await _db.Quotes.CountAsync();
    }

    // ===========================================
    // COMMANDS
    // ===========================================

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
            customer.PorcentajeDescuento);

        // 5. Calculate line items (quotes use PrecioQuote, no VAT calculation)
        var lineResults = new List<(CreateQuoteDetalleRequest detail, LineCalculationResult calc, Product product)>();
        foreach (var detail in request.Details)
        {
            var product = products[detail.ProductId];
            
            // Use provided price or default to product's quote price
            var unitPrice = detail.UnitPrice ?? product.PrecioQuote;
            
            // For quotes, VAT is included in price, so we set 0 for calculation
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

        // 11. Return
        return (await GetByIdAsync(quote.Id))!;
    }

    public async Task<bool> AnularAsync(int id, string reason)
    {
        var quote = await _db.Quotes.FindAsync(id);
        if (quote == null || quote.IsVoided)
            return false;

        quote.IsVoided = true;
        quote.Notes = string.IsNullOrEmpty(quote.Notes)
            ? $"ANULADO: {reason}"
            : $"{quote.Notes} | ANULADO: {reason}";

        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<long> GetNextQuoteNumberAsync(int branchId)
    {
        var lastNumber = await _db.Quotes
            .Where(q => q.BranchId == branchId)
            .MaxAsync(q => (long?)q.QuoteNumber) ?? 0;

        return lastNumber + 1;
    }

    // ===========================================
    // MAPPING
    // ===========================================

    private static QuoteResponse MapToResponse(Quote quote)
    {
        return new QuoteResponse
        {
            Id = quote.Id,
            BranchId = quote.BranchId,
            BranchName = quote.Branch?.Name,
            QuoteNumber = quote.QuoteNumber,
            NumeroCompleto = $"{quote.Branch?.Code ?? "?"}-{quote.QuoteNumber:D8}",
            QuoteDate = quote.QuoteDate,
            CustomerId = quote.CustomerId,
            CustomerName = quote.Customer?.RazonSocial ?? "",
            SalesRepId = quote.SalesRepId,
            SalesRepName = quote.SalesRep?.Nombre,
            Subtotal = quote.Subtotal,
            DiscountPercent = quote.DiscountPercent,
            DiscountAmount = quote.DiscountAmount,
            Total = quote.Total,
            IsVoided = quote.IsVoided,
            ItemCount = quote.Details.Count
        };
    }

    private static QuoteCompletoResponse MapToCompleteResponse(Quote quote)
    {
        return new QuoteCompletoResponse
        {
            Id = quote.Id,
            BranchId = quote.BranchId,
            BranchName = quote.Branch?.Name,
            QuoteNumber = quote.QuoteNumber,
            NumeroCompleto = $"{quote.Branch?.Code ?? "?"}-{quote.QuoteNumber:D8}",
            QuoteDate = quote.QuoteDate,
            CustomerId = quote.CustomerId,
            CustomerName = quote.Customer?.RazonSocial ?? "",
            SalesRepId = quote.SalesRepId,
            SalesRepName = quote.SalesRep?.Nombre,
            Subtotal = quote.Subtotal,
            DiscountPercent = quote.DiscountPercent,
            DiscountAmount = quote.DiscountAmount,
            Total = quote.Total,
            IsVoided = quote.IsVoided,
            ItemCount = quote.Details.Count,
            BusinessUnit = quote.BusinessUnit,
            Notes = quote.Notes,
            Details = quote.Details.Select(d => new QuoteDetalleResponse
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
