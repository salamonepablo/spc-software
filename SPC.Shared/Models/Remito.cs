using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPC.Shared.Models;

/// <summary>
/// Delivery note header.
/// </summary>
public class DeliveryNote
{
    public int Id { get; set; }
    
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
    
    public int PointOfSale { get; set; } = 1;
    
    public long DeliveryNoteNumber { get; set; }
    
    public DateTime DeliveryNoteDate { get; set; } = DateTime.Now;
    
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    
    public int? SalesRepId { get; set; }
    public SalesRep? SalesRep { get; set; }
    
    [StringLength(300)]
    public string? DeliveryAddress { get; set; }
    
    [StringLength(100)]
    public string? DeliveryCity { get; set; }
    
    /// <summary>Business unit.</summary>
    [StringLength(50)]
    public string? BusinessUnit { get; set; }
    
    /// <summary>Clarification on delivery note.</summary>
    [StringLength(200)]
    public string? Clarification { get; set; }
    
    // Status
    public bool IsInvoiced { get; set; } = false;
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    
    /// <summary>Associated invoice type.</summary>
    [StringLength(1)]
    public string? InvoiceType { get; set; }
    
    public bool IsVoided { get; set; } = false;
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    // Navigation
    public List<DeliveryNoteDetail> Details { get; set; } = new();
}
