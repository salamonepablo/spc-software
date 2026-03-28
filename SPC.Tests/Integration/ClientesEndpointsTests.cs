using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SPC.API.Contracts.Customers;
using SPC.Tests.Infrastructure;

namespace SPC.Tests.Integration;

/// <summary>
/// Integration tests for /api/clientes endpoints.
/// Tests the full request/response cycle with InMemory database.
/// </summary>
public class CustomersEndpointsTests : IClassFixture<SPCWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SPCWebApplicationFactory _factory;

    public CustomersEndpointsTests(SPCWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCustomers_ReturnsOkWithList()
    {
        // Act
        var response = await _client.GetAsync("/api/clientes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var clientes = await response.Content.ReadFromJsonAsync<List<CustomerResponse>>();
        clientes.Should().NotBeNull();
        // List may contain data from other tests in same class (shared factory)
    }

    [Fact]
    public async Task PostCustomer_CreatesCustomer_ReturnsCreated()
    {
        // Arrange
        var nuevoCustomer = new CreateCustomerRequest
        {
            CompanyName = "Test Company SRL",
            CUIT = "30-12345678-9",
            Address = "Test Street 123",
            City = "Buenos Aires",
            Province = "Buenos Aires",
            TaxConditionId = 1 // Responsable Inscripto (seed data)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/clientes", nuevoCustomer);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var clienteCreado = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        clienteCreado.Should().NotBeNull();
        clienteCreado!.Id.Should().BeGreaterThan(0);
        clienteCreado.CompanyName.Should().Be("Test Company SRL");
        clienteCreado.IsActive.Should().BeTrue();
        clienteCreado.CreatedDate.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetCustomerById_ReturnsCustomer_WhenExists()
    {
        // Arrange - Create a cliente first
        var nuevoCustomer = new CreateCustomerRequest
        {
            CompanyName = "Get By Id Test SRL",
            CUIT = "30-11111111-1",
            TaxConditionId = 1
        };
        var createResponse = await _client.PostAsJsonAsync("/api/clientes", nuevoCustomer);
        var clienteCreado = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Act
        var response = await _client.GetAsync($"/api/clientes/{clienteCreado!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cliente = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        cliente.Should().NotBeNull();
        cliente!.CompanyName.Should().Be("Get By Id Test SRL");
    }

    [Fact]
    public async Task GetCustomerById_ReturnsNotFound_WhenDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/clientes/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BuscarCustomers_ReturnsMatchingCustomers_WhenSearchByName()
    {
        // Arrange - Create clientes with unique prefix
        var prefix = Guid.NewGuid().ToString()[..8];
        var cliente1 = new CreateCustomerRequest { CompanyName = $"{prefix} Baterias Norte SRL", CUIT = "30-22222222-2", TaxConditionId = 1 };
        var cliente2 = new CreateCustomerRequest { CompanyName = $"{prefix} Baterias Sur SA", CUIT = "30-33333333-3", TaxConditionId = 2 };
        var cliente3 = new CreateCustomerRequest { CompanyName = $"{prefix} Accesorios Auto", CUIT = "30-44444444-4", TaxConditionId = 3 };

        await _client.PostAsJsonAsync("/api/clientes", cliente1);
        await _client.PostAsJsonAsync("/api/clientes", cliente2);
        await _client.PostAsJsonAsync("/api/clientes", cliente3);

        // Act - Search by unique prefix + Baterias
        var response = await _client.GetAsync($"/api/clientes/buscar?Name={prefix}%20Baterias");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var clientes = await response.Content.ReadFromJsonAsync<List<CustomerResponse>>();
        clientes.Should().NotBeNull();
        clientes.Should().HaveCount(2);
        clientes.Should().OnlyContain(c => c.CompanyName.Contains($"{prefix} Baterias"));
    }

    [Fact]
    public async Task BuscarCustomers_ReturnsMatchingCustomers_WhenSearchByInternalId()
    {
        // Arrange
        var nuevoCustomer = new CreateCustomerRequest
        {
            CompanyName = "Internal Id Search SRL",
            CUIT = "30-77777777-7",
            TaxConditionId = 1
        };

        var createResponse = await _client.PostAsJsonAsync("/api/clientes", nuevoCustomer);
        var clienteCreado = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Act
        var response = await _client.GetAsync($"/api/clientes/buscar?Name={clienteCreado!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var clientes = await response.Content.ReadFromJsonAsync<List<CustomerResponse>>();
        clientes.Should().NotBeNull();
        clientes.Should().ContainSingle(c => c.Id == clienteCreado.Id);
        clientes!.First().CompanyName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task BuscarCustomers_ReturnsBadRequest_WhenNoSearchTerm()
    {
        // Act
        var response = await _client.GetAsync("/api/clientes/buscar");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PutCustomer_UpdatesCustomer_ReturnsOk()
    {
        // Arrange - Create a cliente
        var nuevoCustomer = new CreateCustomerRequest
        {
            CompanyName = "Original Name SRL",
            CUIT = "30-55555555-5",
            TaxConditionId = 1
        };
        var createResponse = await _client.PostAsJsonAsync("/api/clientes", nuevoCustomer);
        var clienteCreado = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Modify with UpdateCustomerRequest
        var updateRequest = new UpdateCustomerRequest
        {
            CompanyName = "Updated Name SA",
            CUIT = clienteCreado!.CUIT,
            Phone = "11-4444-5555",
            TaxConditionId = clienteCreado.TaxConditionId
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/clientes/{clienteCreado.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var clienteActualizado = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        clienteActualizado!.CompanyName.Should().Be("Updated Name SA");
        clienteActualizado.Phone.Should().Be("11-4444-5555");
    }

    [Fact]
    public async Task PutCustomer_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var updateRequest = new UpdateCustomerRequest { CompanyName = "Ghost SRL" };

        // Act
        var response = await _client.PutAsJsonAsync("/api/clientes/99999", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCustomer_SoftDeletes_ReturnsNoContent()
    {
        // Arrange - Create a cliente
        var nuevoCustomer = new CreateCustomerRequest
        {
            CompanyName = "To Delete SRL",
            CUIT = "30-66666666-6",
            TaxConditionId = 1
        };
        var createResponse = await _client.PostAsJsonAsync("/api/clientes", nuevoCustomer);
        var clienteCreado = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Act
        var response = await _client.DeleteAsync($"/api/clientes/{clienteCreado!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify soft delete - cliente should not appear in list (filtered by IsActive=true)
        var listResponse = await _client.GetAsync("/api/clientes");
        var clientes = await listResponse.Content.ReadFromJsonAsync<List<CustomerResponse>>();
        clientes.Should().NotContain(c => c.Id == clienteCreado.Id);
    }

    [Fact]
    public async Task DeleteCustomer_ReturnsNotFound_WhenDoesNotExist()
    {
        // Act
        var response = await _client.DeleteAsync("/api/clientes/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
