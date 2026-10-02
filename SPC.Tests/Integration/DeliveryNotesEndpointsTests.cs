using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SPC.API.Contracts.DeliveryNotes;
using SPC.Tests.Infrastructure;

namespace SPC.Tests.Integration;

public sealed class DeliveryNotesEndpointsTests : IClassFixture<SPCWebApplicationFactory>
{
    private readonly HttpClient _client;
    public DeliveryNotesEndpointsTests(SPCWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task PreviewIsReadOnlyAndCreateReturnsSavedNote()
    {
        var preview = await _client.GetFromJsonAsync<DeliveryNoteNextNumberResponse>("/api/delivery-notes/next-number?branchId=1");
        preview.Should().NotBeNull();
        preview!.IsAdvisory.Should().BeTrue();
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/delivery-notes")
        {
            Content = JsonContent.Create(new CreateDeliveryNoteRequest
            {
                BranchId = 1, CustomerId = 1, AdjustStock = false,
                Details = [new CreateDeliveryNoteDetailRequest { ProductId = 1, Quantity = 1 }]
            })
        };
        message.Headers.Add("Idempotency-Key", "delivery-note-integration-1");
        var response = await _client.SendAsync(message);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var note = await response.Content.ReadFromJsonAsync<DeliveryNoteResponse>();
        note.Should().NotBeNull();
        note!.DeliveryNoteNumber.Should().Be(preview.NextNumber);
    }

    [Fact]
    public async Task CreateDuplicateManualNumberReturnsConflict()
    {
        async Task<HttpResponseMessage> Create(string key)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/delivery-notes")
            {
                Content = JsonContent.Create(new CreateDeliveryNoteRequest
                {
                    BranchId = 1, CustomerId = 1, AdjustStock = false, RequestedDeliveryNoteNumber = 450,
                    Details = [new CreateDeliveryNoteDetailRequest { ProductId = 1, Quantity = 1 }]
                })
            };
            request.Headers.Add("Idempotency-Key", key);
            return await _client.SendAsync(request);
        }
        (await Create("manual-duplicate-a")).StatusCode.Should().Be(HttpStatusCode.Created);
        (await Create("manual-duplicate-b")).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
