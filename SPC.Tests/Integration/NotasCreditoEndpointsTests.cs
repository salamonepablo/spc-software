using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SPC.API.Contracts.CreditNotes;
using SPC.Tests.Infrastructure;

namespace SPC.Tests.Integration;

/// <summary>
/// Integration tests for Notas de Credito (Credit Notes) endpoints.
/// </summary>
public class CreditNotesEndpointsTests : IClassFixture<SPCWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CreditNotesEndpointsTests(SPCWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCreditNotes_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/notas-credito");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCreditNoteById_ReturnsNotFound_WhenDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/notas-credito/99999");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateCreditNote_ReturnsCreated_WithValidData()
    {
        // Arrange
        var request = new CreateCreditNoteRequest
        {
            BranchId = 1,
            VoucherType = "B",
            CustomerId = 1,
            DiscountPercent = 0,
            Details = new List<CreateCreditNoteDetalleRequest>
            {
                new CreateCreditNoteDetalleRequest
                {
                    ProductId = 1,
                    Quantity = 1,
                    DiscountPercent = 0
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/notas-credito", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var note = await response.Content.ReadFromJsonAsync<CreditNoteCompletaResponse>();
        note.Should().NotBeNull();
        note!.CustomerId.Should().Be(1);
        note.VoucherType.Should().Be("B");
    }

    [Fact]
    public async Task CreateCreditNote_CalculatesVATCorrectly()
    {
        // Arrange - Product 1: PrecioInvoice = 1000, IVA = 21%
        var request = new CreateCreditNoteRequest
        {
            BranchId = 1,
            VoucherType = "B",
            CustomerId = 1,
            DiscountPercent = 0,
            Details = new List<CreateCreditNoteDetalleRequest>
            {
                new CreateCreditNoteDetalleRequest
                {
                    ProductId = 1,
                    Quantity = 1,
                    DiscountPercent = 0
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/notas-credito", request);
        var note = await response.Content.ReadFromJsonAsync<CreditNoteCompletaResponse>();

        // Assert
        note.Should().NotBeNull();
        note!.Subtotal.Should().Be(1000m);
        note.VATPercent.Should().Be(21m);
        note.VATAmount.Should().Be(210m);
        note.Total.Should().Be(1210m);
    }

    [Fact]
    public async Task CreateCreditNote_AppliesIIBBPerception()
    {
        // Arrange - Apply 3% IIBB
        var request = new CreateCreditNoteRequest
        {
            BranchId = 1,
            VoucherType = "B",
            CustomerId = 1,
            DiscountPercent = 0,
            IIBBPercent = 3,
            Details = new List<CreateCreditNoteDetalleRequest>
            {
                new CreateCreditNoteDetalleRequest
                {
                    ProductId = 1,
                    Quantity = 1,
                    DiscountPercent = 0
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/notas-credito", request);
        var note = await response.Content.ReadFromJsonAsync<CreditNoteCompletaResponse>();

        // Assert
        // Subtotal = 1000, VAT = 210, IIBB base = 1210, IIBB = 36.30
        // Total = 1000 + 210 + 36.30 = 1246.30
        note.Should().NotBeNull();
        note!.IIBBPercent.Should().Be(3m);
        note.IIBBAmount.Should().Be(36.30m);
        note.Total.Should().Be(1246.30m);
    }

    [Fact]
    public async Task CreateCreditNote_StoresVATPercentageForImmutability()
    {
        // Arrange
        var request = new CreateCreditNoteRequest
        {
            BranchId = 1,
            VoucherType = "B",
            CustomerId = 1,
            Details = new List<CreateCreditNoteDetalleRequest>
            {
                new CreateCreditNoteDetalleRequest { ProductId = 1, Quantity = 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/notas-credito", request);
        var note = await response.Content.ReadFromJsonAsync<CreditNoteCompletaResponse>();

        // Assert
        note.Should().NotBeNull();
        note!.VATPercent.Should().Be(21m); // VAT rate stored in document
    }

    [Fact]
    public async Task AnularCreditNote_ReturnsOk_WhenExists()
    {
        // Arrange - First create a credit note
        var createRequest = new CreateCreditNoteRequest
        {
            BranchId = 1,
            VoucherType = "B",
            CustomerId = 1,
            Details = new List<CreateCreditNoteDetalleRequest>
            {
                new CreateCreditNoteDetalleRequest { ProductId = 1, Quantity = 1 }
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/notas-credito", createRequest);
        var note = await createResponse.Content.ReadFromJsonAsync<CreditNoteCompletaResponse>();

        // Act - Void the credit note
        var anularRequest = new AnularCreditNoteRequest { Reason = "Test void" };
        var response = await _client.PostAsJsonAsync($"/api/notas-credito/{note!.Id}/anular", anularRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify it's voided
        var getResponse = await _client.GetAsync($"/api/notas-credito/{note.Id}");
        var voidedNote = await getResponse.Content.ReadFromJsonAsync<CreditNoteCompletaResponse>();
        voidedNote!.IsVoided.Should().BeTrue();
    }
}
