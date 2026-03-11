using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPC.Shared.Models;

/// <summary>
/// DeliveryNote oficial - Cabecera (DeliveryNoteC en Access).
/// Albaran de entrega de mercaderia.
/// </summary>
public class DeliveryNote
{
    public int Id { get; set; }
    
    /// <summary>Sucursal</summary>
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
    
    public int PuntoVenta { get; set; } = 1;
    
    public long NumeroDeliveryNote { get; set; }
    
    public DateTime FechaDeliveryNote { get; set; } = DateTime.Now;
    
    // Relacion con Customer
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    
    // SalesRep
    public int? SalesRepId { get; set; }
    public SalesRep? SalesRep { get; set; }
    
    // Direccion de entrega (puede ser diferente a la del cliente)
    [StringLength(300)]
    public string? DireccionEntrega { get; set; }
    
    [StringLength(100)]
    public string? LocalidadEntrega { get; set; }
    
    /// <summary>Unidad de negocio</summary>
    [StringLength(50)]
    public string? UnidadNegocio { get; set; }
    
    /// <summary>Aclaracion en remito</summary>
    [StringLength(200)]
    public string? Aclaracion { get; set; }
    
    // Estado
    public bool Facturado { get; set; } = false;
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    
    /// <summary>Tipo de factura asociada</summary>
    [StringLength(1)]
    public string? TipoFactura { get; set; }
    
    public bool Anulado { get; set; } = false;
    
    [StringLength(500)]
    public string? Observaciones { get; set; }
    
    // Navegacion
    public List<DeliveryNoteDetail> Detalles { get; set; } = new();
}
