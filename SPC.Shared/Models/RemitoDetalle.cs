using System.ComponentModel.DataAnnotations.Schema;

namespace SPC.Shared.Models;

/// <summary>
/// Remito Detalle - Items
/// </summary>
public class RemitoDetalle
{
    public int Id { get; set; }
    
    // Relación con Remito
    public int RemitoId { get; set; }
    public Remito Remito { get; set; } = null!;
    
    public int ItemNumero { get; set; }
    
    // Relación con Producto
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Cantidad { get; set; } = 1;
}
