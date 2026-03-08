namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for stock summary by product
/// </summary>
public class StockResumenDto
{
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = "";
    public string ProductoDescripcion { get; set; } = "";
    public string? RubroNombre { get; set; }
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
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = "";
    public string ProductoDescripcion { get; set; } = "";
    public int DepositoId { get; set; }
    public string DepositoNombre { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal StockMinimo { get; set; }
    public bool BajoMinimo { get; set; }
}

/// <summary>
/// DTO for warehouse dropdown
/// </summary>
public class DepositoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}
