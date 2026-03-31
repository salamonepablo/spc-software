using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Sucursal de ventas.
/// Ej: 2 = Calle (vendedores de ruta), 5 = Distribuidora (oficina)
/// </summary>
public class Branch
{
    public int Id { get; set; }
    
    /// <summary>Codigo corto (ej: "CALLE", "DISTRIB")</summary>
    [Required]
    [StringLength(20)]
    public string Code { get; set; } = "";
    
    /// <summary>Nombre descriptivo</summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";
    
    /// <summary>Punto de venta AFIP asociado</summary>
    public int PointOfSale { get; set; }
    
    public bool IsActive { get; set; } = true;
}
