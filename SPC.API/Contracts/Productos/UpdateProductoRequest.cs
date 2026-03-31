using System.ComponentModel.DataAnnotations;

namespace SPC.API.Contracts.Products;

/// <summary>
/// Request DTO for updating an existing Product
/// </summary>
public class UpdateProductRequest
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = "";
    
    [Required]
    [StringLength(300)]
    public string Description { get; set; } = "";
    
    [StringLength(100)]
    public string? SupplierCode { get; set; }
    
    public int? CategoryId { get; set; }
    
    public int? UnitOfMeasureId { get; set; }
    
    public decimal SalePrice { get; set; } = 0;
    
    public decimal CostPrice { get; set; } = 0;
    
    [Range(0, 100)]
    public decimal VATPercent { get; set; } = 21;
    
    public int MinimumStock { get; set; } = 0;
    
    [StringLength(500)]
    public string? Notes { get; set; }
}
