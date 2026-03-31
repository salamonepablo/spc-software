using Microsoft.Extensions.Options;
using SPC.Shared.Licensing;

namespace SPC.API.Services;

/// <summary>
/// Implementation of license validation and feature flag service.
/// </summary>
public class LicenseService : ILicenseService
{
    private readonly LicensingOptions _options;
    private readonly ILogger<LicenseService> _logger;
    private readonly LicenseInfo _currentLicense;

    public LicenseService(IOptions<LicensingOptions> options, ILogger<LicenseService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _currentLicense = InitializeLicense();
    }

    /// <inheritdoc />
    public bool IsFeatureEnabled(string featureName)
    {
        if (!_currentLicense.IsValid || _currentLicense.IsExpired)
        {
            _logger.LogWarning("License invalid or expired. Feature {Feature} denied.", featureName);
            return false;
        }

        var enabled = _currentLicense.EnabledFeatures.Contains(featureName);
        
        _logger.LogDebug("Feature check: {Feature} = {Enabled}", featureName, enabled);
        
        return enabled;
    }

    /// <inheritdoc />
    public LicenseInfo GetLicenseInfo()
    {
        return _currentLicense;
    }

    /// <inheritdoc />
    public LicenseInfo? ValidateLicenseKey(string licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            return null;
        }

        // License key format: SPC-{CUSTOMER_ID}-{TIER}-{EXPIRY_YYYYMMDD}-{CHECKSUM}
        // Example: SPC-QUILPLAC-PREMIUM-20271231-A7F3B2
        
        var parts = licenseKey.Split('-');
        
        if (parts.Length < 5 || parts[0] != "SPC")
        {
            _logger.LogWarning("Invalid license key format.");
            return null;
        }

        var customerId = parts[1];
        var tier = parts[2];
        var expiryStr = parts[3];
        var checksum = parts[4];

        // Validate checksum (simplified - in production use proper crypto)
        if (!ValidateChecksum(customerId, tier, expiryStr, checksum))
        {
            _logger.LogWarning("License key checksum validation failed.");
            return null;
        }

        // Parse expiry date
        DateTime? expiresAt = null;
        if (expiryStr != "PERPETUAL" && DateTime.TryParseExact(expiryStr, "yyyyMMdd", 
            null, System.Globalization.DateTimeStyles.None, out var expiry))
        {
            expiresAt = expiry;
        }

        var license = new LicenseInfo
        {
            CustomerId = customerId,
            Tier = tier,
            ExpiresAt = expiresAt,
            IsValid = true,
            EnabledFeatures = GetFeaturesForTier(tier)
        };

        _logger.LogInformation("License validated for customer {CustomerId}, tier {Tier}.", 
            customerId, tier);

        return license;
    }

    private LicenseInfo InitializeLicense()
    {
        // First, try to validate the license key
        if (!string.IsNullOrEmpty(_options.LicenseKey))
        {
            var license = ValidateLicenseKey(_options.LicenseKey);
            if (license != null)
            {
                return license;
            }
        }

        // Fall back to configuration-based features (for development)
        var baseLicense = new LicenseInfo
        {
            CustomerId = "COMMUNITY",
            Tier = "BASE",
            IsValid = true,
            EnabledFeatures = new List<string>()
        };

        // Add features based on configuration
        if (_options.Features.DualLineCurrentAccount)
        {
            baseLicense.EnabledFeatures.Add(Features.DualLineCurrentAccount);
        }
        if (_options.Features.MultiBranch)
        {
            baseLicense.EnabledFeatures.Add(Features.MultiBranch);
        }

        _logger.LogInformation("Using BASE license with {Count} features enabled from configuration.",
            baseLicense.EnabledFeatures.Count);

        return baseLicense;
    }

    private static List<string> GetFeaturesForTier(string tier)
    {
        return tier.ToUpperInvariant() switch
        {
            "PREMIUM" => new List<string>
            {
                Features.DualLineCurrentAccount
            },
            "ENTERPRISE" => new List<string>
            {
                Features.DualLineCurrentAccount,
                Features.MultiBranch
            },
            _ => new List<string>() // BASE tier - no premium features
        };
    }

    private bool ValidateChecksum(string customerId, string tier, string expiry, string checksum)
    {
        // Simplified checksum validation
        // In production, use HMAC-SHA256 or similar with a secret key
        
        var data = $"{customerId}-{tier}-{expiry}";
        var expectedChecksum = ComputeSimpleChecksum(data);
        
        return string.Equals(checksum, expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeSimpleChecksum(string data)
    {
        // Simple checksum for demonstration
        // IMPORTANT: Replace with proper cryptographic signature in production
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data + "SPC_SECRET_KEY"));
        return Convert.ToHexString(hash)[..6]; // First 6 chars
    }
}
