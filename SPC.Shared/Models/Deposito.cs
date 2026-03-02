using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Deposito / Almacen.
/// Puede ser un deposito fijo o una camioneta de vendedor.
/// </summary>
public class Deposito
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = "";
    
    [StringLength(300)]
    public string? Direccion { get; set; }
    
    /// <summary>
    /// Vendedor asociado (para camionetas de reparto).
    /// Si es null, es un deposito fijo.
    /// </summary>
    public int? VendedorAsociadoId { get; set; }
    public Vendedor? VendedorAsociado { get; set; }
    
    public bool Activo { get; set; } = true;
    
    // Navegacion
    public List<Stock> Stocks { get; set; } = new();
}
