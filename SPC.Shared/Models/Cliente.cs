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
    
    [StringLength(500)]
    public string? Observaciones { get; set; }
    
    public bool Activo { get; set; } = true;
    
    public DateTime FechaAlta { get; set; } = DateTime.Now;
    
    // Navegación: un cliente tiene muchas facturas, remitos, etc.
    public List<Factura> Facturas { get; set; } = new();
    public List<Remito> Remitos { get; set; } = new();
}
