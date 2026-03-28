namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for stock summary by product
/// </summary>
public class StockResumenDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductDescription { get; set; } = "";
    public string? CategoryName { get; set; }
    public decimal StockTotal { get; set; }
    public int MinimumStock { get; set; }
    public bool BajoMinimo { get; set; }
    public decimal SalePrice { get; set; }
    public decimal ValorStock { get; set; }
}

/// <summary>
/// DTO for stock by warehouse
/// </summary>
public class StockDetalleDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductDescription { get; set; } = "";
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal MinimumStock { get; set; }
    public bool BajoMinimo { get; set; }
}

/// <summary>
/// DTO for warehouse dropdown
/// </summary>
public class WarehouseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
