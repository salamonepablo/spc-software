namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for invoice listing
/// </summary>
public class InvoiceDto
{
    public int Id { get; set; }
    public string TipoInvoice { get; set; } = "";
    public int PuntoVenta { get; set; }
    public long NumeroInvoice { get; set; }
    public string NumeroCompleto { get; set; } = "";
    public DateTime FechaInvoice { get; set; }
    public int CustomerId { get; set; }
    public string CustomerRazonSocial { get; set; } = "";
    public string? CustomerCUIT { get; set; }
    public int? SalesRepId { get; set; }
    public string? SalesRepNombre { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ImporteIVA { get; set; }
    public decimal IVAContenido { get; set; }
    public decimal ImportePercepcionIIBB { get; set; }
    public decimal ImporteDescuento { get; set; }
    public decimal Total { get; set; }
    public string? CAE { get; set; }
    public DateTime? FechaVencimientoCAE { get; set; }
    public bool TieneCAE { get; set; }
    public bool Anulada { get; set; }
    public int CantidadItems { get; set; }
}

/// <summary>
/// DTO for invoice detail line
/// </summary>
public class InvoiceDetailDto
{
    public int Id { get; set; }
    public int ItemNumero { get; set; }
    public int ProductId { get; set; }
    public string ProductCodigo { get; set; } = "";
    public string ProductDescripcion { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public decimal PorcentajeIVA { get; set; }
    public decimal Subtotal { get; set; }
}

/// <summary>
/// DTO for complete invoice with details
/// </summary>
public class InvoiceCompletaDto : InvoiceDto
{
    public List<InvoiceDetailDto> Detalles { get; set; } = new();
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
