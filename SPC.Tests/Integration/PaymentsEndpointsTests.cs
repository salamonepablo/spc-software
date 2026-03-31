using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SPC.API.Contracts.Payments;
using SPC.API.Data;
using SPC.Shared.Models;
using SPC.Tests.Infrastructure;

namespace SPC.Tests.Integration;

public class PaymentsEndpointsTests : IClassFixture<SPCWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SPCWebApplicationFactory _factory;

    public PaymentsEndpointsTests(SPCWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPaymentByNumber_ReturnsOk_WhenPaymentExists()
    {
        await SeedPaymentAsync(new Payment
        {
            BranchId = 1,
            PaymentNumber = 7001,
            PaymentDate = new DateTime(2026, 3, 28),
            CustomerId = 3,
            TotalAmount = 250m,
            AppliesTo = AccountLineType.Billing,
            Details = new List<PaymentDetail>
            {
                new PaymentDetail
                {
                    LineNumber = 1,
                    PaymentMethodId = 1,
                    Amount = 250m,
                    Notes = "Efectivo"
                }
            }
        });

        var response = await _client.GetAsync("/api/payments/7001?customerId=3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payment = await response.Content.ReadFromJsonAsync<PaymentDetailResponse>();
        payment.Should().NotBeNull();
        payment!.PaymentNumber.Should().Be(7001);
        payment.CustomerId.Should().Be(3);
        payment.Details.Should().ContainSingle();
        payment.Details[0].PaymentMethodCode.Should().Be("EF");
    }

    [Fact]
    public async Task GetPaymentByNumber_ReturnsNotFound_WhenPaymentDoesNotExist()
    {
        var response = await _client.GetAsync("/api/payments/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task SeedPaymentAsync(Payment payment)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SPCDbContext>();
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
    }
}
