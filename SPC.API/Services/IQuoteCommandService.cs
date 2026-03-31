using SPC.API.Contracts.Quotes;

namespace SPC.API.Services;

/// <summary>
/// Command-side interface for quote write operations (CQRS-lite).
/// Contains business logic for creating and voiding quotes,
/// including current account impact management.
/// Separated from queries to honor SRP.
/// </summary>
public interface IQuoteCommandService
{
    /// <summary>
    /// Creates a new quote with pricing calculations.
    /// - Uses QuotePrice from products (includes VAT by convention)
    /// - No VAT calculation (quote prices are final)
    /// - Applies customer and document-level discounts
    /// - Records current account movement (Budget line)
    /// </summary>
    Task<QuoteCompletoResponse> CreateAsync(CreateQuoteRequest request);

    /// <summary>
    /// Voids a quote (soft delete) and reverses current account impact.
    /// </summary>
    Task<bool> VoidAsync(int id, string reason);
}
