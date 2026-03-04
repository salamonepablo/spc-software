using System.ComponentModel.DataAnnotations;

namespace SPC.API.Contracts.Productos;

/// <summary>
/// Request DTO for updating an existing Producto
/// </summary>
public class UpdateProductoRequest
{
    [Required]
    [StringLength(50)]
    public string Codigo { get; set; } = "";
    
    [Required]
    [StringLength(300)]
    public string Descripcion { get; set; } = "";
    
    [StringLength(100)]
    public string? CodigoProveedor { get; set; }
    
    public int? RubroId { get; set; }
    
    public int? UnidadMedidaId { get; set; }
    
    public decimal PrecioVenta { get; set; } = 0;
    
    public decimal PrecioCosto { get; set; } = 0;
    
    [Range(0, 100)]
    public decimal PorcentajeIVA { get; set; } = 21;
    
    public int StockMinimo { get; set; } = 0;
    
    [StringLength(500)]
    public string? Observaciones { get; set; }
}
