using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SPC.Shared.Models;
using SPC.Tests.Infrastructure;

namespace SPC.Tests.Integration;

/// <summary>
/// Integration tests for auxiliary table endpoints.
/// These endpoints return seed data.
/// </summary>
public class AuxiliaryEndpointsTests : IClassFixture<SPCWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuxiliaryEndpointsTests(SPCWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ===========================================
    // Root Endpoint
    // ===========================================

    [Fact]
    public async Task GetRoot_ReturnsSystemInfo()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<RootResponse>();
        content.Should().NotBeNull();
        content!.Sistema.Should().Be("SPC - Sistema de Gestion Comercial");
        content.Version.Should().Be("1.0");
        content.Endpoints.Should().Contain("/api/clientes");
    }

    // ===========================================
    // Condiciones IVA
    // ===========================================

    [Fact]
    public async Task GetCondicionesIva_ReturnsSeedData()
    {
        // Act
        var response = await _client.GetAsync("/api/TaxConditions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var condiciones = await response.Content.ReadFromJsonAsync<List<TaxCondition>>();
        condiciones.Should().NotBeNull();
        condiciones.Should().HaveCount(4);
        condiciones.Should().Contain(c => c.Code == "RI" && c.Description == "Responsable Inscripto");
        condiciones.Should().Contain(c => c.Code == "MO" && c.Description == "Monotributo");
        condiciones.Should().Contain(c => c.Code == "CF" && c.Description == "Consumidor Final");
        condiciones.Should().Contain(c => c.Code == "EX" && c.Description == "Exento");
    }

    [Fact]
    public async Task GetCondicionesIva_ReturnsCorrectTipoInvoice()
    {
        // Act
        var response = await _client.GetAsync("/api/TaxConditions");
        var condiciones = await response.Content.ReadFromJsonAsync<List<TaxCondition>>();

        // Assert - Responsable Inscripto gets Invoice A, others get B
        condiciones.Should().NotBeNull();
        var condicionesList = condiciones!;
        condicionesList.First(c => c.Code == "RI").InvoiceType.Should().Be("A");
        condicionesList.First(c => c.Code == "MO").InvoiceType.Should().Be("B");
        condicionesList.First(c => c.Code == "CF").InvoiceType.Should().Be("B");
        condicionesList.First(c => c.Code == "EX").InvoiceType.Should().Be("B");
    }

    // ===========================================
    // Categories
    // ===========================================

    [Fact]
    public async Task GetCategorys_ReturnsSeedData()
    {
        // Act
        var response = await _client.GetAsync("/api/rubros");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rubros = await response.Content.ReadFromJsonAsync<List<Category>>();
        rubros.Should().NotBeNull();
        rubros.Should().HaveCount(4);
        rubros.Should().Contain(r => r.Name == "Baterias Auto");
        rubros.Should().Contain(r => r.Name == "Baterias Moto");
        rubros.Should().Contain(r => r.Name == "Baterias Camion");
        rubros.Should().Contain(r => r.Name == "Accesorios");
    }

    // ===========================================
    // Warehouses
    // ===========================================

    [Fact]
    public async Task GetWarehouses_ReturnsSeedData()
    {
        // Act
        var response = await _client.GetAsync("/api/depositos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var depositos = await response.Content.ReadFromJsonAsync<List<Warehouse>>();
        depositos.Should().NotBeNull();
        depositos.Should().ContainSingle(d => d.Name == "Warehouse Principal");
    }

    // ===========================================
    // SalesReps
    // ===========================================

    [Fact]
    public async Task GetSalesRepes_ReturnsEmptyList_WhenNoSalesRepes()
    {
        // Act
        var response = await _client.GetAsync("/api/vendedores");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var vendedores = await response.Content.ReadFromJsonAsync<List<SalesRep>>();
        vendedores.Should().NotBeNull();
        vendedores.Should().BeEmpty(); // No seed data for vendedores
    }

    [Fact]
    public async Task PostSalesRep_CreatesSalesRep_ReturnsCreated()
    {
        // Arrange
        var nuevoSalesRep = new SalesRep
        {
            EmployeeCode = "V001",
            FirstName = "Juan",
            LastName = "Perez",
            CommissionPercent = 5.0m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/vendedores", nuevoSalesRep);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var vendedorCreado = await response.Content.ReadFromJsonAsync<SalesRep>();
        vendedorCreado.Should().NotBeNull();
        vendedorCreado!.Id.Should().BeGreaterThan(0);
        vendedorCreado.EmployeeCode.Should().Be("V001");
        vendedorCreado.IsActive.Should().BeTrue();
    }

    // ===========================================
    // Zonas de Venta
    // ===========================================

    [Fact]
    public async Task GetZonasVenta_ReturnsEmptyList_WhenNoZonas()
    {
        // Act
        var response = await _client.GetAsync("/api/zonasventas");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var zonas = await response.Content.ReadFromJsonAsync<List<SalesZone>>();
        zonas.Should().NotBeNull();
        zonas.Should().BeEmpty(); // No seed data for zonas
    }

    // ===========================================
    // Document Types
    // ===========================================

    [Fact]
    public async Task GetDocumentTypes_ReturnsSeedData()
    {
        // Act
        var response = await _client.GetAsync("/api/documenttypes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var documentTypes = await response.Content.ReadFromJsonAsync<List<DocumentTypeMaster>>();
        documentTypes.Should().NotBeNull();
        documentTypes.Should().HaveCount(6);
        documentTypes.Should().Contain(d => d.Code == "FA" && d.Description == "Factura");
        documentTypes.Should().Contain(d => d.Code == "NC" && d.Description == "Nota de Crédito");
        documentTypes.Should().Contain(d => d.Code == "ND" && d.Description == "Nota de Débito");
        documentTypes.Should().Contain(d => d.Code == "RE" && d.Description == "Recibo");
        documentTypes.Should().Contain(d => d.Code == "PR" && d.Description == "Presupuesto");
        documentTypes.Should().Contain(d => d.Code == "NDI" && d.Description == "Nota de Débito Interna");
    }

    [Fact]
    public async Task GetDocumentTypes_ReturnsCorrectBalanceImpact()
    {
        // Act
        var response = await _client.GetAsync("/api/documenttypes");
        var documentTypes = await response.Content.ReadFromJsonAsync<List<DocumentTypeMaster>>();

        // Assert - Check balance impact for different document types
        documentTypes.Should().NotBeNull();
        var types = documentTypes!;
        types.First(d => d.Code == "FA").BalanceImpact.Should().Be(1);  // Factura increases debt
        types.First(d => d.Code == "NC").BalanceImpact.Should().Be(-1); // NC decreases debt
        types.First(d => d.Code == "ND").BalanceImpact.Should().Be(1);  // ND increases debt
        types.First(d => d.Code == "RE").BalanceImpact.Should().Be(-1); // Recibo decreases debt
    }

    [Fact]
    public async Task GetDocumentTypes_ReturnsCorrectAccountLine()
    {
        // Act
        var response = await _client.GetAsync("/api/documenttypes");
        var documentTypes = await response.Content.ReadFromJsonAsync<List<DocumentTypeMaster>>();

        // Assert - Check which account line each document type affects
        documentTypes.Should().NotBeNull();
        var types = documentTypes!;
        types.First(d => d.Code == "FA").IsBillingLine.Should().BeTrue();   // Factura -> Billing
        types.First(d => d.Code == "NC").IsBillingLine.Should().BeTrue();   // NC -> Billing
        types.First(d => d.Code == "ND").IsBillingLine.Should().BeTrue();   // ND -> Billing
        types.First(d => d.Code == "PR").IsBillingLine.Should().BeFalse();  // Presupuesto -> Budget
        types.First(d => d.Code == "NDI").IsBillingLine.Should().BeFalse(); // NDI -> Budget
    }

    // ===========================================
    // Helper class for root response
    // ===========================================
    private class RootResponse
    {
        public string Sistema { get; set; } = "";
        public string Version { get; set; } = "";
        public string License { get; set; } = "";
        public string[] Endpoints { get; set; } = Array.Empty<string>();
    }
}
