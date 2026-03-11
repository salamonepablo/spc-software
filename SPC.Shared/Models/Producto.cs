using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPC.Shared.Models;

/// <summary>
/// Product / Artículo - Baterías y accesorios
/// </summary>
public class Product
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
    
    // Relación con Category/Categoría
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    
    // Relación con Unidad de Medida
    public int? UnitOfMeasureId { get; set; }
    public UnitOfMeasure? UnitOfMeasure { get; set; }
    
    /// <summary>
    /// Precio para facturas (sin IVA incluido por convención).
    /// Este precio se usa al crear facturas, notas de crédito y débito.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 9999999.99, ErrorMessage = "Precio inválido")]
    public decimal PrecioInvoice { get; set; } = 0;
    
    /// <summary>
    /// Precio para presupuestos (con IVA incluido por convención).
    /// Este precio se usa al crear presupuestos/cotizaciones.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 9999999.99, ErrorMessage = "Precio inválido")]
    public decimal PrecioQuote { get; set; } = 0;
    
    /// <summary>
    /// Precio de venta legacy (se mantiene para compatibilidad).
    /// Usar PrecioInvoice o PrecioQuote según el documento.
    /// </summary>
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
