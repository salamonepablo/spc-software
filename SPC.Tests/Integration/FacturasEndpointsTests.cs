using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SPC.API.Contracts.Invoices;
using SPC.Tests.Infrastructure;

namespace SPC.Tests.Integration;

/// <summary>
/// Integration tests for Invoices endpoints including business rule calculations.
/// </summary>
public class InvoicesEndpointsTests : IClassFixture<SPCWebApplicationFactory>
{
    private readonly HttpClient _client;

    public InvoicesEndpointsTests(SPCWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetInvoices_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/facturas");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetInvoiceById_ReturnsNotFound_WhenDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/facturas/99999");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateInvoice_ReturnsCreated_WithValidData()
    {
        // Arrange
        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            TipoInvoice = "B",
            CustomerId = 1,
            PorcentajeDescuento = 0,
            Detalles = new List<CreateInvoiceDetailRequest>
            {
                new CreateInvoiceDetailRequest
                {
                    ProductId = 1,
                    Cantidad = 2,
                    PorcentajeDescuento = 0
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var factura = await response.Content.ReadFromJsonAsync<InvoiceCompletaResponse>();
        factura.Should().NotBeNull();
        factura!.CustomerId.Should().Be(1);
        factura.Detalles.Should().HaveCount(1);
        factura.Detalles[0].Cantidad.Should().Be(2);
    }

    [Fact]
    public async Task CreateInvoice_CalculatesVATCorrectly()
    {
        // Arrange - Product 1 has PrecioInvoice = 1000, PrecioQuote = 1210, IVA = 21%
        // Using Invoice A (net prices + VAT discriminated)
        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            TipoInvoice = "A",  // Changed to A for discriminated VAT
            CustomerId = 1,
            PorcentajeDescuento = 0,
            Detalles = new List<CreateInvoiceDetailRequest>
            {
                new CreateInvoiceDetailRequest
                {
                    ProductId = 1,
                    Cantidad = 1,
                    PorcentajeDescuento = 0
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<InvoiceCompletaResponse>();

        // Assert
        // Invoice A: Uses PrecioInvoice = 1000
        // Subtotal = 1000 (1 x 1000)
        // VAT = 1000 * 21% = 210
        // Total = 1000 + 210 = 1210
        factura.Should().NotBeNull();
        factura!.Subtotal.Should().Be(1000m);
        factura.ImporteIVA.Should().Be(210m);
        factura.Total.Should().Be(1210m);
    }

    [Fact]
    public async Task CreateInvoice_AppliesLineDiscount()
    {
        // Arrange - Product 1: PrecioInvoice = 1000, apply 10% line discount
        // Using Invoice A (net prices + VAT discriminated)
        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            TipoInvoice = "A",  // Changed to A
            CustomerId = 1,
            PorcentajeDescuento = 0,
            Detalles = new List<CreateInvoiceDetailRequest>
            {
                new CreateInvoiceDetailRequest
                {
                    ProductId = 1,
                    Cantidad = 1,
                    PorcentajeDescuento = 10 // 10% line discount
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<InvoiceCompletaResponse>();

        // Assert
        // Invoice A: Gross = 1000, Line discount = 100, Subtotal = 900
        // VAT = 900 * 21% = 189
        // Total = 900 + 189 = 1089
        factura.Should().NotBeNull();
        factura!.Subtotal.Should().Be(900m);
        factura.ImporteIVA.Should().Be(189m);
        factura.Total.Should().Be(1089m);
    }

    [Fact]
    public async Task CreateInvoice_AppliesDocumentDiscount()
    {
        // Arrange - Apply 20% document discount (overrides customer default)
        // Using Invoice A (net prices + VAT discriminated)
        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            TipoInvoice = "A",  // Changed to A
            CustomerId = 1,
            PorcentajeDescuento = 20, // 20% document discount
            Detalles = new List<CreateInvoiceDetailRequest>
            {
                new CreateInvoiceDetailRequest
                {
                    ProductId = 1,
                    Cantidad = 1,
                    PorcentajeDescuento = 0
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<InvoiceCompletaResponse>();

        // Assert
        // Invoice A: Lines subtotal = 1000
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
    public async Task CreateInvoice_AppliesMultipleLevelDiscounts()
    {
        // Arrange - Line discount 10% + Document discount 10%
        // Using Invoice A (net prices + VAT discriminated)
        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            TipoInvoice = "A",  // Changed to A
            CustomerId = 1,
            PorcentajeDescuento = 10, // 10% document discount
            Detalles = new List<CreateInvoiceDetailRequest>
            {
                new CreateInvoiceDetailRequest
                {
                    ProductId = 1,
                    Cantidad = 1,
                    PorcentajeDescuento = 10 // 10% line discount
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<InvoiceCompletaResponse>();

        // Assert
        // Invoice A: Gross = 1000, Line discount = 100, Line subtotal = 900
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
    public async Task CreateInvoice_StoresVATPercentageForImmutability()
    {
        // Arrange - Using Invoice A for discriminated VAT
        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            TipoInvoice = "A",  // Changed to A for discriminated VAT
            CustomerId = 1,
            Detalles = new List<CreateInvoiceDetailRequest>
            {
                new CreateInvoiceDetailRequest { ProductId = 1, Cantidad = 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        var factura = await response.Content.ReadFromJsonAsync<InvoiceCompletaResponse>();

        // Assert - VAT percentage should be stored in the document
        factura.Should().NotBeNull();
        
        // Verify by fetching the invoice again
        var getResponse = await _client.GetAsync($"/api/facturas/{factura!.Id}");
        var fetchedInvoice = await getResponse.Content.ReadFromJsonAsync<InvoiceCompletaResponse>();
        
        // Invoice A: The VAT rate (21%) should be preserved even if system VAT changes later
        // For Invoice A with Product 1: Subtotal = 1000 (PrecioInvoice), IVA = 210
        fetchedInvoice!.ImporteIVA.Should().Be(210m);  // 1000 * 21%
    }

    [Fact]
    public async Task CreateInvoice_ReturnsBadRequest_WhenCustomerNotFound()
    {
        // Arrange
        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            TipoInvoice = "B",
            CustomerId = 99999, // Non-existent
            Detalles = new List<CreateInvoiceDetailRequest>
            {
                new CreateInvoiceDetailRequest { ProductId = 1, Cantidad = 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/facturas", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AnularInvoice_ReturnsOk_WhenExists()
    {
        // Arrange - First create an invoice
        var createRequest = new CreateInvoiceRequest
        {
            BranchId = 1,
            TipoInvoice = "B",
            CustomerId = 1,
            Detalles = new List<CreateInvoiceDetailRequest>
            {
                new CreateInvoiceDetailRequest { ProductId = 1, Cantidad = 1 }
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/facturas", createRequest);
        var factura = await createResponse.Content.ReadFromJsonAsync<InvoiceCompletaResponse>();

        // Act - Void the invoice
        var anularRequest = new AnularInvoiceRequest { Motivo = "Test void" };
        var response = await _client.PostAsJsonAsync($"/api/facturas/{factura!.Id}/anular", anularRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify it's voided
        var getResponse = await _client.GetAsync($"/api/facturas/{factura.Id}");
        var voidedInvoice = await getResponse.Content.ReadFromJsonAsync<InvoiceCompletaResponse>();
        voidedInvoice!.Anulada.Should().BeTrue();
    }
}
