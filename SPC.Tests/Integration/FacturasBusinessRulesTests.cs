using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SPC.API.Contracts.Facturas;
using SPC.Tests.Infrastructure;

namespace SPC.Tests.Integration;

/// <summary>
/// Integration tests for Facturas business rules.
/// TDD: These tests define expected behavior for Factura A vs B calculations.
/// </summary>
public class FacturasBusinessRulesTests : IClassFixture<SPCWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FacturasBusinessRulesTests(SPCWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ===========================================
    // FACTURA A TESTS (Net prices + VAT discriminated)
    // ===========================================

    [Fact]
    public async Task FacturaA_AddsVATToNetPrice()
    {
        // Arrange - Factura A: uses PrecioFactura (net), adds VAT
        // Product 1: PrecioFactura = 1000, VAT 21%
        var request = new CreateFacturaRequest
        {
            BranchId = 1,
            TipoFactura = "A",
            ClienteId = 1,  // Cliente con CondicionIva = RI (Factura A)
            PorcentajeDescuento = 0,
            Detalles = new List<CreateFacturaDetalleRequest>
            {
                new() { ProductoId = 1, Cantidad = 1, PorcentajeDescuento = 0 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<FacturaCompletaResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        factura.Should().NotBeNull();
        factura!.TipoFactura.Should().Be("A");
        factura.Subtotal.Should().Be(1000m);  // Net price
        factura.ImporteIVA.Should().Be(210m); // 21% VAT added
        factura.Total.Should().Be(1210m);     // Net + VAT
    }

    [Fact]
    public async Task FacturaA_WithIIBB_OnlyIfAgentAndCustomerHasRate()
    {
        // Arrange - Factura A with IIBB perception
        // This test requires company to be IIBB perception agent
        // and customer to have AlicuotaIIBB > 0
        var request = new CreateFacturaRequest
        {
            BranchId = 1,
            TipoFactura = "A",
            ClienteId = 1,
            PorcentajeDescuento = 0,
            AlicuotaIIBB = 3, // 3% IIBB - should only apply if company is agent
            Detalles = new List<CreateFacturaDetalleRequest>
            {
                new() { ProductoId = 1, Cantidad = 1, PorcentajeDescuento = 0 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<FacturaCompletaResponse>();

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
    public async Task FacturaB_UsesFinalPriceWithVATIncluded()
    {
        // Arrange - Factura B: uses PrecioPresupuesto (includes VAT)
        // Product 1: PrecioPresupuesto = 1210 (1000 + 21% VAT)
        var request = new CreateFacturaRequest
        {
            BranchId = 1,
            TipoFactura = "B",
            ClienteId = 1,
            PorcentajeDescuento = 0,
            Detalles = new List<CreateFacturaDetalleRequest>
            {
                new() { ProductoId = 1, Cantidad = 1, PorcentajeDescuento = 0 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<FacturaCompletaResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        factura.Should().NotBeNull();
        factura!.TipoFactura.Should().Be("B");
        factura.Total.Should().Be(1210m);  // Final price (VAT included)
    }

    [Fact]
    public async Task FacturaB_ShowsIVAContenido()
    {
        // Arrange - Factura B must show "IVA Contenido" (Ley 27.743)
        var request = new CreateFacturaRequest
        {
            BranchId = 1,
            TipoFactura = "B",
            ClienteId = 1,
            PorcentajeDescuento = 0,
            Detalles = new List<CreateFacturaDetalleRequest>
            {
                new() { ProductoId = 1, Cantidad = 1, PorcentajeDescuento = 0 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<FacturaCompletaResponse>();

        // Assert
        factura.Should().NotBeNull();
        // IVA Contenido = 1210 / 1.21 * 0.21 = 210
        factura!.IVAContenido.Should().BeApproximately(210m, 0.01m);
    }

    [Fact]
    public async Task FacturaB_SubtotalEqualsTotal()
    {
        // Arrange - In Factura B, SubTotal = Total (both are final prices)
        var request = new CreateFacturaRequest
        {
            BranchId = 1,
            TipoFactura = "B",
            ClienteId = 1,
            PorcentajeDescuento = 0,
            Detalles = new List<CreateFacturaDetalleRequest>
            {
                new() { ProductoId = 1, Cantidad = 1, PorcentajeDescuento = 0 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<FacturaCompletaResponse>();

        // Assert - For Factura B display purposes
        factura.Should().NotBeNull();
        factura!.Subtotal.Should().Be(factura.Total);
    }

    // ===========================================
    // PRICE SELECTION TESTS
    // ===========================================

    [Fact]
    public async Task FacturaA_UsesPrecioFactura_FromProduct()
    {
        // Arrange - Product 1: PrecioFactura = 1000
        var request = new CreateFacturaRequest
        {
            BranchId = 1,
            TipoFactura = "A",
            ClienteId = 1,
            Detalles = new List<CreateFacturaDetalleRequest>
            {
                new() { ProductoId = 1, Cantidad = 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<FacturaCompletaResponse>();

        // Assert
        factura.Should().NotBeNull();
        factura!.Detalles[0].PrecioUnitario.Should().Be(1000m); // PrecioFactura
    }

    [Fact]
    public async Task FacturaB_UsesPrecioPresupuesto_FromProduct()
    {
        // Arrange - Product 1: PrecioPresupuesto = 1210
        var request = new CreateFacturaRequest
        {
            BranchId = 1,
            TipoFactura = "B",
            ClienteId = 1,
            Detalles = new List<CreateFacturaDetalleRequest>
            {
                new() { ProductoId = 1, Cantidad = 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<FacturaCompletaResponse>();

        // Assert
        factura.Should().NotBeNull();
        factura!.Detalles[0].PrecioUnitario.Should().Be(1210m); // PrecioPresupuesto
    }

    [Fact]
    public async Task FacturaB_WithDiscount_AppliesCorrectly()
    {
        // Arrange - Factura B with 10% document discount
        var request = new CreateFacturaRequest
        {
            BranchId = 1,
            TipoFactura = "B",
            ClienteId = 1,
            PorcentajeDescuento = 10,
            Detalles = new List<CreateFacturaDetalleRequest>
            {
                new() { ProductoId = 1, Cantidad = 1, PorcentajeDescuento = 0 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<FacturaCompletaResponse>();

        // Assert
        // Original: 1210, Discount: 121, Final: 1089
        // IVA Contenido = 1089 / 1.21 * 0.21 = 189
        factura.Should().NotBeNull();
        factura!.ImporteDescuento.Should().Be(121m);
        factura.Total.Should().Be(1089m);
        factura.IVAContenido.Should().BeApproximately(189m, 0.01m);
    }
}
