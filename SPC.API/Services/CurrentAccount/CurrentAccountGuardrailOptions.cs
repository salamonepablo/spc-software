namespace SPC.API.Services.CurrentAccount;

public class CurrentAccountGuardrailOptions
{
    public const string SectionName = "CurrentAccount:Guardrails";

    public int MaxRows { get; set; } = 5000;
}
