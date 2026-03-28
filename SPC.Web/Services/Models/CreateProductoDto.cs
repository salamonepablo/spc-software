using System.ComponentModel.DataAnnotations;

namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for creating a new Product
/// </summary>
public class CreateProductDto
{
    [Required(ErrorMessage = "El código es requerido")]
    [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
    public string Code { get; set; } = "";
    
    [Required(ErrorMessage = "La descripción es requerida")]
    [StringLength(300, ErrorMessage = "Máximo 300 caracteres")]
    public string Description { get; set; } = "";
    
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
    public string? SupplierCode { get; set; }
    
    public int? CategoryId { get; set; }
    
    public int? UnitOfMeasureId { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Debe ser mayor o igual a 0")]
    public decimal SalePrice { get; set; } = 0;
    
    [Range(0, double.MaxValue, ErrorMessage = "Debe ser mayor o igual a 0")]
    public decimal CostPrice { get; set; } = 0;
    
    [Range(0, 100, ErrorMessage = "Debe estar entre 0 y 100")]
    public decimal VATPercent { get; set; } = 21;
    
    [Range(0, int.MaxValue, ErrorMessage = "Debe ser mayor o igual a 0")]
    public int MinimumStock { get; set; } = 0;
    
    [StringLength(500, ErrorMessage = "Máximo 500 caracteres")]
    public string? Notes { get; set; }
}
