namespace SPC.Shared.Licensing;

/// <summary>
/// Feature flag constants for licensed modules.
/// </summary>
public static class Features
{
    /// <summary>
    /// Dual-line current account (Billing + Budget).
    /// When disabled, only Billing balance is tracked.
    /// Budget = Quotes (sin IVA, no fiscal, linea de credito paralela).
    /// </summary>
    public const string DualLineCurrentAccount = "DualLineCurrentAccount";

    /// <summary>
    /// Multi-branch/point of sale support.
    /// </summary>
    public const string MultiBranch = "MultiBranch";
}
