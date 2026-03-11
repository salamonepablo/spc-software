using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Warehouse / Almacen.
/// Puede ser un deposito fijo o una camioneta de vendedor.
/// </summary>
public class Warehouse
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = "";
    
    [StringLength(300)]
    public string? Direccion { get; set; }
    
    /// <summary>
    /// SalesRep asociado (para camionetas de reparto).
    /// Si es null, es un deposito fijo.
    /// </summary>
    public int? SalesRepAsociadoId { get; set; }
    public SalesRep? SalesRepAsociado { get; set; }
    
    public bool Activo { get; set; } = true;
    
    // Navegacion
    public List<Stock> Stocks { get; set; } = new();
}
