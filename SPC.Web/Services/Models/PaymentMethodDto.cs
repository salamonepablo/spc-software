namespace SPC.Web.Services.Models;

public class PaymentMethodDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public bool RequiresDetail { get; set; }
}
