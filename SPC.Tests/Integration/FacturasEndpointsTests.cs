using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SPC.API.Contracts.Facturas;
using SPC.Tests.Infrastructure;

namespace SPC.Tests.Integration;

/// <summary>
/// Integration tests for Facturas endpoints including business rule calculations.
/// </summary>
public class FacturasEndpointsTests : IClassFixture<SPCWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FacturasEndpointsTests(SPCWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetFacturas_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/facturas");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetFacturaById_ReturnsNotFound_WhenDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/facturas/99999");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateFactura_ReturnsCreated_WithValidData()
    {
        // Arrange
        var request = new CreateFacturaRequest
        {
            BranchId = 1,
            TipoFactura = "B",
            ClienteId = 1,
            PorcentajeDescuento = 0,
            Detalles = new List<CreateFacturaDetalleRequest>
            {
                new CreateFacturaDetalleRequest
                {
                    ProductoId = 1,
                    Cantidad = 2,
                    PorcentajeDescuento = 0
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var factura = await response.Content.ReadFromJsonAsync<FacturaCompletaResponse>();
        factura.Should().NotBeNull();
        factura!.ClienteId.Should().Be(1);
        factura.Detalles.Should().HaveCount(1);
        factura.Detalles[0].Cantidad.Should().Be(2);
    }

    [Fact]
    public async Task CreateFactura_CalculatesVATCorrectly()
    {
        // Arrange - Product 1 has PrecioFactura = 1000, PrecioPresupuesto = 1210, IVA = 21%
        // Using Factura A (net prices + VAT discriminated)
        var request = new CreateFacturaRequest
        {
            BranchId = 1,
            TipoFactura = "A",  // Changed to A for discriminated VAT
            ClienteId = 1,
            PorcentajeDescuento = 0,
            Detalles = new List<CreateFacturaDetalleRequest>
            {
                new CreateFacturaDetalleRequest
                {
                    ProductoId = 1,
                    Cantidad = 1,
                    PorcentajeDescuento = 0
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<FacturaCompletaResponse>();

        // Assert
        // Factura A: Uses PrecioFactura = 1000
        // Subtotal = 1000 (1 x 1000)
        // VAT = 1000 * 21% = 210
        // Total = 1000 + 210 = 1210
        factura.Should().NotBeNull();
        factura!.Subtotal.Should().Be(1000m);
        factura.ImporteIVA.Should().Be(210m);
        factura.Total.Should().Be(1210m);
    }

    [Fact]
    public async Task CreateFactura_AppliesLineDiscount()
    {
        // Arrange - Product 1: PrecioFactura = 1000, apply 10% line discount
        // Using Factura A (net prices + VAT discriminated)
        var request = new CreateFacturaRequest
        {
            BranchId = 1,
            TipoFactura = "A",  // Changed to A
            ClienteId = 1,
            PorcentajeDescuento = 0,
            Detalles = new List<CreateFacturaDetalleRequest>
            {
                new CreateFacturaDetalleRequest
                {
                    ProductoId = 1,
                    Cantidad = 1,
                    PorcentajeDescuento = 10 // 10% line discount
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<FacturaCompletaResponse>();

        // Assert
        // Factura A: Gross = 1000, Line discount = 100, Subtotal = 900
        // VAT = 900 * 21% = 189
        // Total = 900 + 189 = 1089
        factura.Should().NotBeNull();
        factura!.Subtotal.Should().Be(900m);
        factura.ImporteIVA.Should().Be(189m);
        factura.Total.Should().Be(1089m);
    }

    [Fact]
    public async Task CreateFactura_AppliesDocumentDiscount()
    {
        // Arrange - Apply 20% document discount (overrides customer default)
        // Using Factura A (net prices + VAT discriminated)
        var request = new CreateFacturaRequest
        {
            BranchId = 1,
            TipoFactura = "A",  // Changed to A
            ClienteId = 1,
            PorcentajeDescuento = 20, // 20% document discount
            Detalles = new List<CreateFacturaDetalleRequest>
            {
                new CreateFacturaDetalleRequest
                {
                    ProductoId = 1,
                    Cantidad = 1,
                    PorcentajeDescuento = 0
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<FacturaCompletaResponse>();

        // Assert
        // Factura A: Lines subtotal = 1000
        // Document discount = 1000 * 20% = 200
        // Net subtotal = 800
        // VAT = 800 * 21% = 168
        // Total = 800 + 168 = 968
        factura.Should().NotBeNull();
        factura!.ImporteDescuento.Should().Be(200m);
        factura.Subtotal.Should().Be(800m);
        factura.ImporteIVA.Should().Be(168m);
        factura.Total.Should().Be(968m);
    }

    [Fact]
    public async Task CreateFactura_AppliesMultipleLevelDiscounts()
    {
        // Arrange - Line discount 10% + Document discount 10%
        // Using Factura A (net prices + VAT discriminated)
        var request = new CreateFacturaRequest
        {
            BranchId = 1,
            TipoFactura = "A",  // Changed to A
            ClienteId = 1,
            PorcentajeDescuento = 10, // 10% document discount
            Detalles = new List<CreateFacturaDetalleRequest>
            {
                new CreateFacturaDetalleRequest
                {
                    ProductoId = 1,
                    Cantidad = 1,
                    PorcentajeDescuento = 10 // 10% line discount
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<FacturaCompletaResponse>();

        // Assert
        // Factura A: Gross = 1000, Line discount = 100, Line subtotal = 900
        // Document discount = 900 * 10% = 90
        // Net subtotal = 810
        // VAT = 810 * 21% = 170.10
        // Total = 810 + 170.10 = 980.10
        factura.Should().NotBeNull();
        factura!.ImporteDescuento.Should().Be(90m);
        factura.Subtotal.Should().Be(810m);
        factura.ImporteIVA.Should().Be(170.10m);
        factura.Total.Should().Be(980.10m);
    }

    [Fact]
    public async Task CreateFactura_StoresVATPercentageForImmutability()
    {
        // Arrange - Using Factura A for discriminated VAT
        var request = new CreateFacturaRequest
        {
            BranchId = 1,
            TipoFactura = "A",  // Changed to A for discriminated VAT
            ClienteId = 1,
            Detalles = new List<CreateFacturaDetalleRequest>
            {
                new CreateFacturaDetalleRequest { ProductoId = 1, Cantidad = 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<FacturaCompletaResponse>();

        // Assert - VAT percentage should be stored in the document
        factura.Should().NotBeNull();
        
        // Verify by fetching the invoice again
        var getResponse = await _client.GetAsync($"/api/facturas/{factura!.Id}");
        var fetchedFactura = await getResponse.Content.ReadFromJsonAsync<FacturaCompletaResponse>();
        
        // Factura A: The VAT rate (21%) should be preserved even if system VAT changes later
        // For Factura A with Product 1: Subtotal = 1000 (PrecioFactura), IVA = 210
        fetchedFactura!.ImporteIVA.Should().Be(210m);  // 1000 * 21%
    }

    [Fact]
    public async Task CreateFactura_ReturnsBadRequest_WhenClienteNotFound()
    {
        // Arrange
        var request = new CreateFacturaRequest
        {
            BranchId = 1,
            TipoFactura = "B",
            ClienteId = 99999, // Non-existent
            Detalles = new List<CreateFacturaDetalleRequest>
            {
                new CreateFacturaDetalleRequest { ProductoId = 1, Cantidad = 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AnularFactura_ReturnsOk_WhenExists()
    {
        // Arrange - First create an invoice
        var createRequest = new CreateFacturaRequest
        {
            BranchId = 1,
            TipoFactura = "B",
            ClienteId = 1,
            Detalles = new List<CreateFacturaDetalleRequest>
            {
                new CreateFacturaDetalleRequest { ProductoId = 1, Cantidad = 1 }
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/facturas", createRequest);
        var factura = await createResponse.Content.ReadFromJsonAsync<FacturaCompletaResponse>();

        // Act - Void the invoice
        var anularRequest = new AnularFacturaRequest { Motivo = "Test void" };
        var response = await _client.PostAsJsonAsync($"/api/facturas/{factura!.Id}/anular", anularRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify it's voided
        var getResponse = await _client.GetAsync($"/api/facturas/{factura.Id}");
        var voidedFactura = await getResponse.Content.ReadFromJsonAsync<FacturaCompletaResponse>();
        voidedFactura!.Anulada.Should().BeTrue();
    }
}
