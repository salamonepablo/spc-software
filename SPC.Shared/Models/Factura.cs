using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPC.Shared.Models;

/// <summary>
/// Invoice header. Fiscal document that generates balance in billing (Line 1).
/// </summary>
public class Invoice
{
    public int Id { get; set; }
    
    /// <summary>Issuing branch.</summary>
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
    
    [Required]
    [StringLength(1)]
    public string InvoiceType { get; set; } = "B";  // A or B
    
    public int PointOfSale { get; set; } = 1;
    
    public long InvoiceNumber { get; set; }
    
    public DateTime InvoiceDate { get; set; } = DateTime.Now;
    
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    
    public int? SalesRepId { get; set; }
    public SalesRep? SalesRep { get; set; }
    
    // Totals
    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; } = 0;
    
    /// <summary>Applied VAT percentage.</summary>
    public decimal VATPercent { get; set; } = 21;
    
    /// <summary>
    /// Itemized VAT amount (Invoice A).
    /// For Invoice B this is 0 because VAT is included in the price.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal VATAmount { get; set; } = 0;
    
    /// <summary>
    /// VAT included in the final price (Invoice B only).
    /// Required by Law 27.743 - Fiscal Transparency Act.
    /// For Invoice A this is 0.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal IncludedVAT { get; set; } = 0;
    
    /// <summary>IIBB rate (perceptions).</summary>
    public decimal IIBBPercent { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal IIBBPerceptionAmount { get; set; } = 0;
    
    /// <summary>Discount percentage.</summary>
    public decimal DiscountPercent { get; set; } = 0;
    
    /// <summary>Discount amount.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; } = 0;
    
    // AFIP
    [StringLength(20)]
    public string? CAE { get; set; }
    
    public DateTime? CAEExpirationDate { get; set; }
    
    /// <summary>Sales condition.</summary>
    [StringLength(50)]
    public string? SalesCondition { get; set; }
    
    /// <summary>Payment method.</summary>
    public int? PaymentMethodId { get; set; }
    
    /// <summary>Business unit.</summary>
    [StringLength(50)]
    public string? BusinessUnit { get; set; }
    
    /// <summary>Associated delivery note.</summary>
    public int? DeliveryNoteId { get; set; }
    
    /// <summary>Clarification on invoice.</summary>
    [StringLength(200)]
    public string? Clarification { get; set; }
    
    // Status
    public bool IsVoided { get; set; } = false;
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    // Navigation
    public List<InvoiceDetail> Details { get; set; } = new();
    public List<DeliveryNote> DeliveryNotes { get; set; } = new();
    public List<CreditNote> CreditNotes { get; set; } = new();
}
