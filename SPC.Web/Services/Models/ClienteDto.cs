namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for Customer data from API
/// </summary>
public class CustomerDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = "";
    public string? TradeName { get; set; }
    public string? CUIT { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal CreditLimit { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    
    // Related entity info (flattened)
    public int? TaxConditionId { get; set; }
    public string? TaxConditionDescription { get; set; }
    public string? TaxConditionCode { get; set; }
    public string? InvoiceType { get; set; }
    
    // IIBB data from AFIP padrón
    public decimal IIBBPercent { get; set; }
    public string? IIBBRegistryProvince { get; set; }
    
    public int? SalesRepId { get; set; }
    public string? SalesRepFirstName { get; set; }
    
    public int? SalesZoneId { get; set; }
    public string? SalesZoneName { get; set; }
}
