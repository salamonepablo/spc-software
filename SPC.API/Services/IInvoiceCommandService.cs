using SPC.API.Contracts.Invoices;

namespace SPC.API.Services;

/// <summary>
/// Command-side interface for invoice write operations (CQRS-lite).
/// Contains all business logic for creating and voiding invoices.
/// Separated from queries to honor SRP.
/// </summary>
public interface IInvoiceCommandService
{
    /// <summary>
    /// Creates a new invoice with full business rule calculations.
    /// - Resolves customer default discount
    /// - Calculates line items with individual discounts
    /// - Applies document-level discount
    /// - Calculates VAT from configuration (not hardcoded)
    /// - Calculates IIBB perception if applicable
    /// - Stores VAT percentage in document for historical immutability
    /// </summary>
    Task<InvoiceCompletaResponse> CreateAsync(CreateInvoiceRequest request);

    /// <summary>
    /// Voids an invoice (soft delete, marks as IsVoided).
    /// </summary>
    Task<bool> VoidAsync(int id, string reason);
}
