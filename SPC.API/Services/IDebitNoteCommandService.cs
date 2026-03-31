using SPC.API.Contracts.DebitNotes;

namespace SPC.API.Services;

/// <summary>
/// Command-side interface for debit note write operations (CQRS-lite).
/// Contains business logic for creation and voiding.
/// </summary>
public interface IDebitNoteCommandService
{
    /// <summary>
    /// Creates a new debit note with full business rule calculations.
    /// - VAT rate is retrieved from configuration and stored for immutability
    /// - Applies customer and document-level discounts
    /// - Calculates IIBB perception if applicable
    /// </summary>
    Task<DebitNoteCompletaResponse> CreateAsync(CreateDebitNoteRequest request);

    /// <summary>
    /// Voids a debit note (soft delete).
    /// </summary>
    /// <param name="id">Debit note ID</param>
    /// <param name="reason">Reason for voiding</param>
    /// <returns>True if voided successfully, false if not found or already voided</returns>
    Task<bool> VoidAsync(int id, string reason);
}
