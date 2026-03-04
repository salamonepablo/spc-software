using System.ComponentModel.DataAnnotations;

namespace SPC.API.Contracts.Clientes;

/// <summary>
/// Request DTO for creating a new Cliente
/// </summary>
public class CreateClienteRequest
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
    
    public int? CondicionIvaId { get; set; }
    
    public int? VendedorId { get; set; }
    
    public int? ZonaVentaId { get; set; }
    
    [Range(0, 100)]
    public decimal PorcentajeDescuento { get; set; } = 0;
    
    public decimal LimiteCredito { get; set; } = 0;
    
    [StringLength(500)]
    public string? Observaciones { get; set; }
}
