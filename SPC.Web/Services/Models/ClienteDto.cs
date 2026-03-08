namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for Cliente data from API
/// </summary>
public class ClienteDto
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
    public int? CondicionIvaId { get; set; }
    public string? CondicionIvaDescripcion { get; set; }
    public string? CondicionIvaCodigo { get; set; }
    public string? TipoFactura { get; set; }
    
    // IIBB data from AFIP padrón
    public decimal AlicuotaIIBB { get; set; }
    public string? ProvinciaPadronIIBB { get; set; }
    
    public int? VendedorId { get; set; }
    public string? VendedorNombre { get; set; }
    
    public int? ZonaVentaId { get; set; }
    public string? ZonaVentaNombre { get; set; }
}
