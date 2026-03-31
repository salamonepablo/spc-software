using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Unit of measure - Units, Boxes, etc.
/// </summary>
public class UnitOfMeasure
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(10)]
    public string Code { get; set; } = "";  // UN, CJ, KG, etc.
    
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = "";  // Unidades, Cajas, Kilogramos
    
    // Navigation
    public List<Product> Products { get; set; } = new();
}
