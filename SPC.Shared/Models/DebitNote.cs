using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Nota de Debito fiscal.
/// Documento fiscal que aumenta el saldo en Billing (Linea 1).
/// </summary>
public class DebitNote
{
    public int Id { get; set; }
    
    /// <summary>Tipo de comprobante (A o B)</summary>
    public VoucherType VoucherType { get; set; }
    
    /// <summary>Sucursal / Punto de venta</summary>
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
    
    /// <summary>Punto de venta AFIP</summary>
    public int PointOfSale { get; set; }
    
    /// <summary>Numero de nota de debito</summary>
    public long DebitNoteNumber { get; set; }
    
    /// <summary>Fecha de emision</summary>
    public DateTime DebitNoteDate { get; set; }
    
    /// <summary>Customer</summary>
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    /// <summary>SalesRep</summary>
    public int? SalesRepId { get; set; }
    public SalesRep? SalesRep { get; set; }
    
    /// <summary>Subtotal gravado</summary>
    public decimal Subtotal { get; set; }
    
    /// <summary>Porcentaje IVA</summary>
    public decimal VATPercent { get; set; } = 21;
    
    /// <summary>Importe IVA</summary>
    public decimal VATAmount { get; set; }
    
    /// <summary>Alicuota IIBB</summary>
    public decimal IIBBPercent { get; set; } = 0;
    
    /// <summary>Importe percepcion IIBB</summary>
    public decimal IIBBAmount { get; set; } = 0;
    
    /// <summary>Porcentaje descuento</summary>
    public decimal DiscountPercent { get; set; } = 0;
    
    /// <summary>Importe descuento</summary>
    public decimal DiscountAmount { get; set; } = 0;
    
    /// <summary>Total de la nota de debito</summary>
    public decimal Total { get; set; }
    
    /// <summary>CAE</summary>
    [StringLength(20)]
    public string? CAE { get; set; }
    
    /// <summary>Fecha vencimiento CAE</summary>
    public DateTime? CAEExpirationDate { get; set; }
    
    /// <summary>Condicion de venta</summary>
    [StringLength(50)]
    public string? SalesCondition { get; set; }
    
    /// <summary>Numero de remito asociado (opcional)</summary>
    public int? DeliveryNoteId { get; set; }
    
    /// <summary>Esta anulada?</summary>
    public bool IsVoided { get; set; } = false;
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    // Navegacion
    public List<DebitNoteDetail> Details { get; set; } = new();
}

/// <summary>
/// Detalle de nota de debito.
/// </summary>
public class DebitNoteDetail
{
    public int Id { get; set; }
    
    public int DebitNoteId { get; set; }
    public DebitNote? DebitNote { get; set; }
    
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
