using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Condición ante IVA - Responsable Inscripto, Monotributo, etc.
/// </summary>
public class CondicionIva
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(5)]
    public string Codigo { get; set; } = "";  // RI, MO, CF, EX, etc.
    
    [Required]
    [StringLength(100)]
    public string Descripcion { get; set; } = "";  // Responsable Inscripto, Monotributo, etc.
    
    // Tipo de factura que corresponde
    [StringLength(1)]
    public string TipoFactura { get; set; } = "B";  // A o B
    
    // Navegación
    public List<Cliente> Clientes { get; set; } = new();
}
