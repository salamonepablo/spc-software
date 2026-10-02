extern alias SPCWEB;

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using ApiService = SPCWEB::SPC.Web.Services.ApiService;

namespace SPC.Tests.Unit;

public sealed class DeliveryNoteApiServiceTests
{
    [Fact]
    public void GetDeliveryNotePdfUrlReturnsAbsoluteUrlsOnConfiguredApiOrigin()
    {
        using var http = new HttpClient { BaseAddress = new Uri("https://api.example.test/service/") };
        var service = new ApiService(http, NullLogger<ApiService>.Instance);

        service.GetDeliveryNotePdfUrl(42).Should().Be("https://api.example.test/api/delivery-notes/42/pdf");
        service.GetDeliveryNotePdfUrl(42, download: true).Should().Be("https://api.example.test/api/delivery-notes/42/pdf/download");
    }
}
