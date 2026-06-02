using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SPC.API.Contracts.DebitNotes;
using SPC.Tests.Infrastructure;

namespace SPC.Tests.Integration;

/// <summary>
/// Integration tests for Notas de Debito (Debit Notes) endpoints.
/// </summary>
public class DebitNotesEndpointsTests : IClassFixture<SPCWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DebitNotesEndpointsTests(SPCWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDebitNotes_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/notas-debito");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDebitNoteById_ReturnsNotFound_WhenDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/notas-debito/99999");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDebitNoteByNumber_ReturnsOk_WhenExists()
    {
        var createRequest = new CreateDebitNoteRequest
        {
            BranchId = 1,
            VoucherType = "B",
            CustomerId = 1,
            Details = new List<CreateDebitNoteDetalleRequest>
            {
                new CreateDebitNoteDetalleRequest { ProductId = 1, Quantity = 1 }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/notas-debito", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<DebitNoteCompletaResponse>();

        var response = await _client.GetAsync($"/api/notas-debito/number/{created!.DebitNoteNumber}?customerId={created.CustomerId}&voucherType={created.VoucherType}&pointOfSale={created.PointOfSale}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var note = await response.Content.ReadFromJsonAsync<DebitNoteCompletaResponse>();
        note.Should().NotBeNull();
        note!.DebitNoteNumber.Should().Be(created.DebitNoteNumber);
        note.CustomerId.Should().Be(created.CustomerId);
        note.VoucherType.Should().Be(created.VoucherType);
        note.PointOfSale.Should().Be(created.PointOfSale);
    }

    [Fact]
    public async Task SearchDebitNotes_WithOfficialFormattedTerm_ReturnsExactDocumentOnly()
    {
        var createRequest = new CreateDebitNoteRequest
        {
            BranchId = 1,
            VoucherType = "A",
            CustomerId = 1,
            Details = new List<CreateDebitNoteDetalleRequest>
            {
                new CreateDebitNoteDetalleRequest { ProductId = 1, Quantity = 1 }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/notas-debito", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<DebitNoteCompletaResponse>();
        var officialNumber = $"ND {created!.VoucherType} {created.PointOfSale:D4}-{created.DebitNoteNumber:D8}";

        var response = await _client.GetAsync($"/api/notas-debito/buscar?termino={Uri.EscapeDataString(officialNumber)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notes = await response.Content.ReadFromJsonAsync<List<DebitNoteResponse>>();
        notes.Should().NotBeNull();
        notes.Should().ContainSingle(n => n.Id == created.Id);
    }

    [Fact]
    public async Task CreateDebitNote_ReturnsCreated_WithValidData()
    {
        // Arrange
        var request = new CreateDebitNoteRequest
        {
            BranchId = 1,
            VoucherType = "B",
            CustomerId = 1,
            DiscountPercent = 0,
            Details = new List<CreateDebitNoteDetalleRequest>
            {
                new CreateDebitNoteDetalleRequest
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
        
        var note = await response.Content.ReadFromJsonAsync<DebitNoteCompletaResponse>();
        note.Should().NotBeNull();
        note!.CustomerId.Should().Be(1);
        note.VoucherType.Should().Be("B");
    }

    [Fact]
    public async Task CreateDebitNote_CalculatesVATCorrectly()
    {
        // Arrange
        var request = new CreateDebitNoteRequest
        {
            BranchId = 1,
            VoucherType = "B",
            CustomerId = 1,
            DiscountPercent = 0,
            Details = new List<CreateDebitNoteDetalleRequest>
            {
                new CreateDebitNoteDetalleRequest
                {
                    ProductId = 1,
                    Quantity = 1,
                    DiscountPercent = 0
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/notas-debito", request);
        var note = await response.Content.ReadFromJsonAsync<DebitNoteCompletaResponse>();

        // Assert
        note.Should().NotBeNull();
        note!.Subtotal.Should().Be(1000m);
        note.VATPercent.Should().Be(21m);
        note.VATAmount.Should().Be(210m);
        note.Total.Should().Be(1210m);
    }

    [Fact]
    public async Task AnularDebitNote_ReturnsOk_WhenExists()
    {
        // Arrange - First create a debit note
        var createRequest = new CreateDebitNoteRequest
        {
            BranchId = 1,
            VoucherType = "B",
            CustomerId = 1,
            Details = new List<CreateDebitNoteDetalleRequest>
            {
                new CreateDebitNoteDetalleRequest { ProductId = 1, Quantity = 1 }
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/notas-debito", createRequest);
        var note = await createResponse.Content.ReadFromJsonAsync<DebitNoteCompletaResponse>();

        // Act - Void the debit note
        var anularRequest = new AnularDebitNoteRequest { Reason = "Test void" };
        var response = await _client.PostAsJsonAsync($"/api/notas-debito/{note!.Id}/anular", anularRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify it's voided
        var getResponse = await _client.GetAsync($"/api/notas-debito/{note.Id}");
        var voidedNote = await getResponse.Content.ReadFromJsonAsync<DebitNoteCompletaResponse>();
        voidedNote!.IsVoided.Should().BeTrue();
    }
}
