using System.ComponentModel.DataAnnotations;

namespace SPC.API.Contracts.Customers;

/// <summary>
/// Request DTO for creating a new Customer
/// </summary>
public class CreateCustomerRequest
{
    [Required]
    [StringLength(200)]
    public string CompanyName { get; set; } = "";
    
    [StringLength(200)]
    public string? TradeName { get; set; }
    
    [StringLength(13)]
    public string? CUIT { get; set; }
    
    [StringLength(300)]
    public string? Address { get; set; }
    
    [StringLength(100)]
    public string? City { get; set; }
    
    [StringLength(100)]
    public string? Province { get; set; }
    
    [StringLength(10)]
    public string? PostalCode { get; set; }
    
    [StringLength(50)]
    public string? Phone { get; set; }
    
    [StringLength(50)]
    public string? Mobile { get; set; }
    
    [StringLength(200)]
    [EmailAddress]
    public string? Email { get; set; }
    
    public int? TaxConditionId { get; set; }
    
    public int? SalesRepId { get; set; }
    
    public int? SalesZoneId { get; set; }
    
    [Range(0, 100)]
    public decimal DiscountPercent { get; set; } = 0;
    
    public decimal CreditLimit { get; set; } = 0;
    
    [StringLength(500)]
    public string? Notes { get; set; }
}
