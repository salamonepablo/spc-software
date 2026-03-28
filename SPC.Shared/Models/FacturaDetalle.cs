using System.ComponentModel.DataAnnotations.Schema;

namespace SPC.Shared.Models;

/// <summary>
/// Invoice line item.
/// </summary>
public class InvoiceDetail
{
    public int Id { get; set; }
    
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    
    public int ItemNumber { get; set; }
    
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; } = 1;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; } = 0;
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal DiscountPercent { get; set; } = 0;
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal VATPercent { get; set; } = 21;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; } = 0;
}
