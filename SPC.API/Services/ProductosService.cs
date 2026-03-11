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
            .Where(p => p.Activo)
            .OrderBy(p => p.Descripcion)
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

    public async Task<IEnumerable<ProductResponse>> SearchAsync(string descripcion)
    {
        var productos = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.Activo &&
                   (p.Descripcion.Contains(descripcion) || p.Codigo.Contains(descripcion)))
            .OrderBy(p => p.Descripcion)
            .ToListAsync();

        return productos.Select(MapToResponse);
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        var producto = new Product
        {
            Codigo = request.Codigo,
            Descripcion = request.Descripcion,
            CodigoProveedor = request.CodigoProveedor,
            CategoryId = request.CategoryId,
            UnitOfMeasureId = request.UnitOfMeasureId,
            PrecioVenta = request.PrecioVenta,
            PrecioCosto = request.PrecioCosto,
            PorcentajeIVA = request.PorcentajeIVA,
            StockMinimo = request.StockMinimo,
            Observaciones = request.Observaciones,
            Activo = true
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
        producto.Codigo = request.Codigo;
        producto.Descripcion = request.Descripcion;
        producto.CodigoProveedor = request.CodigoProveedor;
        producto.CategoryId = request.CategoryId;
        producto.UnitOfMeasureId = request.UnitOfMeasureId;
        producto.PrecioVenta = request.PrecioVenta;
        producto.PrecioCosto = request.PrecioCosto;
        producto.PorcentajeIVA = request.PorcentajeIVA;
        producto.StockMinimo = request.StockMinimo;
        producto.Observaciones = request.Observaciones;

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
        producto.Activo = false;
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
            Codigo = producto.Codigo,
            Descripcion = producto.Descripcion,
            CodigoProveedor = producto.CodigoProveedor,
            PrecioVenta = producto.PrecioVenta,
            PrecioCosto = producto.PrecioCosto,
            PorcentajeIVA = producto.PorcentajeIVA,
            StockMinimo = producto.StockMinimo,
            Observaciones = producto.Observaciones,
            Activo = producto.Activo,
            CategoryId = producto.CategoryId,
            CategoryNombre = producto.Category?.Nombre,
            UnitOfMeasureId = producto.UnitOfMeasureId,
            UnitOfMeasureNombre = producto.UnitOfMeasure?.Nombre,
            UnitOfMeasureCodigo = producto.UnitOfMeasure?.Codigo
        };
    }
}
