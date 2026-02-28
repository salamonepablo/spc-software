using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPC.Shared.Models;

/// <summary>
/// Producto / Artículo - Baterías y accesorios
/// </summary>
public class Producto
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "El código es requerido")]
    [StringLength(50)]
    public string Codigo { get; set; } = "";
    
    [Required(ErrorMessage = "La descripción es requerida")]
    [StringLength(300)]
    public string Descripcion { get; set; } = "";
    
    [StringLength(100)]
    public string? CodigoProveedor { get; set; }
    
    // Relación con Rubro/Categoría
    public int? RubroId { get; set; }
    public Rubro? Rubro { get; set; }
    
    // Relación con Unidad de Medida
    public int? UnidadMedidaId { get; set; }
    public UnidadMedida? UnidadMedida { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 9999999.99, ErrorMessage = "Precio inválido")]
    public decimal PrecioVenta { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecioCosto { get; set; } = 0;
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal PorcentajeIVA { get; set; } = 21;  // 21%, 10.5%, 0%
    
    public int StockMinimo { get; set; } = 0;
    
    public bool Activo { get; set; } = true;
    
    [StringLength(500)]
    public string? Observaciones { get; set; }
    
    // Navegación
    public List<Stock> Stocks { get; set; } = new();
}
