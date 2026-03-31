using System.ComponentModel.DataAnnotations;

namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for creating a new quote
/// </summary>
public class CreateQuoteDto
{
    [Required]
    public int BranchId { get; set; }
    
    [Required]
    public int CustomerId { get; set; }
    
    public int? SalesRepId { get; set; }
    
    [Range(0, 100)]
    public decimal DiscountPercent { get; set; } = 0;
    
    public string? BusinessUnit { get; set; }
    
    public string? Notes { get; set; }
    
    public List<CreateQuoteDetailDto> Details { get; set; } = new();
}

/// <summary>
/// DTO for quote line item
/// </summary>
public class CreateQuoteDetailDto
{
    public int ProductId { get; set; }
    
    public decimal Quantity { get; set; } = 1;
    
    public decimal? UnitPrice { get; set; }
    
    public decimal DiscountPercent { get; set; } = 0;
}
