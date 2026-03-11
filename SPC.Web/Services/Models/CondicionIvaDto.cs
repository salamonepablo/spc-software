namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for TaxCondition dropdown data
/// </summary>
public class TaxConditionDto
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = "";
    public string TipoInvoice { get; set; } = "";
}
