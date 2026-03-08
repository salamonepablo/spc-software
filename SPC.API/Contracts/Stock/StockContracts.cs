namespace SPC.API.Contracts.Stock;

/// <summary>
/// Response DTO for stock query - shows stock by product and warehouse
/// </summary>
public record StockResponse
{
    public int Id { get; init; }
    public int ProductoId { get; init; }
    public string ProductoCodigo { get; init; } = "";
    public string ProductoDescripcion { get; init; } = "";
    public int DepositoId { get; init; }
    public string DepositoNombre { get; init; } = "";
    public decimal Cantidad { get; init; }
    public decimal StockMinimo { get; init; }
    public bool BajoMinimo => Cantidad < StockMinimo;
}

/// <summary>
/// Summary DTO for stock by product (all warehouses combined)
/// </summary>
public record StockResumenResponse
{
    public int ProductoId { get; init; }
    public string ProductoCodigo { get; init; } = "";
    public string ProductoDescripcion { get; init; } = "";
    public string? RubroNombre { get; init; }
    public decimal StockTotal { get; init; }
    public int StockMinimo { get; init; }
    public bool BajoMinimo => StockTotal < StockMinimo;
    public decimal PrecioVenta { get; init; }
    public decimal ValorStock => StockTotal * PrecioVenta;
}
