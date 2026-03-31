using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SPC.Tests.Infrastructure;

namespace SPC.Tests.Integration;

/// <summary>
/// Integration tests for /api/license endpoint.
/// </summary>
public class LicenseEndpointTests : IClassFixture<SPCWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LicenseEndpointTests(SPCWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetLicense_ReturnsLicenseInfo()
    {
        // Act
        var response = await _client.GetAsync("/api/license");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var license = await response.Content.ReadFromJsonAsync<LicenseResponse>();
        license.Should().NotBeNull();
        license!.Tier.Should().NotBeNullOrEmpty();
        license.CustomerId.Should().NotBeNullOrEmpty();
        license.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetLicense_ReturnsBaseTier_WhenNoLicenseConfigured()
    {
        // Act
        var response = await _client.GetAsync("/api/license");
        var license = await response.Content.ReadFromJsonAsync<LicenseResponse>();

        // Assert - Default is BASE tier with no features
        license!.Tier.Should().Be("BASE");
        license.CustomerId.Should().Be("COMMUNITY");
    }

    [Fact]
    public async Task GetLicense_IncludesFeatureFlags()
    {
        // Act
        var response = await _client.GetAsync("/api/license");
        var license = await response.Content.ReadFromJsonAsync<LicenseResponse>();

        // Assert
        license!.Features.Should().NotBeNull();
        license.Features!.DualLineCurrentAccount.Should().BeFalse(); // Default disabled
        license.Features.MultiBranch.Should().BeFalse(); // Default disabled
    }

    // Helper class for deserializing license response
    private class LicenseResponse
    {
        public string Tier { get; set; } = "";
        public string CustomerId { get; set; } = "";
        public bool IsValid { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public FeatureResponse? Features { get; set; }
    }

    private class FeatureResponse
    {
        public bool DualLineCurrentAccount { get; set; }
        public bool MultiBranch { get; set; }
    }
}
