namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for UnitOfMeasure dropdown data
/// </summary>
public class UnitOfMeasureDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}
