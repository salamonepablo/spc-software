namespace SPC.API.Contracts.Customers;

/// <summary>
/// Response DTO for Customer data returned by API
/// </summary>
public class CustomerResponse
{
    public int Id { get; set; }
    public string RazonSocial { get; set; } = "";
    public string? NombreFantasia { get; set; }
    public string? CUIT { get; set; }
    public string? Direccion { get; set; }
    public string? Localidad { get; set; }
    public string? Provincia { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Telefono { get; set; }
    public string? Celular { get; set; }
    public string? Email { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public decimal LimiteCredito { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaAlta { get; set; }
    
    // Related entity info (flattened)
    public int? TaxConditionId { get; set; }
    public string? TaxConditionDescripcion { get; set; }
    public string? TaxConditionCodigo { get; set; }
    public string? TipoInvoice { get; set; }
    
    // IIBB data from AFIP padrón
    public decimal AlicuotaIIBB { get; set; }
    public string? ProvinciaPadronIIBB { get; set; }
    
    public int? SalesRepId { get; set; }
    public string? SalesRepNombre { get; set; }
    
    public int? SalesZoneId { get; set; }
    public string? SalesZoneNombre { get; set; }
}
