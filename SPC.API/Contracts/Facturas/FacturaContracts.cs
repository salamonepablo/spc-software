namespace SPC.API.Contracts.Facturas;

/// <summary>
/// Response DTO for invoice listing
/// </summary>
public record FacturaResponse
{
    public int Id { get; init; }
    public string TipoFactura { get; init; } = "";
    public int PuntoVenta { get; init; }
    public long NumeroFactura { get; init; }
    
    /// <summary>Formatted invoice number: A 0001-00001234</summary>
    public string NumeroCompleto => $"{TipoFactura} {PuntoVenta:D4}-{NumeroFactura:D8}";
    
    public DateTime FechaFactura { get; init; }
    
    public int ClienteId { get; init; }
    public string ClienteRazonSocial { get; init; } = "";
    public string? ClienteCUIT { get; init; }
    
    public int? VendedorId { get; init; }
    public string? VendedorNombre { get; init; }
    
    public decimal Subtotal { get; init; }
    public decimal ImporteIVA { get; init; }
    public decimal ImportePercepcionIIBB { get; init; }
    public decimal ImporteDescuento { get; init; }
    public decimal Total { get; init; }
    
    public string? CAE { get; init; }
    public DateTime? FechaVencimientoCAE { get; init; }
    public bool TieneCAE => !string.IsNullOrEmpty(CAE);
    
    public bool Anulada { get; init; }
    
    public int CantidadItems { get; init; }
}

/// <summary>
/// Response DTO for invoice with details
/// </summary>
public record FacturaDetalleResponse
{
    public int Id { get; init; }
    public int ItemNumero { get; init; }
    public int ProductoId { get; init; }
    public string ProductoCodigo { get; init; } = "";
    public string ProductoDescripcion { get; init; } = "";
    public decimal Cantidad { get; init; }
    public decimal PrecioUnitario { get; init; }
    public decimal PorcentajeDescuento { get; init; }
    public decimal PorcentajeIVA { get; init; }
    public decimal Subtotal { get; init; }
}

/// <summary>
/// Full invoice response with header and details
/// </summary>
public record FacturaCompletaResponse : FacturaResponse
{
    public List<FacturaDetalleResponse> Detalles { get; init; } = new();
}

/// <summary>
/// Summary statistics for invoicing dashboard
/// </summary>
public record FacturacionResumenResponse
{
    public int TotalFacturas { get; init; }
    public int FacturasHoy { get; init; }
    public int FacturasMes { get; init; }
    public decimal MontoHoy { get; init; }
    public decimal MontoMes { get; init; }
    public decimal MontoAnio { get; init; }
}
