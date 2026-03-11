namespace SPC.Web.Services.Models;

public class CreateInvoiceDto
{
    public int BranchId { get; set; }
    public string TipoInvoice { get; set; } = "B";
    public int CustomerId { get; set; }
    public int? SalesRepId { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public decimal AlicuotaIIBB { get; set; }
    public string? CondicionVenta { get; set; }
    public string? Observaciones { get; set; }
    public List<CreateInvoiceDetailDto> Detalles { get; set; } = new();
}

public class CreateInvoiceDetailDto
{
    public int ProductId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal? PrecioUnitario { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public decimal? PorcentajeIVA { get; set; }
}
