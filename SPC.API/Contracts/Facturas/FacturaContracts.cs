using System.ComponentModel.DataAnnotations;

namespace SPC.API.Contracts.Invoices;

// ===========================================
// REQUEST DTOs (para crear/actualizar)
// ===========================================

/// <summary>
/// Request DTO for creating an invoice
/// </summary>
public record CreateInvoiceRequest
{
    /// <summary>Branch/point of sale ID</summary>
    [Required]
    public int BranchId { get; init; }
    
    /// <summary>Invoice type: A or B</summary>
    [Required]
    [StringLength(1)]
    public string InvoiceType { get; init; } = "B";
    
    /// <summary>Customer ID</summary>
    [Required]
    public int CustomerId { get; init; }
    
    /// <summary>Sales rep ID (optional)</summary>
    public int? SalesRepId { get; init; }
    
    /// <summary>Document-level discount percentage (can override customer default)</summary>
    [Range(0, 100)]
    public decimal DiscountPercent { get; init; } = 0;
    
    /// <summary>IIBB perception rate (optional)</summary>
    [Range(0, 100)]
    public decimal IIBBPercent { get; init; } = 0;
    
    /// <summary>Payment condition</summary>
    [StringLength(50)]
    public string? SalesCondition { get; init; }
    
    /// <summary>Notes/observations</summary>
    [StringLength(500)]
    public string? Notes { get; init; }
    
    /// <summary>Invoice line items</summary>
    [Required]
    [MinLength(1, ErrorMessage = "La factura debe tener al menos un item")]
    public List<CreateInvoiceDetailRequest> Details { get; init; } = new();
}

/// <summary>
/// Request DTO for invoice line item
/// </summary>
public record CreateInvoiceDetailRequest
{
    /// <summary>Product ID</summary>
    [Required]
    public int ProductId { get; init; }
    
    /// <summary>Quantity</summary>
    [Required]
    [Range(0.01, 999999.99, ErrorMessage = "La Quantity debe ser mayor a 0")]
    public decimal Quantity { get; init; }
    
    /// <summary>Unit price (if null, uses product's InvoicePrice)</summary>
    public decimal? UnitPrice { get; init; }
    
    /// <summary>Line-level discount percentage</summary>
    [Range(0, 100)]
    public decimal DiscountPercent { get; init; } = 0;
    
    /// <summary>VAT percentage (if null, uses product's VATPercent)</summary>
    public decimal? VATPercent { get; init; }
}

/// <summary>
/// Request DTO for voiding an invoice
/// </summary>
public record AnularInvoiceRequest
{
    /// <summary>Reason for voiding</summary>
    [Required]
    [StringLength(500)]
    public string Motivo { get; init; } = "";
}

// ===========================================
// RESPONSE DTOs
// ===========================================

/// <summary>
/// Response DTO for invoice listing
/// </summary>
public record InvoiceResponse
{
    public int Id { get; init; }
    public string InvoiceType { get; init; } = "";
    public int PointOfSale { get; init; }
    public long InvoiceNumber { get; init; }
    
    /// <summary>Formatted invoice number: A 0001-00001234</summary>
    public string NumeroCompleto => $"{InvoiceType} {PointOfSale:D4}-{InvoiceNumber:D8}";
    
    public DateTime InvoiceDate { get; init; }
    
    public int CustomerId { get; init; }
    public string CustomerCompanyName { get; init; } = "";
    public string? CustomerCUIT { get; init; }
    
    public int? SalesRepId { get; init; }
    public string? SalesRepFirstName { get; init; }
    
    public decimal Subtotal { get; init; }
    
    /// <summary>IVA discriminado (Invoice A). En Invoice B es 0.</summary>
    public decimal VATAmount { get; init; }
    
    /// <summary>
    /// IVA Contenido en el precio (solo Invoice B).
    /// Requerido por Ley 27.743 - Régimen de Transparencia Fiscal.
    /// </summary>
    public decimal IncludedVAT { get; init; }
    
    public decimal IIBBPerceptionAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal Total { get; init; }
    
    public string? CAE { get; init; }
    public DateTime? CAEExpirationDate { get; init; }
    public bool TieneCAE => !string.IsNullOrEmpty(CAE);
    
    public bool IsVoided { get; init; }
    
    public int ItemCount { get; init; }
}

/// <summary>
/// Response DTO for invoice with details
/// </summary>
public record InvoiceDetailResponse
{
    public int Id { get; init; }
    public int ItemNumber { get; init; }
    public int ProductId { get; init; }
    public string ProductCode { get; init; } = "";
    public string ProductDescription { get; init; } = "";
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal DiscountPercent { get; init; }
    public decimal VATPercent { get; init; }
    public decimal Subtotal { get; init; }
}

/// <summary>
/// Full invoice response with header and details
/// </summary>
public record InvoiceCompletaResponse : InvoiceResponse
{
    public List<InvoiceDetailResponse> Details { get; init; } = new();
}

/// <summary>
/// Summary statistics for invoicing dashboard
/// </summary>
public record InvoicecionResumenResponse
{
    public int TotalInvoices { get; init; }
    public int InvoicesHoy { get; init; }
    public int InvoicesMes { get; init; }
    public decimal MontoHoy { get; init; }
    public decimal MontoMes { get; init; }
    public decimal MontoAnio { get; init; }
}
