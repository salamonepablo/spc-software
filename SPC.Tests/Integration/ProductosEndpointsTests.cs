using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using System.Text.Json;
using SPC.API.Contracts.Products;
using SPC.Tests.Infrastructure;

namespace SPC.Tests.Integration;

/// <summary>
/// Integration tests for /api/productos endpoints.
/// </summary>
public class ProductsEndpointsTests : IClassFixture<SPCWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsEndpointsTests(SPCWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ReturnsOkWithList()
    {
        // Act
        var response = await _client.GetAsync("/api/productos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var productos = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        productos.Should().NotBeNull();
        // List may contain data from other tests in same class (shared factory)
    }

    [Fact]
    public async Task PostProduct_CreatesProduct_ReturnsCreated()
    {
        // Arrange
        var nuevoProduct = new CreateProductRequest
        {
            Code = "BAT-001",
            Description = "Bateria 12V 75Ah Auto",
            SalePrice = 150000.00m,
            CategoryId = 1, // Baterias Auto (seed data)
            UnitOfMeasureId = 1 // Unidades (seed data)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/productos", nuevoProduct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var productoCreado = await response.Content.ReadFromJsonAsync<ProductResponse>();
        productoCreado.Should().NotBeNull();
        productoCreado!.Id.Should().BeGreaterThan(0);
        productoCreado.Code.Should().Be("BAT-001");
        productoCreado.Description.Should().Be("Bateria 12V 75Ah Auto");
        productoCreado.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetProductById_ReturnsProduct_WhenExists()
    {
        // Arrange - Create a producto first
        var nuevoProduct = new CreateProductRequest
        {
            Code = "BAT-002",
            Description = "Bateria 12V 45Ah Moto",
            SalePrice = 50000.00m,
            CategoryId = 2, // Baterias Moto
            UnitOfMeasureId = 1
        };
        var createResponse = await _client.PostAsJsonAsync("/api/productos", nuevoProduct);
        var productoCreado = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        // Act
        var response = await _client.GetAsync($"/api/productos/{productoCreado!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var producto = await response.Content.ReadFromJsonAsync<ProductResponse>();
        producto.Should().NotBeNull();
        producto!.Code.Should().Be("BAT-002");
    }

    [Fact]
    public async Task GetProductById_ReturnsNotFound_WhenDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/productos/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BuscarProducts_ReturnsMatchingProducts_WhenSearchByDescription()
    {
        // Arrange - Create productos with unique prefix
        var prefix = Guid.NewGuid().ToString()[..8];
        var producto1 = new CreateProductRequest { Code = $"{prefix}-100", Description = $"{prefix} Bateria 60Ah", SalePrice = 100000, CategoryId = 1, UnitOfMeasureId = 1 };
        var producto2 = new CreateProductRequest { Code = $"{prefix}-101", Description = $"{prefix} Bateria 75Ah", SalePrice = 120000, CategoryId = 1, UnitOfMeasureId = 1 };
        var producto3 = new CreateProductRequest { Code = $"{prefix}-ACC", Description = $"{prefix} Cable arranque", SalePrice = 15000, CategoryId = 4, UnitOfMeasureId = 1 };

        await _client.PostAsJsonAsync("/api/productos", producto1);
        await _client.PostAsJsonAsync("/api/productos", producto2);
        await _client.PostAsJsonAsync("/api/productos", producto3);

        // Act - Search by unique prefix + Bateria
        var response = await _client.GetAsync($"/api/productos/buscar?Description={prefix}%20Bateria");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var productos = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        productos.Should().NotBeNull();
        productos.Should().HaveCount(2);
        productos.Should().OnlyContain(p => p.Description.Contains($"{prefix} Bateria"));
    }

    [Fact]
    public async Task BuscarProducts_ReturnsMatchingProducts_WhenSearchByCodigo()
    {
        // Arrange
        var uniqueCode = $"UNIQUE-{Guid.NewGuid().ToString()[..6]}";
        var producto = new CreateProductRequest { Code = uniqueCode, Description = "Product Unico", SalePrice = 10000, CategoryId = 1, UnitOfMeasureId = 1 };
        await _client.PostAsJsonAsync("/api/productos", producto);

        // Act
        var response = await _client.GetAsync($"/api/productos/buscar?Description={uniqueCode}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var productos = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        productos.Should().NotBeNull();
        productos.Should().ContainSingle(p => p.Code == uniqueCode);
    }

    [Fact]
    public async Task BuscarProducts_DoesNotReturn_WhenSearchBySupplierCodeOnly()
    {
        // Arrange
        var supplierCode = $"SUP-{Guid.NewGuid().ToString()[..6]}";
        var producto = new CreateProductRequest
        {
            Code = "BAT-700",
            Description = "Bateria Search Supplier",
            SupplierCode = supplierCode,
            SalePrice = 10000,
            CategoryId = 1,
            UnitOfMeasureId = 1
        };
        await _client.PostAsJsonAsync("/api/productos", producto);

        // Act
        var response = await _client.GetAsync($"/api/productos/buscar?Description={supplierCode}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var productos = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        productos.Should().NotBeNull();
        productos.Should().BeEmpty();
    }

    [Fact]
    public async Task BuscarProducts_IncludesPriceFields_InResponse()
    {
        // Arrange
        var uniqueCode = $"PRICE-{Guid.NewGuid().ToString()[..6]}";
        var producto = new CreateProductRequest
        {
            Code = uniqueCode,
            Description = "Producto Precio",
            SalePrice = 10000,
            CategoryId = 1,
            UnitOfMeasureId = 1
        };
        await _client.PostAsJsonAsync("/api/productos", producto);

        // Act
        var response = await _client.GetAsync($"/api/productos/buscar?Description={uniqueCode}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        document.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        document.RootElement.GetArrayLength().Should().BeGreaterThan(0);

        var first = document.RootElement[0];
        first.TryGetProperty("quotePrice", out _).Should().BeTrue();
        first.TryGetProperty("invoicePrice", out _).Should().BeTrue();
    }

    [Fact]
    public async Task BuscarProducts_ReturnsBadRequest_WhenNoSearchTerm()
    {
        // Act
        var response = await _client.GetAsync("/api/productos/buscar");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PutProduct_UpdatesProduct_ReturnsOk()
    {
        // Arrange - Create a producto first
        var nuevoProduct = new CreateProductRequest
        {
            Code = "BAT-UPD",
            Description = "Bateria Original",
            SalePrice = 100000.00m,
            CategoryId = 1,
            UnitOfMeasureId = 1
        };
        var createResponse = await _client.PostAsJsonAsync("/api/productos", nuevoProduct);
        var productoCreado = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        // Modify with UpdateProductRequest
        var updateRequest = new UpdateProductRequest
        {
            Code = productoCreado!.Code,
            Description = "Bateria Actualizada",
            SalePrice = 120000.00m,
            CategoryId = productoCreado.CategoryId,
            UnitOfMeasureId = productoCreado.UnitOfMeasureId
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/productos/{productoCreado.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var productoActualizado = await response.Content.ReadFromJsonAsync<ProductResponse>();
        productoActualizado!.Description.Should().Be("Bateria Actualizada");
        productoActualizado.SalePrice.Should().Be(120000.00m);
    }

    [Fact]
    public async Task PutProduct_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var updateRequest = new UpdateProductRequest { Code = "GHOST", Description = "Ghost Product" };

        // Act
        var response = await _client.PutAsJsonAsync("/api/productos/99999", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_SoftDeletes_ReturnsNoContent()
    {
        // Arrange - Create a producto
        var nuevoProduct = new CreateProductRequest
        {
            Code = "BAT-DEL",
            Description = "Bateria To Delete",
            SalePrice = 50000.00m,
            CategoryId = 1,
            UnitOfMeasureId = 1
        };
        var createResponse = await _client.PostAsJsonAsync("/api/productos", nuevoProduct);
        var productoCreado = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        // Act
        var response = await _client.DeleteAsync($"/api/productos/{productoCreado!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify soft delete - producto should not appear in list (filtered by IsActive=true)
        var listResponse = await _client.GetAsync("/api/productos");
        var productos = await listResponse.Content.ReadFromJsonAsync<List<ProductResponse>>();
        productos.Should().NotContain(p => p.Id == productoCreado.Id);
    }

    [Fact]
    public async Task DeleteProduct_ReturnsNotFound_WhenDoesNotExist()
    {
        // Act
        var response = await _client.DeleteAsync("/api/productos/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
