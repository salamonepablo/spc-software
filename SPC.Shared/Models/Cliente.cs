using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Entidad Customer - Equivalente a tabla Customers en Access
/// </summary>
public class Customer
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "La razón social es requerida")]
    [StringLength(200, ErrorMessage = "Máximo 200 caracteres")]
    public string RazonSocial { get; set; } = "";
    
    [StringLength(200)]
    public string? NombreFantasia { get; set; }
    
    [StringLength(13)]
    public string? CUIT { get; set; }
    
    [StringLength(300)]
    public string? Direccion { get; set; }
    
    [StringLength(100)]
    public string? Localidad { get; set; }
    
    [StringLength(100)]
    public string? Provincia { get; set; }
    
    [StringLength(10)]
    public string? CodigoPostal { get; set; }
    
    [StringLength(50)]
    public string? Telefono { get; set; }
    
    [StringLength(50)]
    public string? Celular { get; set; }
    
    [StringLength(200)]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string? Email { get; set; }
    
    // Relación con TaxCondition
    public int? TaxConditionId { get; set; }
    public TaxCondition? TaxCondition { get; set; }
    
    // Relación con SalesRep
    public int? SalesRepId { get; set; }
    public SalesRep? SalesRep { get; set; }
    
    // Relación con Zona de Venta
    public int? SalesZoneId { get; set; }
    public SalesZone? SalesZone { get; set; }
    
    [Range(0, 100, ErrorMessage = "Descuento debe estar entre 0 y 100")]
    public decimal PorcentajeDescuento { get; set; } = 0;
    
    public decimal LimiteCredito { get; set; } = 0;
    
    // ===================================
    // IIBB Perception (from AFIP/ARCA padrón)
    // ===================================
    
    /// <summary>
    /// Alícuota de percepción IIBB para este cliente.
    /// Viene del padrón de ARBA/AGIP/etc según la provincia.
    /// Valor 0 = Exento o no aplica.
    /// </summary>
    [Range(0, 100, ErrorMessage = "Alícuota IIBB debe estar entre 0 y 100")]
    public decimal AlicuotaIIBB { get; set; } = 0;
    
    /// <summary>
    /// Código de provincia del padrón IIBB que aplica a este cliente.
    /// Ej: "BA" (Buenos Aires/ARBA), "CABA" (AGIP), etc.
    /// </summary>
    [StringLength(10)]
    public string? ProvinciaPadronIIBB { get; set; }
    
    [StringLength(500)]
    public string? Observaciones { get; set; }
    
    public bool Activo { get; set; } = true;
    
    public DateTime FechaAlta { get; set; } = DateTime.Now;
    
    // Navegacion: un cliente tiene muchas facturas, remitos, etc.
    public List<Invoice> Invoices { get; set; } = new();
    public List<DeliveryNote> DeliveryNotes { get; set; } = new();
    public List<CustomerAddress> DeliveryAddresses { get; set; } = new();
    public List<Quote> Quotes { get; set; } = new();
    public List<CreditNote> CreditNotes { get; set; } = new();
    public List<DebitNote> DebitNotes { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}
