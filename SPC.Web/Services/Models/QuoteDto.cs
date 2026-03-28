namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for quote listing
/// </summary>
public class QuoteDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string? BranchName { get; set; }
    public long QuoteNumber { get; set; }
    public string NumeroCompleto { get; set; } = "";
    public DateTime QuoteDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string? CustomerCUIT { get; set; }
    public int? SalesRepId { get; set; }
    public string? SalesRepName { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public bool IsVoided { get; set; }
    public int ItemCount { get; set; }
}

/// <summary>
/// DTO for quote detail line
/// </summary>
public class QuoteDetailDto
{
    public int Id { get; set; }
    public int ItemNumber { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductDescription { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Subtotal { get; set; }
}

/// <summary>
/// Full quote DTO with details
/// </summary>
public class QuoteCompletaDto : QuoteDto
{
    public string? BusinessUnit { get; set; }
    public string? Notes { get; set; }
    public List<QuoteDetailDto> Details { get; set; } = new();
}

/// <summary>
/// Summary statistics for quotes
/// </summary>
public class QuotesResumenDto
{
    public decimal MontoHoy { get; set; }
    public int QuotesHoy { get; set; }
    public decimal MontoMes { get; set; }
    public int QuotesMes { get; set; }
    public decimal MontoAnio { get; set; }
    public int TotalQuotes { get; set; }
}
