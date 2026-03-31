using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Customer entity.
/// </summary>
public class Customer
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Company name is required")]
    [StringLength(200, ErrorMessage = "Max 200 characters")]
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
    [EmailAddress(ErrorMessage = "Invalid email")]
    public string? Email { get; set; }
    
    // Relationship with TaxCondition
    public int? TaxConditionId { get; set; }
    public TaxCondition? TaxCondition { get; set; }
    
    // Relationship with SalesRep
    public int? SalesRepId { get; set; }
    public SalesRep? SalesRep { get; set; }
    
    // Relationship with SalesZone
    public int? SalesZoneId { get; set; }
    public SalesZone? SalesZone { get; set; }
    
    [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100")]
    public decimal DiscountPercent { get; set; } = 0;
    
    public decimal CreditLimit { get; set; } = 0;
    
    // ===================================
    // IIBB Perception (from AFIP/ARCA registry)
    // ===================================
    
    /// <summary>
    /// IIBB perception rate for this customer.
    /// From ARBA/AGIP/etc. registry by province.
    /// 0 = Exempt or not applicable.
    /// </summary>
    [Range(0, 100, ErrorMessage = "IIBB rate must be between 0 and 100")]
    public decimal IIBBPercent { get; set; } = 0;
    
    /// <summary>
    /// Province code from the IIBB registry applicable to this customer.
    /// E.g.: "BA" (Buenos Aires/ARBA), "CABA" (AGIP), etc.
    /// </summary>
    [StringLength(10)]
    public string? IIBBRegistryProvince { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    
    // Navigation
    public List<Invoice> Invoices { get; set; } = new();
    public List<DeliveryNote> DeliveryNotes { get; set; } = new();
    public List<CustomerAddress> DeliveryAddresses { get; set; } = new();
    public List<Quote> Quotes { get; set; } = new();
    public List<CreditNote> CreditNotes { get; set; } = new();
    public List<DebitNote> DebitNotes { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}
