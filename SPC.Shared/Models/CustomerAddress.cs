using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Direcciones adicionales del cliente (entrega, sucursales, etc).
/// La direccion fiscal principal esta en Cliente.
/// </summary>
public class CustomerAddress
{
    public int Id { get; set; }
    
    /// <summary>Cliente al que pertenece</summary>
    public int CustomerId { get; set; }
    public Cliente? Customer { get; set; }
    
    /// <summary>Numero de item (1, 2, 3...)</summary>
    public int ItemNumber { get; set; }
    
    /// <summary>Tipo de direccion</summary>
    public AddressType AddressType { get; set; } = AddressType.Delivery;
    
    [Required]
    [StringLength(300)]
    public string Address { get; set; } = "";
    
    [StringLength(100)]
    public string? City { get; set; }
    
    [StringLength(100)]
    public string? Province { get; set; }
    
    [StringLength(10)]
    public string? PostalCode { get; set; }
    
    [StringLength(100)]
    public string? Country { get; set; }
    
    [StringLength(50)]
    public string? Phone { get; set; }
    
    [StringLength(50)]
    public string? Mobile { get; set; }
    
    [StringLength(200)]
    public string? Email { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    /// <summary>Es la direccion de entrega por defecto?</summary>
    public bool IsDefault { get; set; } = false;
}
