using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Vendedor / Empleado de ventas
/// </summary>
public class Vendedor
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = "";
    
    [StringLength(100)]
    public string? Apellido { get; set; }
    
    [StringLength(50)]
    public string? Telefono { get; set; }
    
    [StringLength(200)]
    public string? Email { get; set; }
    
    [Range(0, 100)]
    public decimal PorcentajeComision { get; set; } = 0;
    
    public bool Activo { get; set; } = true;
    
    // Navegación
    public List<Cliente> Clientes { get; set; } = new();
}
