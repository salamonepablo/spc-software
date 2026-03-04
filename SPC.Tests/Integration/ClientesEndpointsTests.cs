using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SPC.API.Contracts.Clientes;
using SPC.Tests.Infrastructure;

namespace SPC.Tests.Integration;

/// <summary>
/// Integration tests for /api/clientes endpoints.
/// Tests the full request/response cycle with InMemory database.
/// </summary>
public class ClientesEndpointsTests : IClassFixture<SPCWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SPCWebApplicationFactory _factory;

    public ClientesEndpointsTests(SPCWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetClientes_ReturnsOkWithList()
    {
        // Act
        var response = await _client.GetAsync("/api/clientes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var clientes = await response.Content.ReadFromJsonAsync<List<ClienteResponse>>();
        clientes.Should().NotBeNull();
        // List may contain data from other tests in same class (shared factory)
    }

    [Fact]
    public async Task PostCliente_CreatesCliente_ReturnsCreated()
    {
        // Arrange
        var nuevoCliente = new CreateClienteRequest
        {
            RazonSocial = "Test Company SRL",
            CUIT = "30-12345678-9",
            Direccion = "Test Street 123",
            Localidad = "Buenos Aires",
            Provincia = "Buenos Aires",
            CondicionIvaId = 1 // Responsable Inscripto (seed data)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/clientes", nuevoCliente);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var clienteCreado = await response.Content.ReadFromJsonAsync<ClienteResponse>();
        clienteCreado.Should().NotBeNull();
        clienteCreado!.Id.Should().BeGreaterThan(0);
        clienteCreado.RazonSocial.Should().Be("Test Company SRL");
        clienteCreado.Activo.Should().BeTrue();
        clienteCreado.FechaAlta.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetClienteById_ReturnsCliente_WhenExists()
    {
        // Arrange - Create a cliente first
        var nuevoCliente = new CreateClienteRequest
        {
            RazonSocial = "Get By Id Test SRL",
            CUIT = "30-11111111-1",
            CondicionIvaId = 1
        };
        var createResponse = await _client.PostAsJsonAsync("/api/clientes", nuevoCliente);
        var clienteCreado = await createResponse.Content.ReadFromJsonAsync<ClienteResponse>();

        // Act
        var response = await _client.GetAsync($"/api/clientes/{clienteCreado!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cliente = await response.Content.ReadFromJsonAsync<ClienteResponse>();
        cliente.Should().NotBeNull();
        cliente!.RazonSocial.Should().Be("Get By Id Test SRL");
    }

    [Fact]
    public async Task GetClienteById_ReturnsNotFound_WhenDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/clientes/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BuscarClientes_ReturnsMatchingClientes_WhenSearchByName()
    {
        // Arrange - Create clientes with unique prefix
        var prefix = Guid.NewGuid().ToString()[..8];
        var cliente1 = new CreateClienteRequest { RazonSocial = $"{prefix} Baterias Norte SRL", CUIT = "30-22222222-2", CondicionIvaId = 1 };
        var cliente2 = new CreateClienteRequest { RazonSocial = $"{prefix} Baterias Sur SA", CUIT = "30-33333333-3", CondicionIvaId = 2 };
        var cliente3 = new CreateClienteRequest { RazonSocial = $"{prefix} Accesorios Auto", CUIT = "30-44444444-4", CondicionIvaId = 3 };

        await _client.PostAsJsonAsync("/api/clientes", cliente1);
        await _client.PostAsJsonAsync("/api/clientes", cliente2);
        await _client.PostAsJsonAsync("/api/clientes", cliente3);

        // Act - Search by unique prefix + Baterias
        var response = await _client.GetAsync($"/api/clientes/buscar?nombre={prefix}%20Baterias");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var clientes = await response.Content.ReadFromJsonAsync<List<ClienteResponse>>();
        clientes.Should().NotBeNull();
        clientes.Should().HaveCount(2);
        clientes.Should().OnlyContain(c => c.RazonSocial.Contains($"{prefix} Baterias"));
    }

    [Fact]
    public async Task BuscarClientes_ReturnsBadRequest_WhenNoSearchTerm()
    {
        // Act
        var response = await _client.GetAsync("/api/clientes/buscar");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PutCliente_UpdatesCliente_ReturnsOk()
    {
        // Arrange - Create a cliente
        var nuevoCliente = new CreateClienteRequest
        {
            RazonSocial = "Original Name SRL",
            CUIT = "30-55555555-5",
            CondicionIvaId = 1
        };
        var createResponse = await _client.PostAsJsonAsync("/api/clientes", nuevoCliente);
        var clienteCreado = await createResponse.Content.ReadFromJsonAsync<ClienteResponse>();

        // Modify with UpdateClienteRequest
        var updateRequest = new UpdateClienteRequest
        {
            RazonSocial = "Updated Name SA",
            CUIT = clienteCreado!.CUIT,
            Telefono = "11-4444-5555",
            CondicionIvaId = clienteCreado.CondicionIvaId
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/clientes/{clienteCreado.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var clienteActualizado = await response.Content.ReadFromJsonAsync<ClienteResponse>();
        clienteActualizado!.RazonSocial.Should().Be("Updated Name SA");
        clienteActualizado.Telefono.Should().Be("11-4444-5555");
    }

    [Fact]
    public async Task PutCliente_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var updateRequest = new UpdateClienteRequest { RazonSocial = "Ghost SRL" };

        // Act
        var response = await _client.PutAsJsonAsync("/api/clientes/99999", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCliente_SoftDeletes_ReturnsNoContent()
    {
        // Arrange - Create a cliente
        var nuevoCliente = new CreateClienteRequest
        {
            RazonSocial = "To Delete SRL",
            CUIT = "30-66666666-6",
            CondicionIvaId = 1
        };
        var createResponse = await _client.PostAsJsonAsync("/api/clientes", nuevoCliente);
        var clienteCreado = await createResponse.Content.ReadFromJsonAsync<ClienteResponse>();

        // Act
        var response = await _client.DeleteAsync($"/api/clientes/{clienteCreado!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify soft delete - cliente should not appear in list (filtered by Activo=true)
        var listResponse = await _client.GetAsync("/api/clientes");
        var clientes = await listResponse.Content.ReadFromJsonAsync<List<ClienteResponse>>();
        clientes.Should().NotContain(c => c.Id == clienteCreado.Id);
    }

    [Fact]
    public async Task DeleteCliente_ReturnsNotFound_WhenDoesNotExist()
    {
        // Act
        var response = await _client.DeleteAsync("/api/clientes/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
