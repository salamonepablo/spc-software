namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for Producto data from API
/// </summary>
public class ProductoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string? CodigoProveedor { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal PrecioFactura { get; set; }  // Net price for Factura A
    public decimal PrecioPresupuesto { get; set; }  // Final price with VAT for Factura B
    public decimal PorcentajeIVA { get; set; }
    public int StockMinimo { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; }
    
    // Related entity info (flattened)
    public int? RubroId { get; set; }
    public string? RubroNombre { get; set; }
    
    public int? UnidadMedidaId { get; set; }
    public string? UnidadMedidaNombre { get; set; }
    public string? UnidadMedidaCodigo { get; set; }
}
