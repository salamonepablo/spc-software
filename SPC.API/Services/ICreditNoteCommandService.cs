using SPC.API.Contracts.CreditNotes;

namespace SPC.API.Services;

/// <summary>
/// Command-side interface for credit note write operations (CQRS-lite).
/// Contains business logic for creation and voiding.
/// </summary>
public interface ICreditNoteCommandService
{
    /// <summary>
    /// Creates a new credit note with full business rule calculations.
    /// - VAT rate is retrieved from configuration and stored for immutability
    /// - Applies customer and document-level discounts
    /// - Calculates IIBB perception if applicable
    /// </summary>
    Task<CreditNoteCompletaResponse> CreateAsync(CreateCreditNoteRequest request);

    /// <summary>
    /// Voids a credit note (soft delete).
    /// </summary>
    /// <param name="id">Credit note ID</param>
    /// <param name="reason">Reason for voiding</param>
    /// <returns>True if voided successfully, false if not found or already voided</returns>
    Task<bool> VoidAsync(int id, string reason);
}
