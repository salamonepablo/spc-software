using System.ComponentModel.DataAnnotations.Schema;

namespace SPC.Shared.Models;

/// <summary>
/// Delivery note line item.
/// </summary>
public class DeliveryNoteDetail
{
    public int Id { get; set; }
    
    public int DeliveryNoteId { get; set; }
    public DeliveryNote DeliveryNote { get; set; } = null!;
    
    public int ItemNumber { get; set; }
    
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; } = 1;
}
