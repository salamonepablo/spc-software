using System.ComponentModel.DataAnnotations.Schema;

namespace SPC.Shared.Models;

/// <summary>
/// Invoice Detalle - Items (InvoiceD en Access)
/// </summary>
public class InvoiceDetail
{
    public int Id { get; set; }
    
    // Relación con Invoice
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    
    public int ItemNumero { get; set; }  // Número de línea
    
    // Relación con Product
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
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
