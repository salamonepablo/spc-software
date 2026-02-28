using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Zona de Venta - Agrupación geográfica de clientes
/// </summary>
public class ZonaVenta
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = "";
    
    [StringLength(300)]
    public string? Descripcion { get; set; }
    
    public bool Activa { get; set; } = true;
    
    // Navegación
    public List<Cliente> Clientes { get; set; } = new();
}
