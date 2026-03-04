using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.Productos;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Service implementation for Producto business operations
/// </summary>
public class ProductosService : IProductosService
{
    private readonly SPCDbContext _db;

    public ProductosService(SPCDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ProductoResponse>> GetAllAsync()
    {
        var productos = await _db.Productos
            .Include(p => p.Rubro)
            .Include(p => p.UnidadMedida)
            .Where(p => p.Activo)
            .OrderBy(p => p.Descripcion)
            .ToListAsync();

        return productos.Select(MapToResponse);
    }

    public async Task<ProductoResponse?> GetByIdAsync(int id)
    {
        var producto = await _db.Productos
            .Include(p => p.Rubro)
            .Include(p => p.UnidadMedida)
            .FirstOrDefaultAsync(p => p.Id == id);

        return producto != null ? MapToResponse(producto) : null;
    }

    public async Task<IEnumerable<ProductoResponse>> SearchAsync(string descripcion)
    {
        var productos = await _db.Productos
            .Include(p => p.Rubro)
            .Where(p => p.Activo &&
                   (p.Descripcion.Contains(descripcion) || p.Codigo.Contains(descripcion)))
            .OrderBy(p => p.Descripcion)
            .ToListAsync();

        return productos.Select(MapToResponse);
    }

    public async Task<ProductoResponse> CreateAsync(CreateProductoRequest request)
    {
        var producto = new Producto
        {
            Codigo = request.Codigo,
            Descripcion = request.Descripcion,
            CodigoProveedor = request.CodigoProveedor,
            RubroId = request.RubroId,
            UnidadMedidaId = request.UnidadMedidaId,
            PrecioVenta = request.PrecioVenta,
            PrecioCosto = request.PrecioCosto,
            PorcentajeIVA = request.PorcentajeIVA,
            StockMinimo = request.StockMinimo,
            Observaciones = request.Observaciones,
            Activo = true
        };

        _db.Productos.Add(producto);
        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(producto).Reference(p => p.Rubro).LoadAsync();
        await _db.Entry(producto).Reference(p => p.UnidadMedida).LoadAsync();

        return MapToResponse(producto);
    }

    public async Task<ProductoResponse?> UpdateAsync(int id, UpdateProductoRequest request)
    {
        var producto = await _db.Productos.FindAsync(id);

        if (producto == null)
            return null;

        // Update properties
        producto.Codigo = request.Codigo;
        producto.Descripcion = request.Descripcion;
        producto.CodigoProveedor = request.CodigoProveedor;
        producto.RubroId = request.RubroId;
        producto.UnidadMedidaId = request.UnidadMedidaId;
        producto.PrecioVenta = request.PrecioVenta;
        producto.PrecioCosto = request.PrecioCosto;
        producto.PorcentajeIVA = request.PorcentajeIVA;
        producto.StockMinimo = request.StockMinimo;
        producto.Observaciones = request.Observaciones;

        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(producto).Reference(p => p.Rubro).LoadAsync();
        await _db.Entry(producto).Reference(p => p.UnidadMedida).LoadAsync();

        return MapToResponse(producto);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var producto = await _db.Productos.FindAsync(id);

        if (producto == null)
            return false;

        // Soft delete
        producto.Activo = false;
        await _db.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Maps a Producto entity to ProductoResponse DTO
    /// </summary>
    private static ProductoResponse MapToResponse(Producto producto)
    {
        return new ProductoResponse
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
            RubroId = producto.RubroId,
            RubroNombre = producto.Rubro?.Nombre,
            UnidadMedidaId = producto.UnidadMedidaId,
            UnidadMedidaNombre = producto.UnidadMedida?.Nombre,
            UnidadMedidaCodigo = producto.UnidadMedida?.Codigo
        };
    }
}
