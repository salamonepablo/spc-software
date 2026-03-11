using SPC.API.Contracts.Products;

namespace SPC.API.Services;

/// <summary>
/// Service interface for Product business operations
/// </summary>
public interface IProductsService
{
    Task<IEnumerable<ProductResponse>> GetAllAsync();
    Task<ProductResponse?> GetByIdAsync(int id);
    Task<IEnumerable<ProductResponse>> SearchAsync(string descripcion);
    Task<ProductResponse> CreateAsync(CreateProductRequest request);
    Task<ProductResponse?> UpdateAsync(int id, UpdateProductRequest request);
    Task<bool> DeleteAsync(int id);
}
