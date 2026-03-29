using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SPC.API.Contracts.CurrentAccount;
using SPC.Tests.Infrastructure;

namespace SPC.Tests.Integration;

/// <summary>
/// Integration tests for /api/current-accounts endpoints.
/// Tests the full request/response cycle with InMemory database.
/// </summary>
public class CurrentAccountEndpointsTests : IClassFixture<SPCWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SPCWebApplicationFactory _factory;

    public CurrentAccountEndpointsTests(SPCWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCurrentAccount_ReturnsOk_WhenCustomerExists()
    {
        // Act - Use seeded customer ID 1
        var response = await _client.GetAsync("/api/current-accounts/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var account = await response.Content.ReadFromJsonAsync<CurrentAccountResponse>();
        account.Should().NotBeNull();
        account!.CustomerId.Should().Be(1);
    }

    [Fact]
    public async Task GetCurrentAccount_ReturnsNotFound_WhenCustomerDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/current-accounts/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCurrentAccountMovements_ReturnsOk_WhenCustomerExists()
    {
        // Act - Use seeded customer ID 1
        var response = await _client.GetAsync("/api/current-accounts/1/movements");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CurrentAccountMovementsResponse>();
        result.Should().NotBeNull();
        result!.CustomerId.Should().Be(1);
        result.Movements.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCurrentAccountMovements_ReturnsNotFound_WhenCustomerDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/current-accounts/99999/movements");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCurrentAccountMovements_SupportsDateFilters()
    {
        // Act
        var dateFrom = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
        var dateTo = DateTime.Now.ToString("yyyy-MM-dd");
        var response = await _client.GetAsync($"/api/current-accounts/1/movements?dateFrom={dateFrom}&dateTo={dateTo}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CurrentAccountMovementsResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCurrentAccountMovements_SupportsLineFilter_Billing()
    {
        // Act - Filter to Billing (L1) only
        var response = await _client.GetAsync("/api/current-accounts/1/movements?line=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CurrentAccountMovementsResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCurrentAccountMovements_SupportsLineFilter_Budget()
    {
        // Act - Filter to Budget (L2) only
        var response = await _client.GetAsync("/api/current-accounts/1/movements?line=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CurrentAccountMovementsResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCurrentAccountMovements_ReturnsBadRequest_ForInvalidLineFilter()
    {
        // Act - Invalid line filter
        var response = await _client.GetAsync("/api/current-accounts/1/movements?line=3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCurrentAccountMovements_SupportsPagination()
    {
        // Act
        var response = await _client.GetAsync("/api/current-accounts/1/movements?skip=0&take=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CurrentAccountMovementsResponse>();
        result.Should().NotBeNull();
        result!.Movements.Count.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public async Task GetCurrentAccountMovements_ReturnsMovementsInAscendingOrder()
    {
        // Act
        var response = await _client.GetAsync("/api/current-accounts/1/movements");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CurrentAccountMovementsResponse>();
        result.Should().NotBeNull();

        // Verify ascending order (oldest first)
        if (result!.Movements.Count > 1)
        {
            for (int i = 1; i < result.Movements.Count; i++)
            {
                result.Movements[i].MovementDate.Should().BeOnOrAfter(result.Movements[i - 1].MovementDate);
            }
        }
    }
}
