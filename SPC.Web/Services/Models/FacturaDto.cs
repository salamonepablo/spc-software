namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for invoice listing
/// </summary>
public class InvoiceDto
{
    public int Id { get; set; }
    public string InvoiceType { get; set; } = "";
    public int PointOfSale { get; set; }
    public long InvoiceNumber { get; set; }
    public string NumeroCompleto { get; set; } = "";
    public DateTime InvoiceDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerCompanyName { get; set; } = "";
    public string? CustomerCUIT { get; set; }
    public int? SalesRepId { get; set; }
    public string? SalesRepFirstName { get; set; }
    public decimal Subtotal { get; set; }
    public decimal VATAmount { get; set; }
    public decimal IncludedVAT { get; set; }
    public decimal IIBBPerceptionAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public string? CAE { get; set; }
    public DateTime? CAEExpirationDate { get; set; }
    public bool TieneCAE { get; set; }
    public bool IsVoided { get; set; }
    public int ItemCount { get; set; }
}

/// <summary>
/// DTO for invoice detail line
/// </summary>
public class InvoiceDetailDto
{
    public int Id { get; set; }
    public int ItemNumber { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductDescription { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal VATPercent { get; set; }
    public decimal Subtotal { get; set; }
}

/// <summary>
/// DTO for complete invoice with details
/// </summary>
public class InvoiceCompletaDto : InvoiceDto
{
    public List<InvoiceDetailDto> Details { get; set; } = new();
}

/// <summary>
/// DTO for invoicing summary statistics
/// </summary>
public class InvoicecionResumenDto
{
    public int TotalInvoices { get; set; }
    public int InvoicesHoy { get; set; }
    public int InvoicesMes { get; set; }
    public decimal MontoHoy { get; set; }
    public decimal MontoMes { get; set; }
    public decimal MontoAnio { get; set; }
}
