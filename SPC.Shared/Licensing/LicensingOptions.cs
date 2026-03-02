namespace SPC.Shared.Licensing;

/// <summary>
/// Configuration options for the licensing system.
/// Bound from appsettings.json "Licensing" section.
/// </summary>
public class LicensingOptions
{
    public const string SectionName = "Licensing";

    /// <summary>
    /// The license key (stored in User Secrets or environment variable).
    /// </summary>
    public string? LicenseKey { get; set; }

    /// <summary>
    /// Feature flags that can be configured directly (for development/testing).
    /// In production, these are determined by the license key.
    /// </summary>
    public FeatureFlags Features { get; set; } = new();
}

/// <summary>
/// Individual feature flags.
/// </summary>
public class FeatureFlags
{
    /// <summary>
    /// Enable dual-line current account (Billing + Budget).
    /// Budget = Presupuestos (sin IVA, no fiscal, linea de credito paralela).
    /// Default: false (single-line Billing only).
    /// </summary>
    public bool DualLineCurrentAccount { get; set; } = false;

    /// <summary>
    /// Enable multi-branch support.
    /// Default: false.
    /// </summary>
    public bool MultiBranch { get; set; } = false;
}
