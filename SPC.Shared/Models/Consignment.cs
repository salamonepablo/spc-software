using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Consignacion de mercaderia a cliente.
/// Stock en poder del cliente pero propiedad de la empresa.
/// </summary>
public class Consignment
{
    public int Id { get; set; }
    
    /// <summary>Numero de consignacion</summary>
    public long ConsignmentNumber { get; set; }
    
    /// <summary>Fecha de consignacion</summary>
    public DateTime ConsignmentDate { get; set; }
    
    /// <summary>Customer</summary>
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    /// <summary>SalesRep</summary>
    public int? SalesRepId { get; set; }
    public SalesRep? SalesRep { get; set; }
    
    /// <summary>Esta activa? (false = devuelta o facturada)</summary>
    public bool IsActive { get; set; } = true;
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    // Navegacion
    public List<ConsignmentDetail> Details { get; set; } = new();
}

/// <summary>
/// Detalle de consignacion.
/// </summary>
public class ConsignmentDetail
{
    public int Id { get; set; }
    
    public int ConsignmentId { get; set; }
    public Consignment? Consignment { get; set; }
    
    public int ItemNumber { get; set; }
    
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    
    public decimal Quantity { get; set; }
    
    [StringLength(20)]
    public string? UnitOfMeasure { get; set; }
}
