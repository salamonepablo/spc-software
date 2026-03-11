using System.ComponentModel.DataAnnotations.Schema;

namespace SPC.Shared.Models;

/// <summary>
/// DeliveryNote Detalle - Items
/// </summary>
public class DeliveryNoteDetail
{
    public int Id { get; set; }
    
    // Relación con DeliveryNote
    public int DeliveryNoteId { get; set; }
    public DeliveryNote DeliveryNote { get; set; } = null!;
    
    public int ItemNumero { get; set; }
    
    // Relación con Product
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Cantidad { get; set; } = 1;
}
