using System.ComponentModel.DataAnnotations.Schema;

namespace SPC.Shared.Models;

/// <summary>
/// Stock de productos por depósito
/// </summary>
public class Stock
{
    public int Id { get; set; }
    
    // Relación con Producto
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    
    // Relación con Depósito
    public int DepositoId { get; set; }
    public Deposito Deposito { get; set; } = null!;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Cantidad { get; set; } = 0;
}
