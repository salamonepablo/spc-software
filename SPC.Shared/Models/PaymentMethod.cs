using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Forma de pago aceptada.
/// Incluye formas especiales como Rezago (baterias viejas) y Mercaderia (canje).
/// </summary>
public class PaymentMethod
{
    public int Id { get; set; }
    
    /// <summary>Codigo corto (EF, CH, TR, TC, RZ, ME)</summary>
    [Required]
    [StringLength(10)]
    public string Code { get; set; } = "";
    
    /// <summary>Descripcion (Efectivo, Cheque, Rezago, etc)</summary>
    [Required]
    [StringLength(100)]
    public string Description { get; set; } = "";
    
    /// <summary>Tipo de forma de pago</summary>
    public PaymentMethodType Type { get; set; } = PaymentMethodType.Cash;
    
    /// <summary>Requiere detalle adicional (ej: nro cheque, descripcion mercaderia)</summary>
    public bool RequiresDetail { get; set; } = false;
    
    public bool IsActive { get; set; } = true;
}
