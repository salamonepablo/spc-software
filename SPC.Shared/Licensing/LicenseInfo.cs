namespace SPC.Shared.Licensing;

/// <summary>
/// Contains information about the current license.
/// </summary>
public class LicenseInfo
{
    /// <summary>
    /// Customer identifier from the license key.
    /// </summary>
    public string CustomerId { get; set; } = "COMMUNITY";

    /// <summary>
    /// License tier: BASE, PREMIUM, ENTERPRISE.
    /// </summary>
    public string Tier { get; set; } = "BASE";

    /// <summary>
    /// License expiration date. Null for perpetual licenses.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether the license is currently valid.
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// List of enabled feature names.
    /// </summary>
    public List<string> EnabledFeatures { get; set; } = new();

    /// <summary>
    /// Checks if the license has expired.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
}
