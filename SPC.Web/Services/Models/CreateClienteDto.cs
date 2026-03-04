using System.ComponentModel.DataAnnotations;

namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for creating a new Cliente
/// </summary>
public class CreateClienteDto
{
    [Required(ErrorMessage = "La Razón Social es requerida")]
    [StringLength(200, ErrorMessage = "Máximo 200 caracteres")]
    public string RazonSocial { get; set; } = "";
    
    [StringLength(200, ErrorMessage = "Máximo 200 caracteres")]
    public string? NombreFantasia { get; set; }
    
    [StringLength(13, ErrorMessage = "Máximo 13 caracteres")]
    public string? CUIT { get; set; }
    
    [StringLength(300, ErrorMessage = "Máximo 300 caracteres")]
    public string? Direccion { get; set; }
    
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
    public string? Localidad { get; set; }
    
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
    public string? Provincia { get; set; }
    
    [StringLength(10, ErrorMessage = "Máximo 10 caracteres")]
    public string? CodigoPostal { get; set; }
    
    [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
    public string? Telefono { get; set; }
    
    [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
    public string? Celular { get; set; }
    
    [StringLength(200, ErrorMessage = "Máximo 200 caracteres")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string? Email { get; set; }
    
    public int? CondicionIvaId { get; set; }
    
    public int? VendedorId { get; set; }
    
    public int? ZonaVentaId { get; set; }
    
    [Range(0, 100, ErrorMessage = "Debe estar entre 0 y 100")]
    public decimal PorcentajeDescuento { get; set; } = 0;
    
    [Range(0, double.MaxValue, ErrorMessage = "Debe ser mayor o igual a 0")]
    public decimal LimiteCredito { get; set; } = 0;
    
    [StringLength(500, ErrorMessage = "Máximo 500 caracteres")]
    public string? Observaciones { get; set; }
}
