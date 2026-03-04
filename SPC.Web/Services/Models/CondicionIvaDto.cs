namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for CondicionIva dropdown data
/// </summary>
public class CondicionIvaDto
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = "";
    public string TipoFactura { get; set; } = "";
}
