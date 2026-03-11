using System.ComponentModel.DataAnnotations;

namespace SPC.API.Contracts.Customers;

/// <summary>
/// Request DTO for updating an existing Customer
/// </summary>
public class UpdateCustomerRequest
{
    [Required]
    [StringLength(200)]
    public string RazonSocial { get; set; } = "";
    
    [StringLength(200)]
    public string? NombreFantasia { get; set; }
    
    [StringLength(13)]
    public string? CUIT { get; set; }
    
    [StringLength(300)]
    public string? Direccion { get; set; }
    
    [StringLength(100)]
    public string? Localidad { get; set; }
    
    [StringLength(100)]
    public string? Provincia { get; set; }
    
    [StringLength(10)]
    public string? CodigoPostal { get; set; }
    
    [StringLength(50)]
    public string? Telefono { get; set; }
    
    [StringLength(50)]
    public string? Celular { get; set; }
    
    [StringLength(200)]
    [EmailAddress]
    public string? Email { get; set; }
    
    public int? TaxConditionId { get; set; }
    
    public int? SalesRepId { get; set; }
    
    public int? SalesZoneId { get; set; }
    
    [Range(0, 100)]
    public decimal PorcentajeDescuento { get; set; } = 0;
    
    public decimal LimiteCredito { get; set; } = 0;
    
    [StringLength(500)]
    public string? Observaciones { get; set; }
}
