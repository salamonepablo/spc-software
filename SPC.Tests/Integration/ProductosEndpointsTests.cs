using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SPC.Shared.Models;
using SPC.Tests.Infrastructure;

namespace SPC.Tests.Integration;

/// <summary>
/// Integration tests for /api/productos endpoints.
/// </summary>
public class ProductosEndpointsTests : IClassFixture<SPCWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductosEndpointsTests(SPCWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProductos_ReturnsOkWithList()
    {
        // Act
        var response = await _client.GetAsync("/api/productos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var productos = await response.Content.ReadFromJsonAsync<List<Producto>>();
        productos.Should().NotBeNull();
        // List may contain data from other tests in same class (shared factory)
    }

    [Fact]
    public async Task PostProducto_CreatesProducto_ReturnsCreated()
    {
        // Arrange
        var nuevoProducto = new Producto
        {
            Codigo = "BAT-001",
            Descripcion = "Bateria 12V 75Ah Auto",
            PrecioVenta = 150000.00m,
            RubroId = 1, // Baterias Auto (seed data)
            UnidadMedidaId = 1 // Unidades (seed data)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/productos", nuevoProducto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var productoCreado = await response.Content.ReadFromJsonAsync<Producto>();
        productoCreado.Should().NotBeNull();
        productoCreado!.Id.Should().BeGreaterThan(0);
        productoCreado.Codigo.Should().Be("BAT-001");
        productoCreado.Descripcion.Should().Be("Bateria 12V 75Ah Auto");
        productoCreado.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task GetProductoById_ReturnsProducto_WhenExists()
    {
        // Arrange - Create a producto first
        var nuevoProducto = new Producto
        {
            Codigo = "BAT-002",
            Descripcion = "Bateria 12V 45Ah Moto",
            PrecioVenta = 50000.00m,
            RubroId = 2, // Baterias Moto
            UnidadMedidaId = 1
        };
        var createResponse = await _client.PostAsJsonAsync("/api/productos", nuevoProducto);
        var productoCreado = await createResponse.Content.ReadFromJsonAsync<Producto>();

        // Act
        var response = await _client.GetAsync($"/api/productos/{productoCreado!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var producto = await response.Content.ReadFromJsonAsync<Producto>();
        producto.Should().NotBeNull();
        producto!.Codigo.Should().Be("BAT-002");
    }

    [Fact]
    public async Task GetProductoById_ReturnsNotFound_WhenDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/productos/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BuscarProductos_ReturnsMatchingProductos_WhenSearchByDescription()
    {
        // Arrange - Create productos with unique prefix
        var prefix = Guid.NewGuid().ToString()[..8];
        var producto1 = new Producto { Codigo = $"{prefix}-100", Descripcion = $"{prefix} Bateria 60Ah", PrecioVenta = 100000, RubroId = 1, UnidadMedidaId = 1 };
        var producto2 = new Producto { Codigo = $"{prefix}-101", Descripcion = $"{prefix} Bateria 75Ah", PrecioVenta = 120000, RubroId = 1, UnidadMedidaId = 1 };
        var producto3 = new Producto { Codigo = $"{prefix}-ACC", Descripcion = $"{prefix} Cable arranque", PrecioVenta = 15000, RubroId = 4, UnidadMedidaId = 1 };

        await _client.PostAsJsonAsync("/api/productos", producto1);
        await _client.PostAsJsonAsync("/api/productos", producto2);
        await _client.PostAsJsonAsync("/api/productos", producto3);

        // Act - Search by unique prefix + Bateria
        var response = await _client.GetAsync($"/api/productos/buscar?descripcion={prefix}%20Bateria");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var productos = await response.Content.ReadFromJsonAsync<List<Producto>>();
        productos.Should().NotBeNull();
        productos.Should().HaveCount(2);
        productos.Should().OnlyContain(p => p.Descripcion.Contains($"{prefix} Bateria"));
    }

    [Fact]
    public async Task BuscarProductos_ReturnsMatchingProductos_WhenSearchByCodigo()
    {
        // Arrange
        var producto = new Producto { Codigo = "UNIQUE-CODE", Descripcion = "Producto Unico", PrecioVenta = 10000, RubroId = 1, UnidadMedidaId = 1 };
        await _client.PostAsJsonAsync("/api/productos", producto);

        // Act
        var response = await _client.GetAsync("/api/productos/buscar?descripcion=UNIQUE");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var productos = await response.Content.ReadFromJsonAsync<List<Producto>>();
        productos.Should().NotBeNull();
        productos.Should().ContainSingle(p => p.Codigo == "UNIQUE-CODE");
    }

    [Fact]
    public async Task BuscarProductos_ReturnsBadRequest_WhenNoSearchTerm()
    {
        // Act
        var response = await _client.GetAsync("/api/productos/buscar");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
