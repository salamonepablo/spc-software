using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.Products;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Service implementation for Product business operations
/// </summary>
public class ProductsService : IProductsService
{
    private readonly SPCDbContext _db;

    public ProductsService(SPCDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ProductResponse>> GetAllAsync()
    {
        var productos = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.UnitOfMeasure)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Description)
            .ToListAsync();

        return productos.Select(MapToResponse);
    }

    public async Task<ProductResponse?> GetByIdAsync(int id)
    {
        var producto = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(p => p.Id == id);

        return producto != null ? MapToResponse(producto) : null;
    }

    public async Task<IEnumerable<ProductResponse>> SearchAsync(string Description)
    {
        var productos = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive &&
                   (p.Description.Contains(Description) || p.Code.Contains(Description)))
            .OrderBy(p => p.Description)
            .Take(20)
            .ToListAsync();

        return productos.Select(MapToResponse);
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        var producto = new Product
        {
            Code = request.Code,
            Description = request.Description,
            SupplierCode = request.SupplierCode,
            CategoryId = request.CategoryId,
            UnitOfMeasureId = request.UnitOfMeasureId,
            SalePrice = request.SalePrice,
            CostPrice = request.CostPrice,
            VATPercent = request.VATPercent,
            MinimumStock = request.MinimumStock,
            Notes = request.Notes,
            IsActive = true
        };

        _db.Products.Add(producto);
        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(producto).Reference(p => p.Category).LoadAsync();
        await _db.Entry(producto).Reference(p => p.UnitOfMeasure).LoadAsync();

        return MapToResponse(producto);
    }

    public async Task<ProductResponse?> UpdateAsync(int id, UpdateProductRequest request)
    {
        var producto = await _db.Products.FindAsync(id);

        if (producto == null)
            return null;

        // Update properties
        producto.Code = request.Code;
        producto.Description = request.Description;
        producto.SupplierCode = request.SupplierCode;
        producto.CategoryId = request.CategoryId;
        producto.UnitOfMeasureId = request.UnitOfMeasureId;
        producto.SalePrice = request.SalePrice;
        producto.CostPrice = request.CostPrice;
        producto.VATPercent = request.VATPercent;
        producto.MinimumStock = request.MinimumStock;
        producto.Notes = request.Notes;

        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(producto).Reference(p => p.Category).LoadAsync();
        await _db.Entry(producto).Reference(p => p.UnitOfMeasure).LoadAsync();

        return MapToResponse(producto);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var producto = await _db.Products.FindAsync(id);

        if (producto == null)
            return false;

        // Soft delete
        producto.IsActive = false;
        await _db.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Maps a Product entity to ProductResponse DTO
    /// </summary>
    private static ProductResponse MapToResponse(Product producto)
    {
        return new ProductResponse
        {
            Id = producto.Id,
            Code = producto.Code,
            Description = producto.Description,
            SupplierCode = producto.SupplierCode,
            SalePrice = producto.SalePrice,
            CostPrice = producto.CostPrice,
            InvoicePrice = producto.InvoicePrice,
            QuotePrice = producto.QuotePrice,
            VATPercent = producto.VATPercent,
            MinimumStock = producto.MinimumStock,
            Notes = producto.Notes,
            IsActive = producto.IsActive,
            CategoryId = producto.CategoryId,
            CategoryName = producto.Category?.Name,
            UnitOfMeasureId = producto.UnitOfMeasureId,
            UnitOfMeasureName = producto.UnitOfMeasure?.Name,
            UnitOfMeasureCode = producto.UnitOfMeasure?.Code
        };
    }
}
