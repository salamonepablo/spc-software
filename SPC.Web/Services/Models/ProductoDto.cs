namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for Product data from API
/// </summary>
public class ProductDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string? CodigoProveedor { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal PrecioInvoice { get; set; }  // Net price for Invoice A
    public decimal PrecioQuote { get; set; }  // Final price with VAT for Invoice B
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
