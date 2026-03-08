using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Entidad Cliente - Equivalente a tabla Clientes en Access
/// </summary>
public class Cliente
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
    
    // Relación con CondicionIva
    public int? CondicionIvaId { get; set; }
    public CondicionIva? CondicionIva { get; set; }
    
    // Relación con Vendedor
    public int? VendedorId { get; set; }
    public Vendedor? Vendedor { get; set; }
    
    // Relación con Zona de Venta
    public int? ZonaVentaId { get; set; }
    public ZonaVenta? ZonaVenta { get; set; }
    
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
    public List<Factura> Facturas { get; set; } = new();
    public List<Remito> Remitos { get; set; } = new();
    public List<CustomerAddress> DeliveryAddresses { get; set; } = new();
    public List<Quote> Quotes { get; set; } = new();
    public List<CreditNote> CreditNotes { get; set; } = new();
    public List<DebitNote> DebitNotes { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}
