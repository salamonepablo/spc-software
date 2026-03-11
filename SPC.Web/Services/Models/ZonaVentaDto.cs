namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for SalesZone dropdown data
/// </summary>
public class SalesZoneDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
}
