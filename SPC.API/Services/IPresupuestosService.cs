using SPC.API.Contracts.Quotes;

namespace SPC.API.Services;

/// <summary>
/// Quotes (Quotes) service interface
/// </summary>
public interface IQuotesService
{
    // ===========================================
    // QUERIES
    // ===========================================
    
    /// <summary>Get all quotes (paginated)</summary>
    Task<IEnumerable<QuoteResponse>> GetAllAsync(int skip = 0, int take = 50);
    
    /// <summary>Get quote by ID with all details</summary>
    Task<QuoteCompletoResponse?> GetByIdAsync(int id);
    
    /// <summary>Get quotes by customer</summary>
    Task<IEnumerable<QuoteResponse>> GetByCustomerAsync(int customerId);
    
    /// <summary>Get quotes by date range</summary>
    Task<IEnumerable<QuoteResponse>> GetByDateRangeAsync(DateTime from, DateTime to);
    
    /// <summary>Search quotes by number or customer name</summary>
    Task<IEnumerable<QuoteResponse>> SearchAsync(string term);
    
    /// <summary>Get total count</summary>
    Task<int> GetCountAsync();
    
    // ===========================================
    // COMMANDS
    // ===========================================
    
    /// <summary>
    /// Creates a new quote with pricing calculations.
    /// - Uses PrecioQuote from products (includes VAT by convention)
    /// - No VAT calculation (quote prices are final)
    /// - Applies customer and document-level discounts
    /// </summary>
    Task<QuoteCompletoResponse> CreateAsync(CreateQuoteRequest request);
    
    /// <summary>
    /// Voids a quote (soft delete)
    /// </summary>
    Task<bool> AnularAsync(int id, string reason);
}
