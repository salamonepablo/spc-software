namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for invoice listing
/// </summary>
public class FacturaDto
{
    public int Id { get; set; }
    public string TipoFactura { get; set; } = "";
    public int PuntoVenta { get; set; }
    public long NumeroFactura { get; set; }
    public string NumeroCompleto { get; set; } = "";
    public DateTime FechaFactura { get; set; }
    public int ClienteId { get; set; }
    public string ClienteRazonSocial { get; set; } = "";
    public string? ClienteCUIT { get; set; }
    public int? VendedorId { get; set; }
    public string? VendedorNombre { get; set; }
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
public class FacturaDetalleDto
{
    public int Id { get; set; }
    public int ItemNumero { get; set; }
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = "";
    public string ProductoDescripcion { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public decimal PorcentajeIVA { get; set; }
    public decimal Subtotal { get; set; }
}

/// <summary>
/// DTO for complete invoice with details
/// </summary>
public class FacturaCompletaDto : FacturaDto
{
    public List<FacturaDetalleDto> Detalles { get; set; } = new();
}

/// <summary>
/// DTO for invoicing summary statistics
/// </summary>
public class FacturacionResumenDto
{
    public int TotalFacturas { get; set; }
    public int FacturasHoy { get; set; }
    public int FacturasMes { get; set; }
    public decimal MontoHoy { get; set; }
    public decimal MontoMes { get; set; }
    public decimal MontoAnio { get; set; }
}
