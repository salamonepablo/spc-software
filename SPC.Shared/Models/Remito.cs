using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPC.Shared.Models;

/// <summary>
/// Remito oficial - Cabecera (RemitoC en Access).
/// Albaran de entrega de mercaderia.
/// </summary>
public class Remito
{
    public int Id { get; set; }
    
    /// <summary>Sucursal</summary>
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
    
    public int PuntoVenta { get; set; } = 1;
    
    public long NumeroRemito { get; set; }
    
    public DateTime FechaRemito { get; set; } = DateTime.Now;
    
    // Relacion con Cliente
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    
    // Vendedor
    public int? VendedorId { get; set; }
    public Vendedor? Vendedor { get; set; }
    
    // Direccion de entrega (puede ser diferente a la del cliente)
    [StringLength(300)]
    public string? DireccionEntrega { get; set; }
    
    [StringLength(100)]
    public string? LocalidadEntrega { get; set; }
    
    /// <summary>Unidad de negocio</summary>
    [StringLength(50)]
    public string? UnidadNegocio { get; set; }
    
    /// <summary>Aclaracion en remito</summary>
    [StringLength(200)]
    public string? Aclaracion { get; set; }
    
    // Estado
    public bool Facturado { get; set; } = false;
    public int? FacturaId { get; set; }
    public Factura? Factura { get; set; }
    
    /// <summary>Tipo de factura asociada</summary>
    [StringLength(1)]
    public string? TipoFactura { get; set; }
    
    public bool Anulado { get; set; } = false;
    
    [StringLength(500)]
    public string? Observaciones { get; set; }
    
    // Navegacion
    public List<RemitoDetalle> Detalles { get; set; } = new();
}
