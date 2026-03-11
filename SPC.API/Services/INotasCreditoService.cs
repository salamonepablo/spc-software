using SPC.API.Contracts.CreditNotes;

namespace SPC.API.Services;

/// <summary>
/// Credit Notes service interface
/// </summary>
public interface ICreditNotesService
{
    // ===========================================
    // QUERIES
    // ===========================================
    
    /// <summary>Get all credit notes (paginated)</summary>
    Task<IEnumerable<CreditNoteResponse>> GetAllAsync(int skip = 0, int take = 50);
    
    /// <summary>Get credit note by ID with all details</summary>
    Task<CreditNoteCompletaResponse?> GetByIdAsync(int id);
    
    /// <summary>Get credit notes by customer</summary>
    Task<IEnumerable<CreditNoteResponse>> GetByCustomerAsync(int customerId);
    
    /// <summary>Get credit notes by invoice</summary>
    Task<IEnumerable<CreditNoteResponse>> GetByInvoiceAsync(int invoiceId);
    
    /// <summary>Get credit notes by date range</summary>
    Task<IEnumerable<CreditNoteResponse>> GetByDateRangeAsync(DateTime from, DateTime to);
    
    /// <summary>Search credit notes</summary>
    Task<IEnumerable<CreditNoteResponse>> SearchAsync(string term);
    
    /// <summary>Get total count</summary>
    Task<int> GetCountAsync();
    
    // ===========================================
    // COMMANDS
    // ===========================================
    
    /// <summary>
    /// Creates a new credit note with full business rule calculations.
    /// - VAT rate is retrieved from configuration and stored for immutability
    /// - Applies customer and document-level discounts
    /// - Calculates IIBB perception if applicable
    /// </summary>
    Task<CreditNoteCompletaResponse> CreateAsync(CreateCreditNoteRequest request);
    
    /// <summary>
    /// Voids a credit note (soft delete)
    /// </summary>
    Task<bool> AnularAsync(int id, string reason);
}
