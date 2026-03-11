using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Category / Categoría de productos
/// </summary>
public class Category
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = "";
    
    [StringLength(300)]
    public string? Descripcion { get; set; }
    
    public bool Activo { get; set; } = true;
    
    // Navegación
    public List<Product> Products { get; set; } = new();
}
