using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPC.Shared.Models;

/// <summary>
/// Product - Batteries and accessories.
/// </summary>
public class Product
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Code is required")]
    [StringLength(50)]
    public string Code { get; set; } = "";
    
    [Required(ErrorMessage = "Description is required")]
    [StringLength(300)]
    public string Description { get; set; } = "";
    
    [StringLength(100)]
    public string? SupplierCode { get; set; }
    
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    
    public int? UnitOfMeasureId { get; set; }
    public UnitOfMeasure? UnitOfMeasure { get; set; }
    
    /// <summary>
    /// Price for invoices (VAT excluded by convention).
    /// Used when creating invoices, credit notes and debit notes.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 9999999.99, ErrorMessage = "Invalid price")]
    public decimal InvoicePrice { get; set; } = 0;
    
    /// <summary>
    /// Price for quotes (VAT included by convention).
    /// Used when creating quotes/estimates.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 9999999.99, ErrorMessage = "Invalid price")]
    public decimal QuotePrice { get; set; } = 0;
    
    /// <summary>
    /// Legacy sale price (kept for compatibility).
    /// Use InvoicePrice or QuotePrice depending on the document.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 9999999.99, ErrorMessage = "Invalid price")]
    public decimal SalePrice { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal CostPrice { get; set; } = 0;
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal VATPercent { get; set; } = 21;  // 21%, 10.5%, 0%
    
    public int MinimumStock { get; set; } = 0;
    
    public bool IsActive { get; set; } = true;
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    // Navigation
    public List<Stock> Stocks { get; set; } = new();
}
