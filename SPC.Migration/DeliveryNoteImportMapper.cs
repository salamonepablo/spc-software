namespace SPC.Migration;

/// <summary>Maps legacy delivery-note status labels to the current semantic properties.</summary>
public static class DeliveryNoteImportMapper
{
    public static (bool IsInvoiced, bool IsVoided) MapStatus(string? legacyInvoiced, string? legacyVoided, bool inferredInvoiced = false)
    {
        return (ParseStatus(legacyInvoiced) || inferredInvoiced, ParseStatus(legacyVoided));
    }

    private static bool ParseStatus(string? value) =>
        value?.Equals("S", StringComparison.OrdinalIgnoreCase) == true ||
        value?.Equals("TRUE", StringComparison.OrdinalIgnoreCase) == true ||
        value == "1";
}
