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
            .Where(s => s.Product.IsActive && s.Warehouse.IsActive)
            .OrderBy(s => s.Product.Description)
            .ThenBy(s => s.Warehouse.Name)
            .ToListAsync();

        return stocks.Select(s => new StockResponse
        {
            Id = s.Id,
            ProductId = s.ProductId,
            ProductCode = s.Product.Code,
            ProductDescription = s.Product.Description,
            WarehouseId = s.WarehouseId,
            WarehouseName = s.Warehouse.Name,
            Quantity = s.Quantity,
            MinimumStock = s.Product.MinimumStock
        });
    }

    public async Task<IEnumerable<StockResumenResponse>> GetResumenAsync()
    {
        var productos = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Stocks)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Description)
            .ToListAsync();

        return productos.Select(p => new StockResumenResponse
        {
            ProductId = p.Id,
            ProductCode = p.Code,
            ProductDescription = p.Description,
            CategoryName = p.Category?.Name,
            StockTotal = p.Stocks.Sum(s => s.Quantity),
            MinimumStock = p.MinimumStock,
            SalePrice = p.SalePrice
        });
    }

    public async Task<IEnumerable<StockResponse>> GetByProductAsync(int productoId)
    {
        var stocks = await _db.Stocks
            .Include(s => s.Product)
            .Include(s => s.Warehouse)
            .Where(s => s.ProductId == productoId && s.Warehouse.IsActive)
            .OrderBy(s => s.Warehouse.Name)
            .ToListAsync();

        return stocks.Select(s => new StockResponse
        {
            Id = s.Id,
            ProductId = s.ProductId,
            ProductCode = s.Product.Code,
            ProductDescription = s.Product.Description,
            WarehouseId = s.WarehouseId,
            WarehouseName = s.Warehouse.Name,
            Quantity = s.Quantity,
            MinimumStock = s.Product.MinimumStock
        });
    }

    public async Task<IEnumerable<StockResponse>> GetByWarehouseAsync(int depositoId)
    {
        var stocks = await _db.Stocks
            .Include(s => s.Product)
            .Include(s => s.Warehouse)
            .Where(s => s.WarehouseId == depositoId && s.Product.IsActive)
            .OrderBy(s => s.Product.Description)
            .ToListAsync();

        return stocks.Select(s => new StockResponse
        {
            Id = s.Id,
            ProductId = s.ProductId,
            ProductCode = s.Product.Code,
            ProductDescription = s.Product.Description,
            WarehouseId = s.WarehouseId,
            WarehouseName = s.Warehouse.Name,
            Quantity = s.Quantity,
            MinimumStock = s.Product.MinimumStock
        });
    }

    public async Task<IEnumerable<StockResumenResponse>> GetBajoMinimoAsync()
    {
        var productos = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Stocks)
            .Where(p => p.IsActive && p.MinimumStock > 0)
            .ToListAsync();

        return productos
            .Where(p => p.Stocks.Sum(s => s.Quantity) < p.MinimumStock)
            .Select(p => new StockResumenResponse
            {
                ProductId = p.Id,
                ProductCode = p.Code,
                ProductDescription = p.Description,
                CategoryName = p.Category?.Name,
                StockTotal = p.Stocks.Sum(s => s.Quantity),
                MinimumStock = p.MinimumStock,
                SalePrice = p.SalePrice
            })
            .OrderBy(p => p.ProductDescription);
    }

    public async Task<IEnumerable<StockResumenResponse>> SearchAsync(string termino)
    {
        var productos = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Stocks)
            .Where(p => p.IsActive &&
                   (p.Code.Contains(termino) || p.Description.Contains(termino)))
            .OrderBy(p => p.Description)
            .ToListAsync();

        return productos.Select(p => new StockResumenResponse
        {
            ProductId = p.Id,
            ProductCode = p.Code,
            ProductDescription = p.Description,
            CategoryName = p.Category?.Name,
            StockTotal = p.Stocks.Sum(s => s.Quantity),
            MinimumStock = p.MinimumStock,
            SalePrice = p.SalePrice
        });
    }
}
