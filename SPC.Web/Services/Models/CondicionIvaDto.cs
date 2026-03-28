namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for TaxCondition dropdown data
/// </summary>
public class TaxConditionDto
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public string InvoiceType { get; set; } = "";
}
