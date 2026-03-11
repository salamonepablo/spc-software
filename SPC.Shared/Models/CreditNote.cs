using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Nota de Credito.
/// Documento fiscal que reduce el saldo en Billing (Linea 1).
/// </summary>
public class CreditNote
{
    public int Id { get; set; }
    
    /// <summary>Tipo de comprobante (A o B)</summary>
    public VoucherType VoucherType { get; set; }
    
    /// <summary>Sucursal / Punto de venta</summary>
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
    
    /// <summary>Punto de venta AFIP</summary>
    public int PointOfSale { get; set; }
    
    /// <summary>Numero de nota de credito</summary>
    public long CreditNoteNumber { get; set; }
    
    /// <summary>Fecha de emision</summary>
    public DateTime CreditNoteDate { get; set; }
    
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
    
    /// <summary>Alicuota IIBB (percepciones)</summary>
    public decimal IIBBPercent { get; set; } = 0;
    
    /// <summary>Importe percepcion IIBB</summary>
    public decimal IIBBAmount { get; set; } = 0;
    
    /// <summary>Porcentaje descuento</summary>
    public decimal DiscountPercent { get; set; } = 0;
    
    /// <summary>Importe descuento</summary>
    public decimal DiscountAmount { get; set; } = 0;
    
    /// <summary>Total de la nota de credito</summary>
    public decimal Total { get; set; }
    
    /// <summary>CAE (Codigo Autorizacion Electronica AFIP)</summary>
    [StringLength(20)]
    public string? CAE { get; set; }
    
    /// <summary>Fecha vencimiento CAE</summary>
    public DateTime? CAEExpirationDate { get; set; }
    
    /// <summary>Condicion de venta</summary>
    [StringLength(50)]
    public string? SalesCondition { get; set; }
    
    /// <summary>Invoice contra la que se emite la NC</summary>
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    
    /// <summary>Esta anulada?</summary>
    public bool IsVoided { get; set; } = false;
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    // Navegacion
    public List<CreditNoteDetail> Details { get; set; } = new();
}

/// <summary>
/// Detalle de nota de credito.
/// </summary>
public class CreditNoteDetail
{
    public int Id { get; set; }
    
    public int CreditNoteId { get; set; }
    public CreditNote? CreditNote { get; set; }
    
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
