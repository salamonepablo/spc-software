using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Remito temporal/casual (RemitoTemp en Access).
/// Para clientes ocasionales, sin cuenta corriente.
/// Lleva numeracion separada de los remitos oficiales.
/// </summary>
public class CasualDeliveryNote
{
    public int Id { get; set; }
    
    /// <summary>Sucursal</summary>
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
    
    /// <summary>Numero de remito temporal (texto para flexibilidad)</summary>
    [Required]
    [StringLength(50)]
    public string DeliveryNoteNumber { get; set; } = "";
    
    /// <summary>Fecha del remito</summary>
    public DateTime DeliveryNoteDate { get; set; }
    
    /// <summary>Cliente (puede ser ocasional)</summary>
    public int? CustomerId { get; set; }
    public Cliente? Customer { get; set; }
    
    /// <summary>Nombre del cliente (si es ocasional sin registro)</summary>
    [StringLength(200)]
    public string? CustomerName { get; set; }
    
    /// <summary>Vendedor</summary>
    public int? SalesRepId { get; set; }
    public Vendedor? SalesRep { get; set; }
    
    /// <summary>Unidad de negocio</summary>
    [StringLength(50)]
    public string? BusinessUnit { get; set; }
    
    /// <summary>Factura asociada (si se facturo)</summary>
    public int? InvoiceId { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    // Navegacion
    public List<CasualDeliveryNoteDetail> Details { get; set; } = new();
}

/// <summary>
/// Detalle de remito temporal.
/// </summary>
public class CasualDeliveryNoteDetail
{
    public int Id { get; set; }
    
    public int CasualDeliveryNoteId { get; set; }
    public CasualDeliveryNote? CasualDeliveryNote { get; set; }
    
    public int ItemNumber { get; set; }
    
    public int ProductId { get; set; }
    public Producto? Product { get; set; }
    
    public decimal Quantity { get; set; }
    
    [StringLength(20)]
    public string? UnitOfMeasure { get; set; }
}
