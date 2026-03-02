using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Movimiento interno de stock entre depositos.
/// Ej: Deposito Principal -> Camioneta Vendedor X
/// </summary>
public class StockMovement
{
    public int Id { get; set; }
    
    /// <summary>Numero de movimiento</summary>
    public long MovementNumber { get; set; }
    
    /// <summary>Fecha del movimiento</summary>
    public DateTime MovementDate { get; set; }
    
    /// <summary>Deposito origen</summary>
    public int SourceWarehouseId { get; set; }
    public Deposito? SourceWarehouse { get; set; }
    
    /// <summary>Deposito destino</summary>
    public int DestinationWarehouseId { get; set; }
    public Deposito? DestinationWarehouse { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    // Navegacion
    public List<StockMovementDetail> Details { get; set; } = new();
}

/// <summary>
/// Detalle de movimiento de stock.
/// </summary>
public class StockMovementDetail
{
    public int Id { get; set; }
    
    public int StockMovementId { get; set; }
    public StockMovement? StockMovement { get; set; }
    
    public int ItemNumber { get; set; }
    
    public int ProductId { get; set; }
    public Producto? Product { get; set; }
    
    public decimal Quantity { get; set; }
}
