using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Pago de cliente.
/// Se imputa a Billing (Linea 1) o Budget (Linea 2).
/// </summary>
public class Payment
{
    public int Id { get; set; }
    
    /// <summary>Sucursal</summary>
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
    
    /// <summary>Numero de pago</summary>
    public long PaymentNumber { get; set; }
    
    /// <summary>Fecha del pago</summary>
    public DateTime PaymentDate { get; set; }
    
    /// <summary>Customer</summary>
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    /// <summary>Total abonado</summary>
    public decimal TotalAmount { get; set; }
    
    /// <summary>Linea a la que se imputa (Billing o Budget)</summary>
    public AccountLineType AppliesTo { get; set; }
    
    /// <summary>Esta anulado?</summary>
    public bool IsVoided { get; set; } = false;
    
    /// <summary>Corresponde a (descripcion opcional)</summary>
    [StringLength(200)]
    public string? AppliesToDescription { get; set; }
    
    // Navegacion
    public List<PaymentDetail> Details { get; set; } = new();
}

/// <summary>
/// Detalle de pago (cada forma de pago usada).
/// </summary>
public class PaymentDetail
{
    public int Id { get; set; }
    
    public int PaymentId { get; set; }
    public Payment? Payment { get; set; }
    
    /// <summary>Numero de linea</summary>
    public int LineNumber { get; set; }
    
    /// <summary>Forma de pago</summary>
    public int PaymentMethodId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    
    /// <summary>Importe en esta forma de pago</summary>
    public decimal Amount { get; set; }
    
    /// <summary>Observaciones (nro cheque, descripcion mercaderia, etc)</summary>
    [StringLength(500)]
    public string? Notes { get; set; }
}
