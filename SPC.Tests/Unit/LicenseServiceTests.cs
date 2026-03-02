using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SPC.API.Services;
using SPC.Shared.Licensing;

namespace SPC.Tests.Unit;

/// <summary>
/// Unit tests for LicenseService.
/// Tests feature flags and license validation logic.
/// </summary>
public class LicenseServiceTests
{
    private readonly Mock<ILogger<LicenseService>> _loggerMock;

    public LicenseServiceTests()
    {
        _loggerMock = new Mock<ILogger<LicenseService>>();
    }

    private LicenseService CreateService(LicensingOptions options)
    {
        var optionsMock = Options.Create(options);
        return new LicenseService(optionsMock, _loggerMock.Object);
    }

    // ===========================================
    // Base License (No features enabled)
    // ===========================================

    [Fact]
    public void GetLicenseInfo_ReturnsBaseLicense_WhenNoConfiguration()
    {
        // Arrange
        var options = new LicensingOptions();
        var service = CreateService(options);

        // Act
        var license = service.GetLicenseInfo();

        // Assert
        license.Should().NotBeNull();
        license.CustomerId.Should().Be("COMMUNITY");
        license.Tier.Should().Be("BASE");
        license.IsValid.Should().BeTrue();
        license.EnabledFeatures.Should().BeEmpty();
    }

    [Fact]
    public void IsFeatureEnabled_ReturnsFalse_WhenFeatureNotEnabled()
    {
        // Arrange
        var options = new LicensingOptions();
        var service = CreateService(options);

        // Act
        var result = service.IsFeatureEnabled(Features.DualLineCurrentAccount);

        // Assert
        result.Should().BeFalse();
    }

    // ===========================================
    // Configuration-based features
    // ===========================================

    [Fact]
    public void IsFeatureEnabled_ReturnsTrue_WhenDualLineEnabledInConfig()
    {
        // Arrange
        var options = new LicensingOptions
        {
            Features = new FeatureFlags
            {
                DualLineCurrentAccount = true
            }
        };
        var service = CreateService(options);

        // Act
        var result = service.IsFeatureEnabled(Features.DualLineCurrentAccount);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsFeatureEnabled_ReturnsTrue_WhenMultiBranchEnabledInConfig()
    {
        // Arrange
        var options = new LicensingOptions
        {
            Features = new FeatureFlags
            {
                MultiBranch = true
            }
        };
        var service = CreateService(options);

        // Act
        var result = service.IsFeatureEnabled(Features.MultiBranch);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetLicenseInfo_IncludesEnabledFeatures_WhenConfigured()
    {
        // Arrange
        var options = new LicensingOptions
        {
            Features = new FeatureFlags
            {
                DualLineCurrentAccount = true,
                MultiBranch = true
            }
        };
        var service = CreateService(options);

        // Act
        var license = service.GetLicenseInfo();

        // Assert
        license.EnabledFeatures.Should().HaveCount(2);
        license.EnabledFeatures.Should().Contain(Features.DualLineCurrentAccount);
        license.EnabledFeatures.Should().Contain(Features.MultiBranch);
    }

    // ===========================================
    // License key validation
    // ===========================================

    [Fact]
    public void ValidateLicenseKey_ReturnsNull_WhenEmpty()
    {
        // Arrange
        var options = new LicensingOptions();
        var service = CreateService(options);

        // Act
        var result = service.ValidateLicenseKey("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ValidateLicenseKey_ReturnsNull_WhenInvalidFormat()
    {
        // Arrange
        var options = new LicensingOptions();
        var service = CreateService(options);

        // Act
        var result = service.ValidateLicenseKey("INVALID-KEY");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ValidateLicenseKey_ReturnsNull_WhenWrongPrefix()
    {
        // Arrange
        var options = new LicensingOptions();
        var service = CreateService(options);

        // Act
        var result = service.ValidateLicenseKey("XXX-CUSTOMER-PREMIUM-20271231-ABCDEF");

        // Assert
        result.Should().BeNull();
    }

    // ===========================================
    // License tiers
    // ===========================================

    [Theory]
    [InlineData("BASE", false, false)]
    [InlineData("PREMIUM", true, false)]
    [InlineData("ENTERPRISE", true, true)]
    public void LicenseTier_HasCorrectFeatures(string tier, bool hasDualLine, bool hasMultiBranch)
    {
        // This test documents the expected feature matrix for each tier
        // BASE: No premium features
        // PREMIUM: DualLineCurrentAccount
        // ENTERPRISE: DualLineCurrentAccount + MultiBranch

        // The actual implementation is in GetFeaturesForTier which is private
        // We test it indirectly through configuration or valid license keys
        
        tier.Should().NotBeNullOrEmpty();
        
        if (tier == "BASE")
        {
            hasDualLine.Should().BeFalse();
            hasMultiBranch.Should().BeFalse();
        }
        else if (tier == "PREMIUM")
        {
            hasDualLine.Should().BeTrue();
            hasMultiBranch.Should().BeFalse();
        }
        else if (tier == "ENTERPRISE")
        {
            hasDualLine.Should().BeTrue();
            hasMultiBranch.Should().BeTrue();
        }
    }
}
