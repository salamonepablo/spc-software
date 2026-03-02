using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Presupuesto (Budget/Quote).
/// Documento interno sin IVA, genera saldo en Linea 2 de cuenta corriente.
/// NO se convierte en factura, es una linea de credito paralela.
/// </summary>
public class Quote
{
    public int Id { get; set; }
    
    /// <summary>Sucursal que emite</summary>
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
    
    /// <summary>Numero de presupuesto</summary>
    public long QuoteNumber { get; set; }
    
    /// <summary>Fecha de emision</summary>
    public DateTime QuoteDate { get; set; }
    
    /// <summary>Cliente</summary>
    public int CustomerId { get; set; }
    public Cliente? Customer { get; set; }
    
    /// <summary>Vendedor</summary>
    public int? SalesRepId { get; set; }
    public Vendedor? SalesRep { get; set; }
    
    /// <summary>Subtotal sin descuento</summary>
    public decimal Subtotal { get; set; }
    
    /// <summary>Porcentaje de descuento aplicado</summary>
    public decimal DiscountPercent { get; set; } = 0;
    
    /// <summary>Importe de descuento</summary>
    public decimal DiscountAmount { get; set; } = 0;
    
    /// <summary>Total del presupuesto</summary>
    public decimal Total { get; set; }
    
    /// <summary>Unidad de negocio (opcional)</summary>
    [StringLength(50)]
    public string? BusinessUnit { get; set; }
    
    /// <summary>Esta anulado?</summary>
    public bool IsVoided { get; set; } = false;
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    // Navegacion
    public List<QuoteDetail> Details { get; set; } = new();
}

/// <summary>
/// Detalle de presupuesto.
/// </summary>
public class QuoteDetail
{
    public int Id { get; set; }
    
    public int QuoteId { get; set; }
    public Quote? Quote { get; set; }
    
    /// <summary>Numero de item (1, 2, 3...)</summary>
    public int ItemNumber { get; set; }
    
    public int ProductId { get; set; }
    public Producto? Product { get; set; }
    
    public decimal Quantity { get; set; }
    
    public decimal UnitPrice { get; set; }
    
    public decimal DiscountPercent { get; set; } = 0;
    
    public decimal DiscountAmount { get; set; } = 0;
    
    public decimal Subtotal { get; set; }
    
    [StringLength(20)]
    public string? UnitOfMeasure { get; set; }
}
