using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Warehouse. Can be a fixed warehouse or a sales rep delivery truck.
/// </summary>
public class Warehouse
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";
    
    [StringLength(300)]
    public string? Address { get; set; }
    
    /// <summary>
    /// Associated sales rep (for delivery trucks).
    /// Null means it is a fixed warehouse.
    /// </summary>
    public int? AssociatedSalesRepId { get; set; }
    public SalesRep? AssociatedSalesRep { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public List<Stock> Stocks { get; set; } = new();
}
