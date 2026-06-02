using SPC.API.Contracts.Invoices;

namespace SPC.API.Services;

/// <summary>
/// Query-side interface for invoice read operations (CQRS-lite).
/// Separated from commands to honor SRP.
/// </summary>
public interface IInvoiceQueryService
{
    /// <summary>Get all invoices (paginated)</summary>
    Task<IEnumerable<InvoiceResponse>> GetAllAsync(int skip = 0, int take = 50);

    /// <summary>Get invoice by ID with all details</summary>
    Task<InvoiceCompletaResponse?> GetByIdAsync(int id);

    /// <summary>Get invoice by official document identity with all details</summary>
    Task<InvoiceCompletaResponse?> GetByDocumentAsync(string invoiceType, long invoiceNumber, int? pointOfSale = null, int? customerId = null);

    /// <summary>Get invoices by customer</summary>
    Task<IEnumerable<InvoiceResponse>> GetByCustomerAsync(int customerId);

    /// <summary>Get invoices by date range</summary>
    Task<IEnumerable<InvoiceResponse>> GetByDateRangeAsync(DateTime from, DateTime to);

    /// <summary>Search invoices by number or customer name</summary>
    Task<IEnumerable<InvoiceResponse>> SearchAsync(string term);

    /// <summary>Get invoicing summary statistics</summary>
    Task<InvoicecionResumenResponse> GetSummaryAsync();

    /// <summary>Get total count of invoices</summary>
    Task<int> GetCountAsync();
}
