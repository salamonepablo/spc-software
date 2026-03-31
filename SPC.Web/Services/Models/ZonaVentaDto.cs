namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for SalesZone dropdown data
/// </summary>
public class SalesZoneDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}
