using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Rubro / Categoría de productos
/// </summary>
public class Rubro
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = "";
    
    [StringLength(300)]
    public string? Descripcion { get; set; }
    
    public bool Activo { get; set; } = true;
    
    // Navegación
    public List<Producto> Productos { get; set; } = new();
}
