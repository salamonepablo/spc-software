using System.ComponentModel.DataAnnotations;

namespace SPC.API.Contracts.Quotes;

// ===========================================
// REQUEST DTOs
// ===========================================

/// <summary>
/// Request DTO for creating a quote/budget
/// </summary>
public record CreateQuoteRequest
{
    /// <summary>Branch ID</summary>
    [Required]
    public int BranchId { get; init; }
    
    /// <summary>Customer ID</summary>
    [Required]
    public int CustomerId { get; init; }
    
    /// <summary>Sales rep ID (optional)</summary>
    public int? SalesRepId { get; init; }
    
    /// <summary>Document-level discount percentage</summary>
    [Range(0, 100)]
    public decimal DiscountPercent { get; init; } = 0;
    
    /// <summary>Business unit (optional)</summary>
    [StringLength(50)]
    public string? BusinessUnit { get; init; }
    
    /// <summary>Notes</summary>
    [StringLength(500)]
    public string? Notes { get; init; }
    
    /// <summary>Quote line items</summary>
    [Required]
    [MinLength(1, ErrorMessage = "El presupuesto debe tener al menos un item")]
    public List<CreateQuoteDetalleRequest> Details { get; init; } = new();
}

/// <summary>
/// Request DTO for quote line item
/// </summary>
public record CreateQuoteDetalleRequest
{
    /// <summary>Product ID</summary>
    [Required]
    public int ProductId { get; init; }
    
    /// <summary>Quantity</summary>
    [Required]
    [Range(0.01, 999999.99, ErrorMessage = "La cantidad debe ser mayor a 0")]
    public decimal Quantity { get; init; }
    
    /// <summary>Unit price (if null, uses product's PrecioQuote)</summary>
    public decimal? UnitPrice { get; init; }
    
    /// <summary>Line-level discount percentage</summary>
    [Range(0, 100)]
    public decimal DiscountPercent { get; init; } = 0;
}

/// <summary>
/// Request DTO for voiding a quote
/// </summary>
public record AnularQuoteRequest
{
    /// <summary>Reason for voiding</summary>
    [Required]
    [StringLength(500)]
    public string Reason { get; init; } = "";
}

// ===========================================
// RESPONSE DTOs
// ===========================================

/// <summary>
/// Response DTO for quote listing
/// </summary>
public record QuoteResponse
{
    public int Id { get; init; }
    public int BranchId { get; init; }
    public string? BranchName { get; init; }
    public long QuoteNumber { get; init; }
    
    /// <summary>Formatted quote number: CALLE-00001234</summary>
    public string NumeroCompleto { get; init; } = "";
    
    public DateTime QuoteDate { get; init; }
    
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = "";
    
    public int? SalesRepId { get; init; }
    public string? SalesRepName { get; init; }
    
    public decimal Subtotal { get; init; }
    public decimal DiscountPercent { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal Total { get; init; }
    
    public bool IsVoided { get; init; }
    
    public int ItemCount { get; init; }
}

/// <summary>
/// Response DTO for quote line item
/// </summary>
public record QuoteDetalleResponse
{
    public int Id { get; init; }
    public int ItemNumber { get; init; }
    public int ProductId { get; init; }
    public string ProductCode { get; init; } = "";
    public string ProductDescription { get; init; } = "";
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal DiscountPercent { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal Subtotal { get; init; }
}

/// <summary>
/// Full quote response with details
/// </summary>
public record QuoteCompletoResponse : QuoteResponse
{
    public string? BusinessUnit { get; init; }
    public string? Notes { get; init; }
    public List<QuoteDetalleResponse> Details { get; init; } = new();
}
