using System.ComponentModel.DataAnnotations.Schema;

namespace SPC.Shared.Models;

/// <summary>
/// Stock de productos por depósito
/// </summary>
public class Stock
{
    public int Id { get; set; }
    
    // Relación con Product
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    // Relación con Depósito
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Cantidad { get; set; } = 0;
}
