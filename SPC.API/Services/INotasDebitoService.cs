using SPC.API.Contracts.DebitNotes;

namespace SPC.API.Services;

/// <summary>
/// Debit Notes service interface
/// </summary>
public interface IDebitNotesService
{
    // ===========================================
    // QUERIES
    // ===========================================
    
    /// <summary>Get all debit notes (paginated)</summary>
    Task<IEnumerable<DebitNoteResponse>> GetAllAsync(int skip = 0, int take = 50);
    
    /// <summary>Get debit note by ID with all details</summary>
    Task<DebitNoteCompletaResponse?> GetByIdAsync(int id);
    
    /// <summary>Get debit notes by customer</summary>
    Task<IEnumerable<DebitNoteResponse>> GetByCustomerAsync(int customerId);
    
    /// <summary>Get debit notes by date range</summary>
    Task<IEnumerable<DebitNoteResponse>> GetByDateRangeAsync(DateTime from, DateTime to);
    
    /// <summary>Search debit notes</summary>
    Task<IEnumerable<DebitNoteResponse>> SearchAsync(string term);
    
    /// <summary>Get total count</summary>
    Task<int> GetCountAsync();
    
    // ===========================================
    // COMMANDS
    // ===========================================
    
    /// <summary>
    /// Creates a new debit note with full business rule calculations.
    /// - VAT rate is retrieved from configuration and stored for immutability
    /// - Applies customer and document-level discounts
    /// - Calculates IIBB perception if applicable
    /// </summary>
    Task<DebitNoteCompletaResponse> CreateAsync(CreateDebitNoteRequest request);
    
    /// <summary>
    /// Voids a debit note (soft delete)
    /// </summary>
    Task<bool> AnularAsync(int id, string reason);
}
