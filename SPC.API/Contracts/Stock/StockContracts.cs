namespace SPC.API.Contracts.Stock;

/// <summary>
/// Response DTO for stock query - shows stock by product and warehouse
/// </summary>
public record StockResponse
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public string ProductCode { get; init; } = "";
    public string ProductDescription { get; init; } = "";
    public int WarehouseId { get; init; }
    public string WarehouseName { get; init; } = "";
    public decimal Quantity { get; init; }
    public decimal MinimumStock { get; init; }
    public bool BajoMinimo => Quantity < MinimumStock;
}

/// <summary>
/// Summary DTO for stock by product (all warehouses combined)
/// </summary>
public record StockResumenResponse
{
    public int ProductId { get; init; }
    public string ProductCode { get; init; } = "";
    public string ProductDescription { get; init; } = "";
    public string? CategoryName { get; init; }
    public decimal StockTotal { get; init; }
    public int MinimumStock { get; init; }
    public bool BajoMinimo => StockTotal < MinimumStock;
    public decimal SalePrice { get; init; }
    public decimal ValorStock => StockTotal * SalePrice;
}
