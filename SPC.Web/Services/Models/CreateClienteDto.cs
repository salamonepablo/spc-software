using System.ComponentModel.DataAnnotations;

namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for creating a new Customer
/// </summary>
public class CreateCustomerDto
{
    [Required(ErrorMessage = "La Razón Social es requerida")]
    [StringLength(200, ErrorMessage = "Máximo 200 caracteres")]
    public string CompanyName { get; set; } = "";
    
    [StringLength(200, ErrorMessage = "Máximo 200 caracteres")]
    public string? TradeName { get; set; }
    
    [StringLength(13, ErrorMessage = "Máximo 13 caracteres")]
    public string? CUIT { get; set; }
    
    [StringLength(300, ErrorMessage = "Máximo 300 caracteres")]
    public string? Address { get; set; }
    
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
    public string? City { get; set; }
    
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
    public string? Province { get; set; }
    
    [StringLength(10, ErrorMessage = "Máximo 10 caracteres")]
    public string? PostalCode { get; set; }
    
    [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
    public string? Phone { get; set; }
    
    [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
    public string? Mobile { get; set; }
    
    [StringLength(200, ErrorMessage = "Máximo 200 caracteres")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string? Email { get; set; }
    
    public int? TaxConditionId { get; set; }
    
    public int? SalesRepId { get; set; }
    
    public int? SalesZoneId { get; set; }
    
    [Range(0, 100, ErrorMessage = "Debe estar entre 0 y 100")]
    public decimal DiscountPercent { get; set; } = 0;
    
    [Range(0, double.MaxValue, ErrorMessage = "Debe ser mayor o igual a 0")]
    public decimal CreditLimit { get; set; } = 0;
    
    [StringLength(500, ErrorMessage = "Máximo 500 caracteres")]
    public string? Notes { get; set; }
}
