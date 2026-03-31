using SPC.API.Contracts.Stock;

namespace SPC.API.Services;

/// <summary>
/// Stock service interface for stock queries
/// </summary>
public interface IStockService
{
    /// <summary>Get all stock entries with product and warehouse info</summary>
    Task<IEnumerable<StockResponse>> GetAllAsync();
    
    /// <summary>Get stock summary by product (all warehouses combined)</summary>
    Task<IEnumerable<StockResumenResponse>> GetResumenAsync();
    
    /// <summary>Get stock for a specific product across all warehouses</summary>
    Task<IEnumerable<StockResponse>> GetByProductAsync(int productoId);
    
    /// <summary>Get all stock in a specific warehouse</summary>
    Task<IEnumerable<StockResponse>> GetByWarehouseAsync(int depositoId);
    
    /// <summary>Get products with stock below minimum</summary>
    Task<IEnumerable<StockResumenResponse>> GetBajoMinimoAsync();
    
    /// <summary>Search stock by product code or description</summary>
    Task<IEnumerable<StockResumenResponse>> SearchAsync(string termino);
}
