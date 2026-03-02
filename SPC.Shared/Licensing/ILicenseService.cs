namespace SPC.Shared.Licensing;

/// <summary>
/// Service for license validation and feature flag checking.
/// </summary>
public interface ILicenseService
{
    /// <summary>
    /// Checks if a specific feature is enabled in the current license.
    /// </summary>
    /// <param name="featureName">Feature name from <see cref="Features"/> constants.</param>
    /// <returns>True if the feature is enabled and license is valid.</returns>
    bool IsFeatureEnabled(string featureName);

    /// <summary>
    /// Gets detailed information about the current license.
    /// </summary>
    LicenseInfo GetLicenseInfo();

    /// <summary>
    /// Validates a license key and returns the result.
    /// </summary>
    /// <param name="licenseKey">The license key to validate.</param>
    /// <returns>License info if valid, null if invalid.</returns>
    LicenseInfo? ValidateLicenseKey(string licenseKey);
}
