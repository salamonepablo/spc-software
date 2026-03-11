using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.Stock;
using SPC.API.Data;

namespace SPC.API.Services;

/// <summary>
/// Stock service implementation for stock queries
/// </summary>
public class StockService : IStockService
{
    private readonly SPCDbContext _db;

    public StockService(SPCDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<StockResponse>> GetAllAsync()
    {
        var stocks = await _db.Stocks
            .Include(s => s.Product)
            .Include(s => s.Warehouse)
            .Where(s => s.Product.Activo && s.Warehouse.Activo)
            .OrderBy(s => s.Product.Descripcion)
            .ThenBy(s => s.Warehouse.Nombre)
            .ToListAsync();

        return stocks.Select(s => new StockResponse
        {
            Id = s.Id,
            ProductId = s.ProductId,
            ProductCodigo = s.Product.Codigo,
            ProductDescripcion = s.Product.Descripcion,
            WarehouseId = s.WarehouseId,
            WarehouseNombre = s.Warehouse.Nombre,
            Cantidad = s.Cantidad,
            StockMinimo = s.Product.StockMinimo
        });
    }

    public async Task<IEnumerable<StockResumenResponse>> GetResumenAsync()
    {
        var productos = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Stocks)
            .Where(p => p.Activo)
            .OrderBy(p => p.Descripcion)
            .ToListAsync();

        return productos.Select(p => new StockResumenResponse
        {
            ProductId = p.Id,
            ProductCodigo = p.Codigo,
            ProductDescripcion = p.Descripcion,
            CategoryNombre = p.Category?.Nombre,
            StockTotal = p.Stocks.Sum(s => s.Cantidad),
            StockMinimo = p.StockMinimo,
            PrecioVenta = p.PrecioVenta
        });
    }

    public async Task<IEnumerable<StockResponse>> GetByProductAsync(int productoId)
    {
        var stocks = await _db.Stocks
            .Include(s => s.Product)
            .Include(s => s.Warehouse)
            .Where(s => s.ProductId == productoId && s.Warehouse.Activo)
            .OrderBy(s => s.Warehouse.Nombre)
            .ToListAsync();

        return stocks.Select(s => new StockResponse
        {
            Id = s.Id,
            ProductId = s.ProductId,
            ProductCodigo = s.Product.Codigo,
            ProductDescripcion = s.Product.Descripcion,
            WarehouseId = s.WarehouseId,
            WarehouseNombre = s.Warehouse.Nombre,
            Cantidad = s.Cantidad,
            StockMinimo = s.Product.StockMinimo
        });
    }

    public async Task<IEnumerable<StockResponse>> GetByWarehouseAsync(int depositoId)
    {
        var stocks = await _db.Stocks
            .Include(s => s.Product)
            .Include(s => s.Warehouse)
            .Where(s => s.WarehouseId == depositoId && s.Product.Activo)
            .OrderBy(s => s.Product.Descripcion)
            .ToListAsync();

        return stocks.Select(s => new StockResponse
        {
            Id = s.Id,
            ProductId = s.ProductId,
            ProductCodigo = s.Product.Codigo,
            ProductDescripcion = s.Product.Descripcion,
            WarehouseId = s.WarehouseId,
            WarehouseNombre = s.Warehouse.Nombre,
            Cantidad = s.Cantidad,
            StockMinimo = s.Product.StockMinimo
        });
    }

    public async Task<IEnumerable<StockResumenResponse>> GetBajoMinimoAsync()
    {
        var productos = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Stocks)
            .Where(p => p.Activo && p.StockMinimo > 0)
            .ToListAsync();

        return productos
            .Where(p => p.Stocks.Sum(s => s.Cantidad) < p.StockMinimo)
            .Select(p => new StockResumenResponse
            {
                ProductId = p.Id,
                ProductCodigo = p.Codigo,
                ProductDescripcion = p.Descripcion,
                CategoryNombre = p.Category?.Nombre,
                StockTotal = p.Stocks.Sum(s => s.Cantidad),
                StockMinimo = p.StockMinimo,
                PrecioVenta = p.PrecioVenta
            })
            .OrderBy(p => p.ProductDescripcion);
    }

    public async Task<IEnumerable<StockResumenResponse>> SearchAsync(string termino)
    {
        var productos = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Stocks)
            .Where(p => p.Activo &&
                   (p.Codigo.Contains(termino) || p.Descripcion.Contains(termino)))
            .OrderBy(p => p.Descripcion)
            .ToListAsync();

        return productos.Select(p => new StockResumenResponse
        {
            ProductId = p.Id,
            ProductCodigo = p.Codigo,
            ProductDescripcion = p.Descripcion,
            CategoryNombre = p.Category?.Nombre,
            StockTotal = p.Stocks.Sum(s => s.Cantidad),
            StockMinimo = p.StockMinimo,
            PrecioVenta = p.PrecioVenta
        });
    }
}
