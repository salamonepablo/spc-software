using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Nota de Debito Interna (NDI).
/// Documento interno para ajustes de saldo (ej: inflacion, correcciones).
/// NO es fiscal, NO lleva CAE.
/// Genera saldo en Budget (Linea 2).
/// Lleva tipo A o B y numeracion propia separada de ND fiscal.
/// </summary>
public class InternalDebitNote
{
    public int Id { get; set; }
    
    /// <summary>Tipo (A o B) - por compatibilidad con sistema anterior</summary>
    public VoucherType VoucherType { get; set; }
    
    /// <summary>Sucursal</summary>
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
    
    /// <summary>Numero de debito interno (numeracion propia)</summary>
    public long InternalDebitNumber { get; set; }
    
    /// <summary>Fecha de emision</summary>
    public DateTime DebitDate { get; set; }
    
    /// <summary>Customer</summary>
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    /// <summary>SalesRep</summary>
    public int? SalesRepId { get; set; }
    public SalesRep? SalesRep { get; set; }
    
    /// <summary>Subtotal (sin IVA - es documento interno)</summary>
    public decimal Subtotal { get; set; }
    
    /// <summary>Porcentaje descuento</summary>
    public decimal DiscountPercent { get; set; } = 0;
    
    /// <summary>Importe descuento</summary>
    public decimal DiscountAmount { get; set; } = 0;
    
    /// <summary>Total del debito interno</summary>
    public decimal Total { get; set; }
    
    /// <summary>Condicion de venta</summary>
    [StringLength(50)]
    public string? SalesCondition { get; set; }
    
    /// <summary>Unidad de negocio</summary>
    [StringLength(50)]
    public string? BusinessUnit { get; set; }
    
    /// <summary>Numero de remito asociado</summary>
    public int? DeliveryNoteId { get; set; }
    
    /// <summary>Esta anulado?</summary>
    public bool IsVoided { get; set; } = false;
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    // Navegacion
    public List<InternalDebitNoteDetail> Details { get; set; } = new();
}

/// <summary>
/// Detalle de nota de debito interna.
/// </summary>
public class InternalDebitNoteDetail
{
    public int Id { get; set; }
    
    public int InternalDebitNoteId { get; set; }
    public InternalDebitNote? InternalDebitNote { get; set; }
    
    public int ItemNumber { get; set; }
    
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    
    public decimal Quantity { get; set; }
    
    public decimal UnitPrice { get; set; }
    
    public decimal DiscountPercent { get; set; } = 0;
    
    public decimal DiscountAmount { get; set; } = 0;
    
    public decimal Subtotal { get; set; }
    
    [StringLength(20)]
    public string? UnitOfMeasure { get; set; }
}
