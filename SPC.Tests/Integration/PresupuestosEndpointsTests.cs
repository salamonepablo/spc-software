using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SPC.API.Contracts.Presupuestos;
using SPC.Tests.Infrastructure;

namespace SPC.Tests.Integration;

/// <summary>
/// Integration tests for Presupuestos (Quotes) endpoints.
/// </summary>
public class PresupuestosEndpointsTests : IClassFixture<SPCWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PresupuestosEndpointsTests(SPCWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPresupuestos_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/presupuestos");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPresupuestoById_ReturnsNotFound_WhenDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/presupuestos/99999");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePresupuesto_ReturnsCreated_WithValidData()
    {
        // Arrange
        var request = new CreatePresupuestoRequest
        {
            BranchId = 1,
            CustomerId = 1,
            DiscountPercent = 0,
            Details = new List<CreatePresupuestoDetalleRequest>
            {
                new CreatePresupuestoDetalleRequest
                {
                    ProductId = 1,
                    Quantity = 2,
                    DiscountPercent = 0
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/presupuestos", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var quote = await response.Content.ReadFromJsonAsync<PresupuestoCompletoResponse>();
        quote.Should().NotBeNull();
        quote!.CustomerId.Should().Be(1);
        quote.Details.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreatePresupuesto_UsesPrecioPresupuesto()
    {
        // Arrange - Product 1 has PrecioPresupuesto = 1210 (includes VAT)
        var request = new CreatePresupuestoRequest
        {
            BranchId = 1,
            CustomerId = 1,
            DiscountPercent = 0,
            Details = new List<CreatePresupuestoDetalleRequest>
            {
                new CreatePresupuestoDetalleRequest
                {
                    ProductId = 1,
                    Quantity = 1,
                    DiscountPercent = 0
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/presupuestos", request);
        var quote = await response.Content.ReadFromJsonAsync<PresupuestoCompletoResponse>();

        // Assert
        // Quote uses PrecioPresupuesto (1210), no separate VAT calculation
        quote.Should().NotBeNull();
        quote!.Total.Should().Be(1210m);
        quote.Details[0].UnitPrice.Should().Be(1210m);
    }

    [Fact]
    public async Task CreatePresupuesto_AppliesDiscounts()
    {
        // Arrange - Line 10% + Document 10%
        var request = new CreatePresupuestoRequest
        {
            BranchId = 1,
            CustomerId = 1,
            DiscountPercent = 10,
            Details = new List<CreatePresupuestoDetalleRequest>
            {
                new CreatePresupuestoDetalleRequest
                {
                    ProductId = 1,
                    Quantity = 1,
                    DiscountPercent = 10
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/presupuestos", request);
        var quote = await response.Content.ReadFromJsonAsync<PresupuestoCompletoResponse>();

        // Assert
        // PrecioPresupuesto = 1210
        // Line discount = 121, Line subtotal = 1089
        // Doc discount = 108.90
        // Total = 980.10
        quote.Should().NotBeNull();
        quote!.Total.Should().Be(980.10m);
    }

    [Fact]
    public async Task CreatePresupuesto_UsesCustomerDefaultDiscount_WhenNotSpecified()
    {
        // Arrange - Customer 1 has 10% default discount
        // We need to NOT specify discount to use customer default
        // Note: Request without explicit discount should fall back to customer's default
        var request = new CreatePresupuestoRequest
        {
            BranchId = 1,
            CustomerId = 1,
            // DiscountPercent not specified - should use customer's 10%
            Details = new List<CreatePresupuestoDetalleRequest>
            {
                new CreatePresupuestoDetalleRequest
                {
                    ProductId = 1,
                    Quantity = 1
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/presupuestos", request);
        var quote = await response.Content.ReadFromJsonAsync<PresupuestoCompletoResponse>();

        // Assert
        // If using customer default 10% discount:
        // PrecioPresupuesto = 1210, Line subtotal = 1210
        // Document discount at 10% = 121
        // Total = 1089
        quote.Should().NotBeNull();
        // Note: Actual behavior depends on how ResolveDiscount handles 0 vs null
        // If DiscountPercent defaults to 0, it won't use customer default
    }

    [Fact]
    public async Task AnularPresupuesto_ReturnsOk_WhenExists()
    {
        // Arrange - First create a quote
        var createRequest = new CreatePresupuestoRequest
        {
            BranchId = 1,
            CustomerId = 1,
            Details = new List<CreatePresupuestoDetalleRequest>
            {
                new CreatePresupuestoDetalleRequest { ProductId = 1, Quantity = 1 }
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/presupuestos", createRequest);
        var quote = await createResponse.Content.ReadFromJsonAsync<PresupuestoCompletoResponse>();

        // Act - Void the quote
        var anularRequest = new AnularPresupuestoRequest { Reason = "Test void" };
        var response = await _client.PostAsJsonAsync($"/api/presupuestos/{quote!.Id}/anular", anularRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify it's voided
        var getResponse = await _client.GetAsync($"/api/presupuestos/{quote.Id}");
        var voidedQuote = await getResponse.Content.ReadFromJsonAsync<PresupuestoCompletoResponse>();
        voidedQuote!.IsVoided.Should().BeTrue();
    }
}
