using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Unidad de Medida - Unidades, Cajas, etc.
/// </summary>
public class UnidadMedida
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(10)]
    public string Codigo { get; set; } = "";  // UN, CJ, KG, etc.
    
    [Required]
    [StringLength(50)]
    public string Nombre { get; set; } = "";  // Unidades, Cajas, Kilogramos
    
    // Navegación
    public List<Producto> Productos { get; set; } = new();
}
