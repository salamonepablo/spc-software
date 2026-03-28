namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for SalesRep dropdown data
/// </summary>
public class SalesRepDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string? LastName { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
