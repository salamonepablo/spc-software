namespace SPC.API.Contracts.Products;

/// <summary>
/// Response DTO for Product data returned by API
/// </summary>
public class ProductResponse
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string? CodigoProveedor { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal PorcentajeIVA { get; set; }
    public int StockMinimo { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; }
    
    // Related entity info (flattened)
    public int? CategoryId { get; set; }
    public string? CategoryNombre { get; set; }
    
    public int? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureNombre { get; set; }
    public string? UnitOfMeasureCodigo { get; set; }
}
