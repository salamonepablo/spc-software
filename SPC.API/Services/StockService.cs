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
            .Include(s => s.Producto)
            .Include(s => s.Deposito)
            .Where(s => s.Producto.Activo && s.Deposito.Activo)
            .OrderBy(s => s.Producto.Descripcion)
            .ThenBy(s => s.Deposito.Nombre)
            .ToListAsync();

        return stocks.Select(s => new StockResponse
        {
            Id = s.Id,
            ProductoId = s.ProductoId,
            ProductoCodigo = s.Producto.Codigo,
            ProductoDescripcion = s.Producto.Descripcion,
            DepositoId = s.DepositoId,
            DepositoNombre = s.Deposito.Nombre,
            Cantidad = s.Cantidad,
            StockMinimo = s.Producto.StockMinimo
        });
    }

    public async Task<IEnumerable<StockResumenResponse>> GetResumenAsync()
    {
        var productos = await _db.Productos
            .Include(p => p.Rubro)
            .Include(p => p.Stocks)
            .Where(p => p.Activo)
            .OrderBy(p => p.Descripcion)
            .ToListAsync();

        return productos.Select(p => new StockResumenResponse
        {
            ProductoId = p.Id,
            ProductoCodigo = p.Codigo,
            ProductoDescripcion = p.Descripcion,
            RubroNombre = p.Rubro?.Nombre,
            StockTotal = p.Stocks.Sum(s => s.Cantidad),
            StockMinimo = p.StockMinimo,
            PrecioVenta = p.PrecioVenta
        });
    }

    public async Task<IEnumerable<StockResponse>> GetByProductoAsync(int productoId)
    {
        var stocks = await _db.Stocks
            .Include(s => s.Producto)
            .Include(s => s.Deposito)
            .Where(s => s.ProductoId == productoId && s.Deposito.Activo)
            .OrderBy(s => s.Deposito.Nombre)
            .ToListAsync();

        return stocks.Select(s => new StockResponse
        {
            Id = s.Id,
            ProductoId = s.ProductoId,
            ProductoCodigo = s.Producto.Codigo,
            ProductoDescripcion = s.Producto.Descripcion,
            DepositoId = s.DepositoId,
            DepositoNombre = s.Deposito.Nombre,
            Cantidad = s.Cantidad,
            StockMinimo = s.Producto.StockMinimo
        });
    }

    public async Task<IEnumerable<StockResponse>> GetByDepositoAsync(int depositoId)
    {
        var stocks = await _db.Stocks
            .Include(s => s.Producto)
            .Include(s => s.Deposito)
            .Where(s => s.DepositoId == depositoId && s.Producto.Activo)
            .OrderBy(s => s.Producto.Descripcion)
            .ToListAsync();

        return stocks.Select(s => new StockResponse
        {
            Id = s.Id,
            ProductoId = s.ProductoId,
            ProductoCodigo = s.Producto.Codigo,
            ProductoDescripcion = s.Producto.Descripcion,
            DepositoId = s.DepositoId,
            DepositoNombre = s.Deposito.Nombre,
            Cantidad = s.Cantidad,
            StockMinimo = s.Producto.StockMinimo
        });
    }

    public async Task<IEnumerable<StockResumenResponse>> GetBajoMinimoAsync()
    {
        var productos = await _db.Productos
            .Include(p => p.Rubro)
            .Include(p => p.Stocks)
            .Where(p => p.Activo && p.StockMinimo > 0)
            .ToListAsync();

        return productos
            .Where(p => p.Stocks.Sum(s => s.Cantidad) < p.StockMinimo)
            .Select(p => new StockResumenResponse
            {
                ProductoId = p.Id,
                ProductoCodigo = p.Codigo,
                ProductoDescripcion = p.Descripcion,
                RubroNombre = p.Rubro?.Nombre,
                StockTotal = p.Stocks.Sum(s => s.Cantidad),
                StockMinimo = p.StockMinimo,
                PrecioVenta = p.PrecioVenta
            })
            .OrderBy(p => p.ProductoDescripcion);
    }

    public async Task<IEnumerable<StockResumenResponse>> SearchAsync(string termino)
    {
        var productos = await _db.Productos
            .Include(p => p.Rubro)
            .Include(p => p.Stocks)
            .Where(p => p.Activo &&
                   (p.Codigo.Contains(termino) || p.Descripcion.Contains(termino)))
            .OrderBy(p => p.Descripcion)
            .ToListAsync();

        return productos.Select(p => new StockResumenResponse
        {
            ProductoId = p.Id,
            ProductoCodigo = p.Codigo,
            ProductoDescripcion = p.Descripcion,
            RubroNombre = p.Rubro?.Nombre,
            StockTotal = p.Stocks.Sum(s => s.Cantidad),
            StockMinimo = p.StockMinimo,
            PrecioVenta = p.PrecioVenta
        });
    }
}
