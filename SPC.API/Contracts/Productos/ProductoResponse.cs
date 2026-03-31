namespace SPC.API.Contracts.Products;

/// <summary>
/// Response DTO for Product data returned by API
/// </summary>
public class ProductResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public string? SupplierCode { get; set; }
    public decimal SalePrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal InvoicePrice { get; set; }
    public decimal QuotePrice { get; set; }
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
