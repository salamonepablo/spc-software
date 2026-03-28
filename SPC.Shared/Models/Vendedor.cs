using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Sales representative.
/// </summary>
public class SalesRep
{
    public int Id { get; set; }
    
    /// <summary>Employee code (original PK in Access).</summary>
    [Required]
    [StringLength(20)]
    public string EmployeeCode { get; set; } = "";
    
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = "";
    
    [StringLength(100)]
    public string? LastName { get; set; }
    
    [StringLength(13)]
    public string? CUIL { get; set; }
    
    [StringLength(300)]
    public string? Address { get; set; }
    
    [StringLength(100)]
    public string? City { get; set; }
    
    [StringLength(100)]
    public string? Province { get; set; }
    
    [StringLength(10)]
    public string? PostalCode { get; set; }
    
    [StringLength(15)]
    public string? DNI { get; set; }
    
    [StringLength(50)]
    public string? Phone { get; set; }
    
    [StringLength(50)]
    public string? Mobile { get; set; }
    
    [StringLength(200)]
    public string? Email { get; set; }
    
    public DateTime? DateOfBirth { get; set; }
    
    public DateTime? HireDate { get; set; }
    
    [Range(0, 100)]
    public decimal CommissionPercent { get; set; } = 0;
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public List<Customer> Customers { get; set; } = new();
    public List<Warehouse> AssignedWarehouses { get; set; } = new();
}
