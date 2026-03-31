namespace SPC.API.Services.CurrentAccount;

public class CurrentAccountGuardrailOptions
{
    public const string SectionName = "CurrentAccount:Guardrails";

    public int MaxRangeDays { get; set; } = 730;
    public int MaxRows { get; set; } = 5000;
    public string OverflowMode { get; set; } = "truncated";
}
