using SPC.API.Contracts.DebitNotes;

namespace SPC.API.Services;

/// <summary>
/// Query-side interface for debit note read operations (CQRS-lite).
/// Depends only on DbContext — no business services.
/// </summary>
public interface IDebitNoteQueryService
{
    /// <summary>Get all debit notes (paginated)</summary>
    Task<IEnumerable<DebitNoteResponse>> GetAllAsync(int skip = 0, int take = 50);

    /// <summary>Get debit note by ID with all details</summary>
    Task<DebitNoteCompletaResponse?> GetByIdAsync(int id);

    /// <summary>Get debit notes by customer</summary>
    Task<IEnumerable<DebitNoteResponse>> GetByCustomerAsync(int customerId);

    /// <summary>Get debit notes by date range</summary>
    Task<IEnumerable<DebitNoteResponse>> GetByDateRangeAsync(DateTime from, DateTime to);

    /// <summary>Search debit notes by number or customer name/CUIT</summary>
    Task<IEnumerable<DebitNoteResponse>> SearchAsync(string term);

    /// <summary>Get total count of debit notes</summary>
    Task<int> GetCountAsync();
}
