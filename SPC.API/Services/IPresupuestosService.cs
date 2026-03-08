using SPC.API.Contracts.Presupuestos;

namespace SPC.API.Services;

/// <summary>
/// Presupuestos (Quotes) service interface
/// </summary>
public interface IPresupuestosService
{
    // ===========================================
    // QUERIES
    // ===========================================
    
    /// <summary>Get all quotes (paginated)</summary>
    Task<IEnumerable<PresupuestoResponse>> GetAllAsync(int skip = 0, int take = 50);
    
    /// <summary>Get quote by ID with all details</summary>
    Task<PresupuestoCompletoResponse?> GetByIdAsync(int id);
    
    /// <summary>Get quotes by customer</summary>
    Task<IEnumerable<PresupuestoResponse>> GetByCustomerAsync(int customerId);
    
    /// <summary>Get quotes by date range</summary>
    Task<IEnumerable<PresupuestoResponse>> GetByDateRangeAsync(DateTime from, DateTime to);
    
    /// <summary>Search quotes by number or customer name</summary>
    Task<IEnumerable<PresupuestoResponse>> SearchAsync(string term);
    
    /// <summary>Get total count</summary>
    Task<int> GetCountAsync();
    
    // ===========================================
    // COMMANDS
    // ===========================================
    
    /// <summary>
    /// Creates a new quote with pricing calculations.
    /// - Uses PrecioPresupuesto from products (includes VAT by convention)
    /// - No VAT calculation (quote prices are final)
    /// - Applies customer and document-level discounts
    /// </summary>
    Task<PresupuestoCompletoResponse> CreateAsync(CreatePresupuestoRequest request);
    
    /// <summary>
    /// Voids a quote (soft delete)
    /// </summary>
    Task<bool> AnularAsync(int id, string reason);
}
