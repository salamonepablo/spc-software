using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Sales zone - geographic grouping of customers.
/// </summary>
public class SalesZone
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";
    
    [StringLength(300)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public List<Customer> Customers { get; set; } = new();
}
