using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Depósito / Almacén
/// </summary>
public class Deposito
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = "";
    
    [StringLength(300)]
    public string? Direccion { get; set; }
    
    public bool Activo { get; set; } = true;
    
    // Navegación
    public List<Stock> Stocks { get; set; } = new();
}
