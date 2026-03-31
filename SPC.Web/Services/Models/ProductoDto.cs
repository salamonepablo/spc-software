namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for Product data from API
/// </summary>
public class ProductDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public string? SupplierCode { get; set; }
    public decimal SalePrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal InvoicePrice { get; set; }  // Net price for Invoice A
    public decimal QuotePrice { get; set; }  // Final price with VAT for Invoice B
    public decimal VATPercent { get; set; }
    public int MinimumStock { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    
    // Related entity info (flattened)
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    
    public int? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }
    public string? UnitOfMeasureCode { get; set; }
}
