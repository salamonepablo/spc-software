using System.ComponentModel.DataAnnotations;

namespace SPC.API.Contracts.DebitNotes;

// ===========================================
// REQUEST DTOs
// ===========================================

/// <summary>
/// Request DTO for creating a debit note
/// </summary>
public record CreateDebitNoteRequest
{
    /// <summary>Branch ID</summary>
    [Required]
    public int BranchId { get; init; }
    
    /// <summary>Voucher type: A or B</summary>
    [Required]
    [StringLength(1)]
    public string VoucherType { get; init; } = "B";
    
    /// <summary>Customer ID</summary>
    [Required]
    public int CustomerId { get; init; }
    
    /// <summary>Sales rep ID (optional)</summary>
    public int? SalesRepId { get; init; }
    
    /// <summary>Document-level discount percentage</summary>
    [Range(0, 100)]
    public decimal DiscountPercent { get; init; } = 0;
    
    /// <summary>IIBB perception rate</summary>
    [Range(0, 100)]
    public decimal IIBBPercent { get; init; } = 0;
    
    /// <summary>Sales condition</summary>
    [StringLength(50)]
    public string? SalesCondition { get; init; }
    
    /// <summary>Notes</summary>
    [StringLength(500)]
    public string? Notes { get; init; }
    
    /// <summary>Debit note line items</summary>
    [Required]
    [MinLength(1, ErrorMessage = "La nota de débito debe tener al menos un item")]
    public List<CreateDebitNoteDetalleRequest> Details { get; init; } = new();
}

/// <summary>
/// Request DTO for debit note line item
/// </summary>
public record CreateDebitNoteDetalleRequest
{
    /// <summary>Product ID</summary>
    [Required]
    public int ProductId { get; init; }
    
    /// <summary>Quantity</summary>
    [Required]
    [Range(0.01, 999999.99, ErrorMessage = "La cantidad debe ser mayor a 0")]
    public decimal Quantity { get; init; }
    
    /// <summary>Unit price (if null, uses product's PrecioInvoice)</summary>
    public decimal? UnitPrice { get; init; }
    
    /// <summary>Line-level discount percentage</summary>
    [Range(0, 100)]
    public decimal DiscountPercent { get; init; } = 0;
}

/// <summary>
/// Request DTO for voiding a debit note
/// </summary>
public record AnularDebitNoteRequest
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
/// Response DTO for debit note listing
/// </summary>
public record DebitNoteResponse
{
    public int Id { get; init; }
    public string VoucherType { get; init; } = "";
    public int PointOfSale { get; init; }
    public long DebitNoteNumber { get; init; }
    
    /// <summary>Formatted number: ND A 0001-00001234</summary>
    public string NumeroCompleto => $"ND {VoucherType} {PointOfSale:D4}-{DebitNoteNumber:D8}";
    
    public DateTime DebitNoteDate { get; init; }
    
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = "";
    public string? CustomerCUIT { get; init; }
    
    public int? SalesRepId { get; init; }
    public string? SalesRepName { get; init; }
    
    public decimal Subtotal { get; init; }
    public decimal VATPercent { get; init; }
    public decimal VATAmount { get; init; }
    public decimal IIBBPercent { get; init; }
    public decimal IIBBAmount { get; init; }
    public decimal DiscountPercent { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal Total { get; init; }
    
    public string? CAE { get; init; }
    public DateTime? CAEExpirationDate { get; init; }
    public bool HasCAE => !string.IsNullOrEmpty(CAE);
    
    public bool IsVoided { get; init; }
    
    public int ItemCount { get; init; }
}

/// <summary>
/// Response DTO for debit note line item
/// </summary>
public record DebitNoteDetalleResponse
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
/// Full debit note response with details
/// </summary>
public record DebitNoteCompletaResponse : DebitNoteResponse
{
    public string? SalesCondition { get; init; }
    public string? Notes { get; init; }
    public List<DebitNoteDetalleResponse> Details { get; init; } = new();
}
