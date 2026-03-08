using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SPC.API.Contracts.NotasDebito;
using SPC.Tests.Infrastructure;

namespace SPC.Tests.Integration;

/// <summary>
/// Integration tests for Notas de Debito (Debit Notes) endpoints.
/// </summary>
public class NotasDebitoEndpointsTests : IClassFixture<SPCWebApplicationFactory>
{
    private readonly HttpClient _client;

    public NotasDebitoEndpointsTests(SPCWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetNotasDebito_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/notas-debito");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetNotaDebitoById_ReturnsNotFound_WhenDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/notas-debito/99999");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateNotaDebito_ReturnsCreated_WithValidData()
    {
        // Arrange
        var request = new CreateNotaDebitoRequest
        {
            BranchId = 1,
            VoucherType = "B",
            CustomerId = 1,
            DiscountPercent = 0,
            Details = new List<CreateNotaDebitoDetalleRequest>
            {
                new CreateNotaDebitoDetalleRequest
                {
                    ProductId = 1,
                    Quantity = 1,
                    DiscountPercent = 0
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/notas-debito", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var note = await response.Content.ReadFromJsonAsync<NotaDebitoCompletaResponse>();
        note.Should().NotBeNull();
        note!.CustomerId.Should().Be(1);
        note.VoucherType.Should().Be("B");
    }

    [Fact]
    public async Task CreateNotaDebito_CalculatesVATCorrectly()
    {
        // Arrange
        var request = new CreateNotaDebitoRequest
        {
            BranchId = 1,
            VoucherType = "B",
            CustomerId = 1,
            DiscountPercent = 0,
            Details = new List<CreateNotaDebitoDetalleRequest>
            {
                new CreateNotaDebitoDetalleRequest
                {
                    ProductId = 1,
                    Quantity = 1,
                    DiscountPercent = 0
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/notas-debito", request);
        var note = await response.Content.ReadFromJsonAsync<NotaDebitoCompletaResponse>();

        // Assert
        note.Should().NotBeNull();
        note!.Subtotal.Should().Be(1000m);
        note.VATPercent.Should().Be(21m);
        note.VATAmount.Should().Be(210m);
        note.Total.Should().Be(1210m);
    }

    [Fact]
    public async Task AnularNotaDebito_ReturnsOk_WhenExists()
    {
        // Arrange - First create a debit note
        var createRequest = new CreateNotaDebitoRequest
        {
            BranchId = 1,
            VoucherType = "B",
            CustomerId = 1,
            Details = new List<CreateNotaDebitoDetalleRequest>
            {
                new CreateNotaDebitoDetalleRequest { ProductId = 1, Quantity = 1 }
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/notas-debito", createRequest);
        var note = await createResponse.Content.ReadFromJsonAsync<NotaDebitoCompletaResponse>();

        // Act - Void the debit note
        var anularRequest = new AnularNotaDebitoRequest { Reason = "Test void" };
        var response = await _client.PostAsJsonAsync($"/api/notas-debito/{note!.Id}/anular", anularRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify it's voided
        var getResponse = await _client.GetAsync($"/api/notas-debito/{note.Id}");
        var voidedNote = await getResponse.Content.ReadFromJsonAsync<NotaDebitoCompletaResponse>();
        voidedNote!.IsVoided.Should().BeTrue();
    }
}
