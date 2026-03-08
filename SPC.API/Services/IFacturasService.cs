using SPC.API.Contracts.Facturas;

namespace SPC.API.Services;

/// <summary>
/// Facturas service interface for invoice queries
/// </summary>
public interface IFacturasService
{
    /// <summary>Get all invoices (paginated)</summary>
    Task<IEnumerable<FacturaResponse>> GetAllAsync(int skip = 0, int take = 50);
    
    /// <summary>Get invoice by ID with all details</summary>
    Task<FacturaCompletaResponse?> GetByIdAsync(int id);
    
    /// <summary>Get invoices by customer</summary>
    Task<IEnumerable<FacturaResponse>> GetByClienteAsync(int clienteId);
    
    /// <summary>Get invoices by date range</summary>
    Task<IEnumerable<FacturaResponse>> GetByFechaAsync(DateTime desde, DateTime hasta);
    
    /// <summary>Search invoices by number or customer name</summary>
    Task<IEnumerable<FacturaResponse>> SearchAsync(string termino);
    
    /// <summary>Get invoicing summary statistics</summary>
    Task<FacturacionResumenResponse> GetResumenAsync();
    
    /// <summary>Get total count of invoices</summary>
    Task<int> GetCountAsync();
}
