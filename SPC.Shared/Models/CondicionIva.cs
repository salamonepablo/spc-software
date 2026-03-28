using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Tax condition - Responsable Inscripto, Monotributo, etc.
/// </summary>
public class TaxCondition
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(5)]
    public string Code { get; set; } = "";  // RI, MO, CF, EX, etc.
    
    [Required]
    [StringLength(100)]
    public string Description { get; set; } = "";  // Responsable Inscripto, Monotributo, etc.
    
    /// <summary>Invoice type determined by this tax condition (A or B).</summary>
    [StringLength(1)]
    public string InvoiceType { get; set; } = "B";
    
    // Navigation
    public List<Customer> Customers { get; set; } = new();
}
