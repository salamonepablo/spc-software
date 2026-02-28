using System.ComponentModel.DataAnnotations.Schema;

namespace SPC.Shared.Models;

/// <summary>
/// Factura Detalle - Items (FacturaD en Access)
/// </summary>
public class FacturaDetalle
{
    public int Id { get; set; }
    
    // Relación con Factura
    public int FacturaId { get; set; }
    public Factura Factura { get; set; } = null!;
    
    public int ItemNumero { get; set; }  // Número de línea
    
    // Relación con Producto
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Cantidad { get; set; } = 1;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecioUnitario { get; set; } = 0;
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal PorcentajeDescuento { get; set; } = 0;
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal PorcentajeIVA { get; set; } = 21;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; } = 0;
}
