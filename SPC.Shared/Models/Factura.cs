using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPC.Shared.Models;

/// <summary>
/// Factura - Cabecera (FacturaC en Access).
/// Documento fiscal que genera saldo en Billing (Linea 1).
/// </summary>
public class Factura
{
    public int Id { get; set; }
    
    /// <summary>Sucursal que emite</summary>
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
    
    [Required]
    [StringLength(1)]
    public string TipoFactura { get; set; } = "B";  // A o B
    
    public int PuntoVenta { get; set; } = 1;
    
    public long NumeroFactura { get; set; }
    
    public DateTime FechaFactura { get; set; } = DateTime.Now;
    
    // Relacion con Cliente
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    
    // Vendedor
    public int? VendedorId { get; set; }
    public Vendedor? Vendedor { get; set; }
    
    // Totales
    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; } = 0;
    
    /// <summary>Porcentaje IVA aplicado</summary>
    public decimal PorcentajeIVA { get; set; } = 21;
    
    /// <summary>
    /// Importe IVA discriminado (Factura A).
    /// En Factura B este campo es 0 porque el IVA está contenido en el precio.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal ImporteIVA { get; set; } = 0;
    
    /// <summary>
    /// IVA Contenido en el precio final (solo Factura B).
    /// Requerido por Ley 27.743 - Régimen de Transparencia Fiscal.
    /// En Factura A este campo es 0.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal IVAContenido { get; set; } = 0;
    
    /// <summary>Alicuota IIBB (percepciones)</summary>
    public decimal AlicuotaIIBB { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal ImportePercepcionIIBB { get; set; } = 0;
    
    /// <summary>Porcentaje descuento</summary>
    public decimal PorcentajeDescuento { get; set; } = 0;
    
    /// <summary>Importe descuento</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal ImporteDescuento { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; } = 0;
    
    // AFIP
    [StringLength(20)]
    public string? CAE { get; set; }
    
    public DateTime? FechaVencimientoCAE { get; set; }
    
    /// <summary>Condicion de venta</summary>
    [StringLength(50)]
    public string? CondicionVenta { get; set; }
    
    /// <summary>Forma de pago</summary>
    public int? FormaPago { get; set; }
    
    /// <summary>Unidad de negocio</summary>
    [StringLength(50)]
    public string? UnidadNegocio { get; set; }
    
    /// <summary>Remito asociado</summary>
    public int? RemitoId { get; set; }
    
    /// <summary>Aclaracion en factura</summary>
    [StringLength(200)]
    public string? Aclaracion { get; set; }
    
    // Estado
    public bool Anulada { get; set; } = false;
    
    [StringLength(500)]
    public string? Observaciones { get; set; }
    
    // Navegacion
    public List<FacturaDetalle> Detalles { get; set; } = new();
    public List<Remito> Remitos { get; set; } = new();
    public List<CreditNote> CreditNotes { get; set; } = new();
}
