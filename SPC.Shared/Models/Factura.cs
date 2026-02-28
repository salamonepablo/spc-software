using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPC.Shared.Models;

/// <summary>
/// Factura - Cabecera (FacturaC en Access)
/// </summary>
public class Factura
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(1)]
    public string TipoFactura { get; set; } = "B";  // A o B
    
    public int PuntoVenta { get; set; } = 1;
    
    public long NumeroFactura { get; set; }
    
    public DateTime FechaFactura { get; set; } = DateTime.Now;
    
    // Relación con Cliente
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    
    // Totales
    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal ImporteIVA { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal ImportePercepcionIIBB { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; } = 0;
    
    // AFIP
    [StringLength(20)]
    public string? CAE { get; set; }
    
    public DateTime? FechaVencimientoCAE { get; set; }
    
    // Estado
    public bool Anulada { get; set; } = false;
    
    [StringLength(500)]
    public string? Observaciones { get; set; }
    
    // Navegación
    public List<FacturaDetalle> Detalles { get; set; } = new();
}
