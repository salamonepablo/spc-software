using SPC.API.Contracts.Quotes;

namespace SPC.API.Services;

/// <summary>
/// Query-side interface for quote read operations (CQRS-lite).
/// Separated from commands to honor SRP.
/// </summary>
public interface IQuoteQueryService
{
    /// <summary>Get all quotes (paginated)</summary>
    Task<IEnumerable<QuoteResponse>> GetAllAsync(int skip = 0, int take = 50);

    /// <summary>Get quote by ID with all details</summary>
    Task<QuoteCompletoResponse?> GetByIdAsync(int id);

    /// <summary>Get quote by quote number with all details</summary>
    Task<QuoteCompletoResponse?> GetByNumberAsync(long quoteNumber);

    /// <summary>Get quotes by customer</summary>
    Task<IEnumerable<QuoteResponse>> GetByCustomerAsync(int customerId);

    /// <summary>Get quotes by date range</summary>
    Task<IEnumerable<QuoteResponse>> GetByDateRangeAsync(DateTime from, DateTime to);

    /// <summary>Search quotes by number or customer name</summary>
    Task<IEnumerable<QuoteResponse>> SearchAsync(string term);

    /// <summary>Get total count of quotes</summary>
    Task<int> GetCountAsync();

    /// <summary>Get summary statistics (today, month, year)</summary>
    Task<QuotesResumenResponse> GetSummaryAsync();
}
