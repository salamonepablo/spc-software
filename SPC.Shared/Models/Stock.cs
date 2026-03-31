using System.ComponentModel.DataAnnotations.Schema;

namespace SPC.Shared.Models;

/// <summary>
/// Product stock per warehouse.
/// </summary>
public class Stock
{
    public int Id { get; set; }
    
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; } = 0;
}
