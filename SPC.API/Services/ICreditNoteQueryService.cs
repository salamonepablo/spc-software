using SPC.API.Contracts.CreditNotes;

namespace SPC.API.Services;

/// <summary>
/// Query-side interface for credit note read operations (CQRS-lite).
/// Depends only on DbContext — no business services.
/// </summary>
public interface ICreditNoteQueryService
{
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

    /// <summary>Search credit notes by number or customer name/CUIT</summary>
    Task<IEnumerable<CreditNoteResponse>> SearchAsync(string term);

    /// <summary>Get total count of credit notes</summary>
    Task<int> GetCountAsync();
}
