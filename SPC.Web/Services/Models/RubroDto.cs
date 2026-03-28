namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for Category dropdown data
/// </summary>
public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}
