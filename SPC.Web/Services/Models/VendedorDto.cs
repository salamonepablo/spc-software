namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for SalesRep dropdown data
/// </summary>
public class SalesRepDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Telefono { get; set; }
    public string? Email { get; set; }
}
