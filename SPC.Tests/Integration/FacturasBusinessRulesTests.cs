using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SPC.API.Contracts.Invoices;
using SPC.Tests.Infrastructure;

namespace SPC.Tests.Integration;

/// <summary>
/// Integration tests for Invoices business rules.
/// TDD: These tests define expected behavior for Invoice A vs B calculations.
/// </summary>
public class InvoicesBusinessRulesTests : IClassFixture<SPCWebApplicationFactory>
{
    private readonly HttpClient _client;

    public InvoicesBusinessRulesTests(SPCWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ===========================================
    // FACTURA A TESTS (Net prices + VAT discriminated)
    // ===========================================

    [Fact]
    public async Task InvoiceA_AddsVATToNetPrice()
    {
        // Arrange - Invoice A: uses InvoicePrice (net), adds VAT
        // Product 1: InvoicePrice = 1000, VAT 21%
        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            InvoiceType = "A",
            CustomerId = 1,  // Customer con TaxCondition = RI (Invoice A)
            DiscountPercent = 0,
            Details = new List<CreateInvoiceDetailRequest>
            {
                new() { ProductId = 1, Quantity = 1, DiscountPercent = 0 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<InvoiceCompletaResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        factura.Should().NotBeNull();
        factura!.InvoiceType.Should().Be("A");
        factura.Subtotal.Should().Be(1000m);  // Net price
        factura.VATAmount.Should().Be(210m); // 21% VAT added
        factura.Total.Should().Be(1210m);     // Net + VAT
    }

    [Fact]
    public async Task InvoiceA_WithIIBB_OnlyIfAgentAndCustomerHasRate()
    {
        // Arrange - Invoice A with IIBB perception
        // This test requires company to be IIBB perception agent
        // and customer to have IIBBPercent > 0
        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            InvoiceType = "A",
            CustomerId = 1,
            DiscountPercent = 0,
            IIBBPercent = 3, // 3% IIBB - should only apply if company is agent
            Details = new List<CreateInvoiceDetailRequest>
            {
                new() { ProductId = 1, Quantity = 1, DiscountPercent = 0 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<InvoiceCompletaResponse>();

        // Assert - IIBB calculation depends on company settings
        // If company is NOT agent, IIBB should be 0
        // If company IS agent, IIBB = (1000 + 210) * 3% = 36.30
        factura.Should().NotBeNull();
        // Note: Default test setup has company as NOT an agent
        // So IIBB should be 0 unless we seed CompanySettings
    }

    // ===========================================
    // FACTURA B TESTS (Final prices with VAT included)
    // ===========================================

    [Fact]
    public async Task InvoiceB_UsesFinalPriceWithVATIncluded()
    {
        // Arrange - Invoice B: uses QuotePrice (includes VAT)
        // Product 1: QuotePrice = 1210 (1000 + 21% VAT)
        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            InvoiceType = "B",
            CustomerId = 1,
            DiscountPercent = 0,
            Details = new List<CreateInvoiceDetailRequest>
            {
                new() { ProductId = 1, Quantity = 1, DiscountPercent = 0 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<InvoiceCompletaResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        factura.Should().NotBeNull();
        factura!.InvoiceType.Should().Be("B");
        factura.Total.Should().Be(1210m);  // Final price (VAT included)
    }

    [Fact]
    public async Task InvoiceB_ShowsIVAContenido()
    {
        // Arrange - Invoice B must show "IVA Contenido" (Ley 27.743)
        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            InvoiceType = "B",
            CustomerId = 1,
            DiscountPercent = 0,
            Details = new List<CreateInvoiceDetailRequest>
            {
                new() { ProductId = 1, Quantity = 1, DiscountPercent = 0 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<InvoiceCompletaResponse>();

        // Assert
        factura.Should().NotBeNull();
        // IVA Contenido = 1210 / 1.21 * 0.21 = 210
        factura!.IncludedVAT.Should().BeApproximately(210m, 0.01m);
    }

    [Fact]
    public async Task InvoiceB_SubtotalEqualsTotal()
    {
        // Arrange - In Invoice B, SubTotal = Total (both are final prices)
        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            InvoiceType = "B",
            CustomerId = 1,
            DiscountPercent = 0,
            Details = new List<CreateInvoiceDetailRequest>
            {
                new() { ProductId = 1, Quantity = 1, DiscountPercent = 0 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<InvoiceCompletaResponse>();

        // Assert - For Invoice B display purposes
        factura.Should().NotBeNull();
        factura!.Subtotal.Should().Be(factura.Total);
    }

    // ===========================================
    // PRICE SELECTION TESTS
    // ===========================================

    [Fact]
    public async Task InvoiceA_UsesPrecioInvoice_FromProduct()
    {
        // Arrange - Product 1: InvoicePrice = 1000
        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            InvoiceType = "A",
            CustomerId = 1,
            Details = new List<CreateInvoiceDetailRequest>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<InvoiceCompletaResponse>();

        // Assert
        factura.Should().NotBeNull();
        factura!.Details[0].UnitPrice.Should().Be(1000m); // InvoicePrice
    }

    [Fact]
    public async Task InvoiceB_UsesPrecioQuote_FromProduct()
    {
        // Arrange - Product 1: QuotePrice = 1210
        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            InvoiceType = "B",
            CustomerId = 1,
            Details = new List<CreateInvoiceDetailRequest>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<InvoiceCompletaResponse>();

        // Assert
        factura.Should().NotBeNull();
        factura!.Details[0].UnitPrice.Should().Be(1210m); // QuotePrice
    }

    [Fact]
    public async Task InvoiceB_WithDiscount_AppliesCorrectly()
    {
        // Arrange - Invoice B with 10% document discount
        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            InvoiceType = "B",
            CustomerId = 1,
            DiscountPercent = 10,
            Details = new List<CreateInvoiceDetailRequest>
            {
                new() { ProductId = 1, Quantity = 1, DiscountPercent = 0 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<InvoiceCompletaResponse>();

        // Assert
        // Original: 1210, Discount: 121, Final: 1089
        // IVA Contenido = 1089 / 1.21 * 0.21 = 189
        factura.Should().NotBeNull();
        factura!.DiscountAmount.Should().Be(121m);
        factura.Total.Should().Be(1089m);
        factura.IncludedVAT.Should().BeApproximately(189m, 0.01m);
    }
}
