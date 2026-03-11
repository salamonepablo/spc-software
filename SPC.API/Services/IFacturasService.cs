using SPC.API.Contracts.Invoices;

namespace SPC.API.Services;

/// <summary>
/// Invoices service interface for invoice operations
/// </summary>
public interface IInvoicesService
{
    // ===========================================
    // QUERIES
    // ===========================================
    
    /// <summary>Get all invoices (paginated)</summary>
    Task<IEnumerable<InvoiceResponse>> GetAllAsync(int skip = 0, int take = 50);
    
    /// <summary>Get invoice by ID with all details</summary>
    Task<InvoiceCompletaResponse?> GetByIdAsync(int id);
    
    /// <summary>Get invoices by customer</summary>
    Task<IEnumerable<InvoiceResponse>> GetByCustomerAsync(int clienteId);
    
    /// <summary>Get invoices by date range</summary>
    Task<IEnumerable<InvoiceResponse>> GetByFechaAsync(DateTime desde, DateTime hasta);
    
    /// <summary>Search invoices by number or customer name</summary>
    Task<IEnumerable<InvoiceResponse>> SearchAsync(string termino);
    
    /// <summary>Get invoicing summary statistics</summary>
    Task<InvoicecionResumenResponse> GetResumenAsync();
    
    /// <summary>Get total count of invoices</summary>
    Task<int> GetCountAsync();
    
    // ===========================================
    // COMMANDS
    // ===========================================
    
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
    /// Voids an invoice (soft delete, marks as Anulada).
    /// </summary>
    Task<bool> AnularAsync(int id, string motivo);
}
