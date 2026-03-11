namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for stock summary by product
/// </summary>
public class StockResumenDto
{
    public int ProductId { get; set; }
    public string ProductCodigo { get; set; } = "";
    public string ProductDescripcion { get; set; } = "";
    public string? CategoryNombre { get; set; }
    public decimal StockTotal { get; set; }
    public int StockMinimo { get; set; }
    public bool BajoMinimo { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal ValorStock { get; set; }
}

/// <summary>
/// DTO for stock by warehouse
/// </summary>
public class StockDetalleDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductCodigo { get; set; } = "";
    public string ProductDescripcion { get; set; } = "";
    public int WarehouseId { get; set; }
    public string WarehouseNombre { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal StockMinimo { get; set; }
    public bool BajoMinimo { get; set; }
}

/// <summary>
/// DTO for warehouse dropdown
/// </summary>
public class WarehouseDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}
