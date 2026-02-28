using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPC.Shared.Models;

/// <summary>
/// Remito - Cabecera
/// </summary>
public class Remito
{
    public int Id { get; set; }
    
    public int PuntoVenta { get; set; } = 1;
    
    public long NumeroRemito { get; set; }
    
    public DateTime FechaRemito { get; set; } = DateTime.Now;
    
    // Relación con Cliente
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    
    // Dirección de entrega (puede ser diferente a la del cliente)
    [StringLength(300)]
    public string? DireccionEntrega { get; set; }
    
    [StringLength(100)]
    public string? LocalidadEntrega { get; set; }
    
    // Estado
    public bool Facturado { get; set; } = false;
    public int? FacturaId { get; set; }
    
    public bool Anulado { get; set; } = false;
    
    [StringLength(500)]
    public string? Observaciones { get; set; }
    
    // Navegación
    public List<RemitoDetalle> Detalles { get; set; } = new();
}
