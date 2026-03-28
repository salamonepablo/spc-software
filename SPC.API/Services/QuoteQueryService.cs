using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.Quotes;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Query-side implementation for quote read operations (CQRS-lite).
/// Only depends on SPCDbContext for data access (read-only).
/// No business logic — just querying and mapping.
/// </summary>
public class QuoteQueryService : IQuoteQueryService
{
    private readonly SPCDbContext _db;

    public QuoteQueryService(SPCDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<QuoteResponse>> GetAllAsync(int skip = 0, int take = 50)
    {
        var quotes = await _db.Quotes
            .AsNoTracking()
            .OrderByDescending(q => q.QuoteDate)
            .ThenByDescending(q => q.QuoteNumber)
            .Skip(skip)
            .Take(take)
            .Select(q => new QuoteResponse
            {
                Id = q.Id,
                BranchId = q.BranchId,
                BranchName = q.Branch != null ? q.Branch.Name : null,
                QuoteNumber = q.QuoteNumber,
                NumeroCompleto = FormatQuoteNumber(q.QuoteNumber),
                QuoteDate = q.QuoteDate,
                CustomerId = q.CustomerId,
                CustomerName = q.Customer != null ? q.Customer.CompanyName : "",
                SalesRepId = q.SalesRepId,
                SalesRepName = q.SalesRep != null ? q.SalesRep.FirstName : null,
                Subtotal = q.Subtotal,
                DiscountPercent = q.DiscountPercent,
                DiscountAmount = q.DiscountAmount,
                Total = q.Total,
                IsVoided = q.IsVoided,
                ItemCount = q.Details.Count
            })
            .ToListAsync();

        return quotes;
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
            .AsNoTracking()
            .Where(q => q.CustomerId == customerId)
            .OrderByDescending(q => q.QuoteDate)
            .ThenByDescending(q => q.QuoteNumber)
            .Select(q => new QuoteResponse
            {
                Id = q.Id,
                BranchId = q.BranchId,
                BranchName = q.Branch != null ? q.Branch.Name : null,
                QuoteNumber = q.QuoteNumber,
                NumeroCompleto = FormatQuoteNumber(q.QuoteNumber),
                QuoteDate = q.QuoteDate,
                CustomerId = q.CustomerId,
                CustomerName = q.Customer != null ? q.Customer.CompanyName : "",
                SalesRepId = q.SalesRepId,
                SalesRepName = q.SalesRep != null ? q.SalesRep.FirstName : null,
                Subtotal = q.Subtotal,
                DiscountPercent = q.DiscountPercent,
                DiscountAmount = q.DiscountAmount,
                Total = q.Total,
                IsVoided = q.IsVoided,
                ItemCount = q.Details.Count
            })
            .ToListAsync();

        return quotes;
    }

    public async Task<IEnumerable<QuoteResponse>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var quotes = await _db.Quotes
            .AsNoTracking()
            .Where(q => q.QuoteDate >= from && q.QuoteDate <= to)
            .OrderByDescending(q => q.QuoteDate)
            .ThenByDescending(q => q.QuoteNumber)
            .Select(q => new QuoteResponse
            {
                Id = q.Id,
                BranchId = q.BranchId,
                BranchName = q.Branch != null ? q.Branch.Name : null,
                QuoteNumber = q.QuoteNumber,
                NumeroCompleto = FormatQuoteNumber(q.QuoteNumber),
                QuoteDate = q.QuoteDate,
                CustomerId = q.CustomerId,
                CustomerName = q.Customer != null ? q.Customer.CompanyName : "",
                SalesRepId = q.SalesRepId,
                SalesRepName = q.SalesRep != null ? q.SalesRep.FirstName : null,
                Subtotal = q.Subtotal,
                DiscountPercent = q.DiscountPercent,
                DiscountAmount = q.DiscountAmount,
                Total = q.Total,
                IsVoided = q.IsVoided,
                ItemCount = q.Details.Count
            })
            .ToListAsync();

        return quotes;
    }

    public async Task<IEnumerable<QuoteResponse>> SearchAsync(string term)
    {
        long.TryParse(term, out var quoteNumber);

        var quotes = await _db.Quotes
            .AsNoTracking()
            .Where(q => q.QuoteNumber == quoteNumber ||
                       q.Customer!.CompanyName.Contains(term) ||
                       (q.Customer!.TradeName != null && q.Customer.TradeName.Contains(term)))
            .OrderByDescending(q => q.QuoteDate)
            .ThenByDescending(q => q.QuoteNumber)
            .Take(100)
            .Select(q => new QuoteResponse
            {
                Id = q.Id,
                BranchId = q.BranchId,
                BranchName = q.Branch != null ? q.Branch.Name : null,
                QuoteNumber = q.QuoteNumber,
                NumeroCompleto = FormatQuoteNumber(q.QuoteNumber),
                QuoteDate = q.QuoteDate,
                CustomerId = q.CustomerId,
                CustomerName = q.Customer != null ? q.Customer.CompanyName : "",
                SalesRepId = q.SalesRepId,
                SalesRepName = q.SalesRep != null ? q.SalesRep.FirstName : null,
                Subtotal = q.Subtotal,
                DiscountPercent = q.DiscountPercent,
                DiscountAmount = q.DiscountAmount,
                Total = q.Total,
                IsVoided = q.IsVoided,
                ItemCount = q.Details.Count
            })
            .ToListAsync();

        return quotes;
    }

    public async Task<int> GetCountAsync()
    {
        return await _db.Quotes.CountAsync();
    }

    public async Task<QuotesResumenResponse> GetSummaryAsync()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var yearStart = new DateTime(today.Year, 1, 1);

        var quotesTodayQuery = _db.Quotes.AsNoTracking()
            .Where(q => q.QuoteDate.Date == today && !q.IsVoided);

        var quotesMonthQuery = _db.Quotes.AsNoTracking()
            .Where(q => q.QuoteDate >= monthStart && !q.IsVoided);

        var quotesYearQuery = _db.Quotes.AsNoTracking()
            .Where(q => q.QuoteDate >= yearStart && !q.IsVoided);

        var quotesTodayCount = await quotesTodayQuery.CountAsync();
        var quotesMonthCount = await quotesMonthQuery.CountAsync();
        var amountToday = await quotesTodayQuery.SumAsync(q => (decimal?)q.Total) ?? 0m;
        var amountMonth = await quotesMonthQuery.SumAsync(q => (decimal?)q.Total) ?? 0m;
        var amountYear = await quotesYearQuery.SumAsync(q => (decimal?)q.Total) ?? 0m;

        var totalQuotes = await _db.Quotes.CountAsync();

        return new QuotesResumenResponse
        {
            TotalQuotes = totalQuotes,
            QuotesHoy = quotesTodayCount,
            QuotesMes = quotesMonthCount,
            MontoHoy = amountToday,
            MontoMes = amountMonth,
            MontoAnio = amountYear
        };
    }

    // ===========================================
    // MAPPING
    // ===========================================

    private static QuoteCompletoResponse MapToCompleteResponse(Quote quote)
    {
        return new QuoteCompletoResponse
        {
            Id = quote.Id,
            BranchId = quote.BranchId,
            BranchName = quote.Branch?.Name,
            QuoteNumber = quote.QuoteNumber,
            NumeroCompleto = FormatQuoteNumber(quote.QuoteNumber),
            QuoteDate = quote.QuoteDate,
            CustomerId = quote.CustomerId,
            CustomerName = quote.Customer?.CompanyName ?? "",
            SalesRepId = quote.SalesRepId,
            SalesRepName = quote.SalesRep?.FirstName,
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

    private static string FormatQuoteNumber(long quoteNumber) => quoteNumber.ToString();
}
