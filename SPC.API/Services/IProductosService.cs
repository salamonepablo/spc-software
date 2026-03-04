using SPC.API.Contracts.Productos;

namespace SPC.API.Services;

/// <summary>
/// Service interface for Producto business operations
/// </summary>
public interface IProductosService
{
    Task<IEnumerable<ProductoResponse>> GetAllAsync();
    Task<ProductoResponse?> GetByIdAsync(int id);
    Task<IEnumerable<ProductoResponse>> SearchAsync(string descripcion);
    Task<ProductoResponse> CreateAsync(CreateProductoRequest request);
    Task<ProductoResponse?> UpdateAsync(int id, UpdateProductoRequest request);
    Task<bool> DeleteAsync(int id);
}
