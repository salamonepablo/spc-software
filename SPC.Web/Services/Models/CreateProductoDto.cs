using System.ComponentModel.DataAnnotations;

namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for creating a new Producto
/// </summary>
public class CreateProductoDto
{
    [Required(ErrorMessage = "El código es requerido")]
    [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
    public string Codigo { get; set; } = "";
    
    [Required(ErrorMessage = "La descripción es requerida")]
    [StringLength(300, ErrorMessage = "Máximo 300 caracteres")]
    public string Descripcion { get; set; } = "";
    
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
    public string? CodigoProveedor { get; set; }
    
    public int? RubroId { get; set; }
    
    public int? UnidadMedidaId { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Debe ser mayor o igual a 0")]
    public decimal PrecioVenta { get; set; } = 0;
    
    [Range(0, double.MaxValue, ErrorMessage = "Debe ser mayor o igual a 0")]
    public decimal PrecioCosto { get; set; } = 0;
    
    [Range(0, 100, ErrorMessage = "Debe estar entre 0 y 100")]
    public decimal PorcentajeIVA { get; set; } = 21;
    
    [Range(0, int.MaxValue, ErrorMessage = "Debe ser mayor o igual a 0")]
    public int StockMinimo { get; set; } = 0;
    
    [StringLength(500, ErrorMessage = "Máximo 500 caracteres")]
    public string? Observaciones { get; set; }
}
