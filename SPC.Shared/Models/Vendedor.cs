using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// SalesRep / Representante de ventas.
/// En Access era "Empleados" pero solo se usa para vendedores.
/// </summary>
public class SalesRep
{
    public int Id { get; set; }
    
    /// <summary>Codigo/Legajo del vendedor (PK original en Access)</summary>
    [Required]
    [StringLength(20)]
    public string Legajo { get; set; } = "";
    
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = "";
    
    [StringLength(100)]
    public string? Apellido { get; set; }
    
    [StringLength(13)]
    public string? CUIL { get; set; }
    
    [StringLength(300)]
    public string? Domicilio { get; set; }
    
    [StringLength(100)]
    public string? Localidad { get; set; }
    
    [StringLength(100)]
    public string? Provincia { get; set; }
    
    [StringLength(10)]
    public string? CodigoPostal { get; set; }
    
    [StringLength(15)]
    public string? DNI { get; set; }
    
    [StringLength(50)]
    public string? Telefono { get; set; }
    
    [StringLength(50)]
    public string? Celular { get; set; }
    
    [StringLength(200)]
    public string? Email { get; set; }
    
    public DateTime? FechaNacimiento { get; set; }
    
    public DateTime? FechaIngreso { get; set; }
    
    [Range(0, 100)]
    public decimal PorcentajeComision { get; set; } = 0;
    
    [StringLength(500)]
    public string? Observaciones { get; set; }
    
    public bool Activo { get; set; } = true;
    
    // Navegacion
    public List<Customer> Customers { get; set; } = new();
    public List<Warehouse> WarehousesAsignados { get; set; } = new();
}
