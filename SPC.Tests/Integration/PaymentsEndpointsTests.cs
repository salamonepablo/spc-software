using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

    [Fact]
    public async Task CreatePayment_PersistsDetailsAndExactlyOneBillingMovement_AndReplaysIdempotentRequest()
    {
        var request = new CreatePaymentRequest
        {
            CustomerId = 1,
            BranchId = 1,
            PaymentDate = new DateTime(2026, 4, 1),
            AppliesTo = "Billing",
            Notes = "Pago parcial",
            Details =
            [
                new CreatePaymentDetailRequest { PaymentMethodId = 1, Amount = 100m, Notes = "Caja" },
                new CreatePaymentDetailRequest { PaymentMethodId = 3, Amount = 50m, Notes = "Banco" }
            ]
        };

        using var first = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(request)
        };
        first.Headers.Add("Idempotency-Key", "payment-001");
        var firstResponse = await _client.SendAsync(first);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await firstResponse.Content.ReadFromJsonAsync<PaymentDetailResponse>();
        created.Should().NotBeNull();
        created!.PaymentNumber.Should().BeGreaterThan(0);
        created.TotalAmount.Should().Be(150m);
        created.Details.Should().HaveCount(2);

        using var replay = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(request)
        };
        replay.Headers.Add("Idempotency-Key", "payment-001");
        var replayResponse = await _client.SendAsync(replay);

        replayResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var replayed = await replayResponse.Content.ReadFromJsonAsync<PaymentDetailResponse>();
        replayed!.Id.Should().Be(created.Id);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SPCDbContext>();
        db.Payments.Should().Contain(p => p.Id == created.Id);
        db.CurrentAccountMovements.Should().ContainSingle(m =>
            m.CustomerId == 1 &&
            m.DocumentNumber == created.PaymentNumber &&
            m.DocumentType == DocumentType.Payment &&
            m.MovementDate == request.PaymentDate.Date &&
            m.BillingAmount == -150m &&
            m.BudgetAmount == 0m);
    }

    [Fact]
    public async Task CreatePayment_ReturnsConflict_WhenIdempotencyKeyIsReusedWithDifferentRequest()
    {
        var request = new CreatePaymentRequest
        {
            CustomerId = 1,
            BranchId = 1,
            PaymentDate = new DateTime(2026, 4, 1),
            AppliesTo = "Billing",
            Details = [new CreatePaymentDetailRequest { PaymentMethodId = 1, Amount = 100m }]
        };

        using var first = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(request)
        };
        first.Headers.Add("Idempotency-Key", "payment-conflict");
        (await _client.SendAsync(first)).StatusCode.Should().Be(HttpStatusCode.Created);

        request.Details[0].Amount = 101m;
        using var conflicting = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(request)
        };
        conflicting.Headers.Add("Idempotency-Key", "payment-conflict");

        (await _client.SendAsync(conflicting)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetPaymentByNumber_ReturnsNotFound_WhenNumberExistsInMultipleBranchesWithoutContext()
    {
        await SeedPaymentAsync(new Payment { BranchId = 1, PaymentNumber = 99, PaymentDate = DateTime.Today, CustomerId = 1, TotalAmount = 1, AppliesTo = AccountLineType.Billing });
        await SeedPaymentAsync(new Payment { BranchId = 2, PaymentNumber = 99, PaymentDate = DateTime.Today, CustomerId = 2, TotalAmount = 1, AppliesTo = AccountLineType.Billing });

        (await _client.GetAsync("/api/payments/99")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.GetAsync("/api/payments/99?branchId=2")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreatePayment_RecordsExactlyOneNegativeL2Movement_WhenDualLineCapabilityIsEnabled()
    {
        using var enabledFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Licensing:Features:DualLineCurrentAccount"] = "true"
                })));
        using var client = enabledFactory.CreateClient();
        decimal billingBalanceBefore;
        decimal budgetBalanceBefore;
        using (var scope = enabledFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SPCDbContext>();
            var accountBefore = await db.CurrentAccounts.SingleOrDefaultAsync(item => item.CustomerId == 1);
            billingBalanceBefore = accountBefore?.BillingBalance ?? 0m;
            budgetBalanceBefore = accountBefore?.BudgetBalance ?? 0m;
        }
        var request = new CreatePaymentRequest
        {
            CustomerId = 1,
            BranchId = 1,
            PaymentDate = new DateTime(2026, 4, 1),
            AppliesTo = "Budget",
            Details = [new CreatePaymentDetailRequest { PaymentMethodId = 1, Amount = 125m }]
        };
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("Idempotency-Key", "budget-enabled");

        var response = await client.SendAsync(message);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<PaymentDetailResponse>();
        created.Should().NotBeNull();
        using var verificationScope = enabledFactory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<SPCDbContext>();
        verificationDb.CurrentAccountMovements.Should().ContainSingle(m =>
            m.CustomerId == 1 &&
            m.DocumentType == DocumentType.Payment &&
            m.DocumentNumber == created!.PaymentNumber &&
            m.MovementDate == request.PaymentDate.Date &&
            m.BillingAmount == 0m &&
            m.BudgetAmount == -125m);
        var account = await verificationDb.CurrentAccounts.SingleAsync(item => item.CustomerId == 1);
        account.BillingBalance.Should().Be(billingBalanceBefore);
        account.BudgetBalance.Should().Be(budgetBalanceBefore - 125m);
    }

    [Fact]
    public async Task CreatePayment_AllocatesNumbersIndependentlyPerBranch()
    {
        async Task<PaymentDetailResponse> CreateAsync(int branchId, string key)
        {
            var request = new CreatePaymentRequest
            {
                CustomerId = 1,
                BranchId = branchId,
                PaymentDate = new DateTime(2026, 4, 1),
                AppliesTo = "Billing",
                Details = [new CreatePaymentDetailRequest { PaymentMethodId = 1, Amount = 10m }]
            };
            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
            {
                Content = JsonContent.Create(request)
            };
            message.Headers.Add("Idempotency-Key", key);
            var response = await _client.SendAsync(message);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            return (await response.Content.ReadFromJsonAsync<PaymentDetailResponse>())!;
        }

        long nextBranchOne;
        long nextBranchTwo;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SPCDbContext>();
            nextBranchOne = (await db.BranchPaymentSequences.SingleOrDefaultAsync(item => item.BranchId == 1))?.NextPaymentNumber ?? 1;
            nextBranchTwo = (await db.BranchPaymentSequences.SingleOrDefaultAsync(item => item.BranchId == 2))?.NextPaymentNumber ?? 1;
        }

        var firstBranchOne = await CreateAsync(1, "branch-one-first");
        var firstBranchTwo = await CreateAsync(2, "branch-two-first");
        var secondBranchOne = await CreateAsync(1, "branch-one-second");

        firstBranchOne.PaymentNumber.Should().Be(nextBranchOne);
        firstBranchTwo.PaymentNumber.Should().Be(nextBranchTwo);
        secondBranchOne.PaymentNumber.Should().Be(nextBranchOne + 1);
    }

    [Fact]
    public async Task CreatePayment_AllocatesTheCurrentNumberFromAnExistingInMemorySequence()
    {
        using var isolatedFactory = new SPCWebApplicationFactory();
        using var client = isolatedFactory.CreateClient();
        using (var scope = isolatedFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SPCDbContext>();
            db.BranchPaymentSequences.Add(new BranchPaymentSequence { BranchId = 2, NextPaymentNumber = 42 });
            await db.SaveChangesAsync();
        }

        var request = new CreatePaymentRequest
        {
            CustomerId = 1,
            BranchId = 2,
            PaymentDate = new DateTime(2026, 4, 1),
            AppliesTo = "Billing",
            Details = [new CreatePaymentDetailRequest { PaymentMethodId = 1, Amount = 10m }]
        };
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("Idempotency-Key", "existing-sequence");

        var response = await client.SendAsync(message);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<PaymentDetailResponse>();
        created!.PaymentNumber.Should().Be(42);
        using var verificationScope = isolatedFactory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<SPCDbContext>();
        (await verificationDb.BranchPaymentSequences.SingleAsync(item => item.BranchId == 2)).NextPaymentNumber.Should().Be(43);
    }

    [Fact]
    public async Task CreatePayment_RejectsBudgetWhenDualLineCapabilityIsDisabled()
    {
        var request = new CreatePaymentRequest
        {
            CustomerId = 1, BranchId = 1, PaymentDate = DateTime.Today, AppliesTo = "Budget",
            Details = [new CreatePaymentDetailRequest { PaymentMethodId = 1, Amount = 10m }]
        };
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/payments") { Content = JsonContent.Create(request) };
        message.Headers.Add("Idempotency-Key", "budget-disabled");

        (await _client.SendAsync(message)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<SPCDbContext>().Payments.Should().NotContain(payment => payment.IdempotencyKey == "budget-disabled");
    }

    [Fact]
        public async Task VoidPayment_RestoresBillingBalance_AndCreatesOneBillingReversal()
        {
            var created = await CreatePaymentAsync("void-billing", "Billing", 80m);
            var response = await _client.PostAsJsonAsync($"/api/payments/{created.Id}/anular", new VoidPaymentRequest { Reason = "Error" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadFromJsonAsync<PaymentDetailResponse>())!.IsVoided.Should().BeTrue();
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SPCDbContext>();
            db.CurrentAccountMovements.Should().ContainSingle(m => m.DocumentType == DocumentType.PaymentVoidBilling && m.DocumentNumber == created.PaymentNumber && m.BillingAmount == 80m && m.BudgetAmount == 0m);
            (await db.CurrentAccounts.SingleAsync(account => account.CustomerId == created.CustomerId)).BillingBalance.Should().Be(0m);
        }

        [Fact]
        public async Task VoidPayment_RestoresBudgetBalance_AndCreatesOneBudgetReversal()
        {
            using var enabledFactory = _factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Licensing:Features:DualLineCurrentAccount"] = "true" })));
            using var client = enabledFactory.CreateClient();
            var request = new CreatePaymentRequest { CustomerId = 1, BranchId = 1, PaymentDate = new DateTime(2026, 4, 1), AppliesTo = "Budget", Details = [new CreatePaymentDetailRequest { PaymentMethodId = 1, Amount = 90m }] };
            using var create = new HttpRequestMessage(HttpMethod.Post, "/api/payments") { Content = JsonContent.Create(request) };
            create.Headers.Add("Idempotency-Key", "void-budget");
            var created = (await (await client.SendAsync(create)).Content.ReadFromJsonAsync<PaymentDetailResponse>())!;

            (await client.PostAsJsonAsync($"/api/payments/{created.Id}/anular", new VoidPaymentRequest())).StatusCode.Should().Be(HttpStatusCode.OK);
            using var scope = enabledFactory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SPCDbContext>();
            db.CurrentAccountMovements.Should().ContainSingle(m => m.DocumentType == DocumentType.PaymentVoidBudget && m.DocumentNumber == created.PaymentNumber && m.BillingAmount == 0m && m.BudgetAmount == 90m);
            (await db.CurrentAccounts.SingleAsync(account => account.CustomerId == created.CustomerId)).BudgetBalance.Should().Be(0m);
        }

        [Fact]
        public async Task VoidPayment_IsIdempotent_WhenAlreadyVoided()
        {
            var created = await CreatePaymentAsync("void-idempotent", "Billing", 45m);
            (await _client.PostAsJsonAsync($"/api/payments/{created.Id}/anular", new VoidPaymentRequest())).StatusCode.Should().Be(HttpStatusCode.OK);
            (await _client.PostAsJsonAsync($"/api/pagos/{created.Id}/anular", new VoidPaymentRequest())).StatusCode.Should().Be(HttpStatusCode.OK);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SPCDbContext>();
            db.CurrentAccountMovements.Count(m => m.DocumentType == DocumentType.PaymentVoidBilling && m.DocumentNumber == created.PaymentNumber).Should().Be(1);
        }

        private async Task<PaymentDetailResponse> CreatePaymentAsync(string idempotencyKey, string appliesTo, decimal amount)
        {
            var request = new CreatePaymentRequest { CustomerId = 1, BranchId = 1, PaymentDate = new DateTime(2026, 4, 1), AppliesTo = appliesTo, Details = [new CreatePaymentDetailRequest { PaymentMethodId = 1, Amount = amount }] };
            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/payments") { Content = JsonContent.Create(request) };
            message.Headers.Add("Idempotency-Key", idempotencyKey);
            var response = await _client.SendAsync(message);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            return (await response.Content.ReadFromJsonAsync<PaymentDetailResponse>())!;
        }

        private async Task SeedPaymentAsync(Payment payment)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SPCDbContext>();
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
    }
}
