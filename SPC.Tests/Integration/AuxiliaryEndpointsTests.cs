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
        var response = await _client.GetAsync("/api/condicionesiva");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var condiciones = await response.Content.ReadFromJsonAsync<List<CondicionIva>>();
        condiciones.Should().NotBeNull();
        condiciones.Should().HaveCount(4);
        condiciones.Should().Contain(c => c.Codigo == "RI" && c.Descripcion == "Responsable Inscripto");
        condiciones.Should().Contain(c => c.Codigo == "MO" && c.Descripcion == "Monotributo");
        condiciones.Should().Contain(c => c.Codigo == "CF" && c.Descripcion == "Consumidor Final");
        condiciones.Should().Contain(c => c.Codigo == "EX" && c.Descripcion == "Exento");
    }

    [Fact]
    public async Task GetCondicionesIva_ReturnsCorrectTipoFactura()
    {
        // Act
        var response = await _client.GetAsync("/api/condicionesiva");
        var condiciones = await response.Content.ReadFromJsonAsync<List<CondicionIva>>();

        // Assert - Responsable Inscripto gets Factura A, others get B
        condiciones!.First(c => c.Codigo == "RI").TipoFactura.Should().Be("A");
        condiciones.First(c => c.Codigo == "MO").TipoFactura.Should().Be("B");
        condiciones.First(c => c.Codigo == "CF").TipoFactura.Should().Be("B");
        condiciones.First(c => c.Codigo == "EX").TipoFactura.Should().Be("B");
    }

    // ===========================================
    // Rubros
    // ===========================================

    [Fact]
    public async Task GetRubros_ReturnsSeedData()
    {
        // Act
        var response = await _client.GetAsync("/api/rubros");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rubros = await response.Content.ReadFromJsonAsync<List<Rubro>>();
        rubros.Should().NotBeNull();
        rubros.Should().HaveCount(4);
        rubros.Should().Contain(r => r.Nombre == "Baterias Auto");
        rubros.Should().Contain(r => r.Nombre == "Baterias Moto");
        rubros.Should().Contain(r => r.Nombre == "Baterias Camion");
        rubros.Should().Contain(r => r.Nombre == "Accesorios");
    }

    // ===========================================
    // Depositos
    // ===========================================

    [Fact]
    public async Task GetDepositos_ReturnsSeedData()
    {
        // Act
        var response = await _client.GetAsync("/api/depositos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var depositos = await response.Content.ReadFromJsonAsync<List<Deposito>>();
        depositos.Should().NotBeNull();
        depositos.Should().ContainSingle(d => d.Nombre == "Deposito Principal");
    }

    // ===========================================
    // Vendedores
    // ===========================================

    [Fact]
    public async Task GetVendedores_ReturnsEmptyList_WhenNoVendedores()
    {
        // Act
        var response = await _client.GetAsync("/api/vendedores");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var vendedores = await response.Content.ReadFromJsonAsync<List<Vendedor>>();
        vendedores.Should().NotBeNull();
        vendedores.Should().BeEmpty(); // No seed data for vendedores
    }

    [Fact]
    public async Task PostVendedor_CreatesVendedor_ReturnsCreated()
    {
        // Arrange
        var nuevoVendedor = new Vendedor
        {
            Legajo = "V001",
            Nombre = "Juan",
            Apellido = "Perez",
            PorcentajeComision = 5.0m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/vendedores", nuevoVendedor);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var vendedorCreado = await response.Content.ReadFromJsonAsync<Vendedor>();
        vendedorCreado.Should().NotBeNull();
        vendedorCreado!.Id.Should().BeGreaterThan(0);
        vendedorCreado.Legajo.Should().Be("V001");
        vendedorCreado.Activo.Should().BeTrue();
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
        var zonas = await response.Content.ReadFromJsonAsync<List<ZonaVenta>>();
        zonas.Should().NotBeNull();
        zonas.Should().BeEmpty(); // No seed data for zonas
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
