namespace SPC.Web.Services.Models;

public class CreateFacturaDto
{
    public int BranchId { get; set; }
    public string TipoFactura { get; set; } = "B";
    public int ClienteId { get; set; }
    public int? VendedorId { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public decimal AlicuotaIIBB { get; set; }
    public string? CondicionVenta { get; set; }
    public string? Observaciones { get; set; }
    public List<CreateFacturaDetalleDto> Detalles { get; set; } = new();
}

public class CreateFacturaDetalleDto
{
    public int ProductoId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal? PrecioUnitario { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public decimal? PorcentajeIVA { get; set; }
}
