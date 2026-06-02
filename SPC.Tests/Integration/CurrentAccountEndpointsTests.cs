using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SPC.API.Contracts.CurrentAccount;
using SPC.API.Data;
using SPC.Tests.Infrastructure;
using SPC.Shared.Models;
using Microsoft.Extensions.DependencyInjection;

namespace SPC.Tests.Integration;

/// <summary>
/// Integration tests for /api/current-accounts endpoints.
/// Tests the full request/response cycle with InMemory database.
/// </summary>
public class CurrentAccountEndpointsTests : IClassFixture<SPCWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SPCWebApplicationFactory _factory;

    public CurrentAccountEndpointsTests(SPCWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCurrentAccount_ReturnsOk_WhenCustomerExists()
    {
        // Act - Use seeded customer ID 1
        var response = await _client.GetAsync("/api/current-accounts/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var account = await response.Content.ReadFromJsonAsync<CurrentAccountResponse>();
        account.Should().NotBeNull();
        account!.CustomerId.Should().Be(1);
    }

    [Fact]
    public async Task GetCurrentAccount_ReturnsNotFound_WhenCustomerDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/current-accounts/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCurrentAccountMovements_ReturnsOk_WhenCustomerExists()
    {
        // Act - Use seeded customer ID 1
        var response = await _client.GetAsync("/api/current-accounts/1/movements");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CurrentAccountMovementsResponse>();
        result.Should().NotBeNull();
        result!.CustomerId.Should().Be(1);
        result.Movements.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCurrentAccountMovements_ReturnsNotFound_WhenCustomerDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/current-accounts/99999/movements");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCurrentAccountMovements_SupportsDateFilters()
    {
        // Act
        var dateFrom = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
        var dateTo = DateTime.Now.ToString("yyyy-MM-dd");
        var response = await _client.GetAsync($"/api/current-accounts/1/movements?dateFrom={dateFrom}&dateTo={dateTo}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CurrentAccountMovementsResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCurrentAccountMovements_SupportsLineFilter_Billing()
    {
        // Act - Filter to Billing (L1) only
        var response = await _client.GetAsync("/api/current-accounts/1/movements?line=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CurrentAccountMovementsResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCurrentAccountMovements_SupportsLineFilter_Budget()
    {
        // Act - Filter to Budget (L2) only
        var response = await _client.GetAsync("/api/current-accounts/1/movements?line=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CurrentAccountMovementsResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCurrentAccountMovements_ReturnsBadRequest_ForInvalidLineFilter()
    {
        // Act - Invalid line filter
        var response = await _client.GetAsync("/api/current-accounts/1/movements?line=3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCurrentAccountMovements_SupportsPagination()
    {
        // Act
        var response = await _client.GetAsync("/api/current-accounts/1/movements?skip=0&take=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CurrentAccountMovementsResponse>();
        result.Should().NotBeNull();
        result!.Movements.Count.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public async Task GetCurrentAccountMovementsRange_ReturnsAllMovementsWithoutFixedTake50()
    {
        // Arrange
        var baseDate = new DateTime(2025, 1, 1);
        for (int i = 0; i < 75; i++)
        {
            await SeedMovementAsync(new CurrentAccountMovement
            {
                CustomerId = 1,
                MovementDate = baseDate.AddDays(i),
                DocumentType = DocumentType.InvoiceA,
                DocumentNumber = 1000 + i,
                BillingAmount = 10
            });
        }

        // Act
        var response = await _client.GetAsync("/api/current-accounts/1/movements/range?dateFrom=2025-01-01&dateTo=2025-03-16");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CurrentAccountMovementsResponse>();
        result.Should().NotBeNull();
        result!.Movements.Should().HaveCount(75);
        result.TotalCount.Should().Be(75);
        result.GuardrailApplied.Should().BeFalse();
    }

    [Fact]
    public async Task GetCurrentAccountMovementsRange_AllowsWideRanges_ForFullCustomerHistory()
    {
        // Act
        var response = await _client.GetAsync("/api/current-accounts/1/movements/range?dateFrom=2020-01-01&dateTo=2025-01-01");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CurrentAccountMovementsResponse>();
        result.Should().NotBeNull();
        result!.GuardrailMode.Should().NotBe("rejected");
    }

    [Fact]
    public async Task GetCurrentAccountMovements_ReturnsMovementsInAscendingOrder()
    {
        // Act
        var response = await _client.GetAsync("/api/current-accounts/1/movements");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CurrentAccountMovementsResponse>();
        result.Should().NotBeNull();

        // Verify ascending order (oldest first)
        if (result!.Movements.Count > 1)
        {
            for (int i = 1; i < result.Movements.Count; i++)
            {
                result.Movements[i].MovementDate.Should().BeOnOrAfter(result.Movements[i - 1].MovementDate);
            }
        }
    }

    [Fact]
    public async Task GetCurrentAccountMovements_MapsNavigationMetadata_ForOpenableInvoiceAndQuote()
    {
        // Arrange
        await SeedMovementAsync(new CurrentAccountMovement
        {
            CustomerId = 2,
            MovementDate = new DateTime(2026, 3, 1),
            DocumentType = DocumentType.InvoiceA,
            DocumentNumber = 12345,
            BillingAmount = 1000,
            Description = "Factura A 0002-00012345"
        });

        await SeedMovementAsync(new CurrentAccountMovement
        {
            CustomerId = 2,
            MovementDate = new DateTime(2026, 3, 2),
            DocumentType = DocumentType.Quote,
            DocumentNumber = 54321,
            BudgetAmount = 500
        });

        // Act
        var response = await _client.GetAsync("/api/current-accounts/2/movements");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CurrentAccountMovementsResponse>();
        result.Should().NotBeNull();

        var invoiceMovement = result!.Movements.Single(m => m.DocumentTypeCode == (int)DocumentType.InvoiceA);
        invoiceMovement.Navigation.TargetType.Should().Be("document");
        invoiceMovement.Navigation.TargetKind.Should().Be("invoice");
        invoiceMovement.Navigation.TargetRoute.Should().Be("/invoices/A/0002/12345?customerId=2");
        invoiceMovement.Navigation.TargetId.Should().Be("12345");
        invoiceMovement.Navigation.CanOpen.Should().BeTrue();
        invoiceMovement.Navigation.DisabledReason.Should().BeNull();

        var quoteMovement = result.Movements.Single(m => m.DocumentTypeCode == (int)DocumentType.Quote);
        quoteMovement.Navigation.TargetKind.Should().Be("quote");
        quoteMovement.Navigation.TargetRoute.Should().Be("/quotes/54321");
        quoteMovement.Navigation.TargetId.Should().Be("54321");
        quoteMovement.Navigation.CanOpen.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentAccountMovements_MapsNavigationMetadata_ForPaymentCreditDebitAndInitialBalance()
    {
        // Arrange
        await SeedMovementAsync(new CurrentAccountMovement
        {
            CustomerId = 3,
            MovementDate = new DateTime(2026, 3, 3),
            DocumentType = DocumentType.Payment,
            DocumentNumber = 7001,
            BillingAmount = -250
        });

        await SeedMovementAsync(new CurrentAccountMovement
        {
            CustomerId = 3,
            MovementDate = new DateTime(2026, 3, 4),
            DocumentType = DocumentType.CreditNoteA,
            DocumentNumber = 8001,
            BillingAmount = -100,
            Description = "Nota de Crédito A 0002-00008001"
        });

        await SeedMovementAsync(new CurrentAccountMovement
        {
            CustomerId = 3,
            MovementDate = new DateTime(2026, 3, 4),
            DocumentType = DocumentType.DebitNoteB,
            DocumentNumber = 8101,
            BillingAmount = 100,
            Description = "Nota de Débito B 0003-00008101"
        });

        await SeedMovementAsync(new CurrentAccountMovement
        {
            CustomerId = 3,
            MovementDate = new DateTime(2026, 3, 5),
            DocumentType = DocumentType.Other,
            DocumentNumber = 0,
            Description = "Saldo inicial"
        });

        // Act
        var response = await _client.GetAsync("/api/current-accounts/3/movements");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CurrentAccountMovementsResponse>();
        result.Should().NotBeNull();

        var paymentMovement = result!.Movements.Single(m => m.DocumentTypeCode == (int)DocumentType.Payment);
        paymentMovement.Navigation.TargetType.Should().Be("payment");
        paymentMovement.Navigation.TargetKind.Should().Be("payment");
        paymentMovement.Navigation.TargetRoute.Should().Be("/payments/7001?customerId=3");
        paymentMovement.Navigation.TargetId.Should().Be("7001");
        paymentMovement.Navigation.CanOpen.Should().BeTrue();
        paymentMovement.Navigation.DisabledReason.Should().BeNull();

        var creditNoteMovement = result.Movements.Single(m => m.DocumentTypeCode == (int)DocumentType.CreditNoteA);
        creditNoteMovement.Navigation.TargetType.Should().Be("document");
        creditNoteMovement.Navigation.TargetKind.Should().Be("credit-note");
        creditNoteMovement.Navigation.TargetRoute.Should().Be("/credit-notes/A/0002/8001?customerId=3");
        creditNoteMovement.Navigation.TargetId.Should().Be("8001");
        creditNoteMovement.Navigation.CanOpen.Should().BeTrue();
        creditNoteMovement.Navigation.DisabledReason.Should().BeNull();

        var debitNoteMovement = result.Movements.Single(m => m.DocumentTypeCode == (int)DocumentType.DebitNoteB);
        debitNoteMovement.Navigation.TargetType.Should().Be("document");
        debitNoteMovement.Navigation.TargetKind.Should().Be("debit-note");
        debitNoteMovement.Navigation.TargetRoute.Should().Be("/debit-notes/B/0003/8101?customerId=3");
        debitNoteMovement.Navigation.TargetId.Should().Be("8101");
        debitNoteMovement.Navigation.CanOpen.Should().BeTrue();
        debitNoteMovement.Navigation.DisabledReason.Should().BeNull();

        var initialBalanceMovement = result.Movements.Single(m => m.DocumentTypeCode == (int)DocumentType.Other);
        initialBalanceMovement.Navigation.TargetType.Should().Be("initial-balance");
        initialBalanceMovement.Navigation.TargetKind.Should().Be("initial-balance");
        initialBalanceMovement.Navigation.CanOpen.Should().BeFalse();
        initialBalanceMovement.Navigation.DisabledReason.Should().Be("Saldo inicial sin detalle navegable");
    }

    [Fact]
    public async Task GetCurrentAccountMovements_UsesResolvedDocumentType_ForNavigationMetadata()
    {
        // Arrange
        await SeedMovementAsync(new CurrentAccountMovement
        {
            CustomerId = 4,
            MovementDate = new DateTime(2026, 3, 6),
            DocumentType = DocumentType.Other,
            DocumentNumber = 4501,
            Description = "Factura A legacy import",
            BillingAmount = 2500
        });

        // Act
        var response = await _client.GetAsync("/api/current-accounts/4/movements");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CurrentAccountMovementsResponse>();
        result.Should().NotBeNull();

        var movement = result!.Movements.Single(m => m.DocumentNumber == 4501);
        movement.DocumentTypeCode.Should().Be((int)DocumentType.Other);
        movement.DocumentTypeShortCode.Should().Be("FA");
        movement.Navigation.TargetType.Should().Be("document");
        movement.Navigation.TargetKind.Should().Be("invoice");
        movement.Navigation.TargetRoute.Should().Be("/invoices/A/4501?customerId=4");
        movement.Navigation.TargetId.Should().Be("4501");
        movement.Navigation.CanOpen.Should().BeTrue();
        movement.Navigation.DisabledReason.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentAccountMovements_ReturnsRequiredDocumentTypeShortCode_AndOptionalMetadata()
    {
        // Arrange
        await SeedMovementAsync(new CurrentAccountMovement
        {
            CustomerId = 4,
            MovementDate = new DateTime(2026, 3, 6),
            DocumentType = DocumentType.InvoiceA,
            DocumentNumber = 4001,
            BillingAmount = 2500
        });

        // Act
        var response = await _client.GetAsync("/api/current-accounts/4/movements");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CurrentAccountMovementsResponse>();
        result.Should().NotBeNull();

        var movement = result!.Movements.Single(m => m.DocumentNumber == 4001);
        movement.DocumentTypeShortCode.Should().NotBeNullOrWhiteSpace();
        movement.DocumentTypeShortCode.Should().Be("FA");
        movement.DocumentTypeLabel.Should().NotBeNullOrWhiteSpace();
        movement.DocumentTypeTooltip.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetCurrentAccountMovements_ReturnsSI_OnlyForTrustedInitialBalanceRows()
    {
        // Arrange
        await SeedMovementAsync(new CurrentAccountMovement
        {
            CustomerId = 4,
            MovementDate = new DateTime(2026, 3, 7),
            DocumentType = DocumentType.Other,
            DocumentNumber = 0,
            Description = "Saldo inicial",
            BillingAmount = 100
        });

        await SeedMovementAsync(new CurrentAccountMovement
        {
            CustomerId = 4,
            MovementDate = new DateTime(2026, 3, 8),
            DocumentType = DocumentType.Other,
            DocumentNumber = 999,
            Description = "Saldo inicial ajustado",
            BillingAmount = 50
        });

        // Act
        var response = await _client.GetAsync("/api/current-accounts/4/movements");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CurrentAccountMovementsResponse>();
        result.Should().NotBeNull();

        var shortCodes = result!.Movements.Select(m => m.DocumentTypeShortCode).ToList();
        shortCodes.Should().Contain("SI");
        shortCodes.Count(code => code == "SI").Should().Be(1);
    }

    private async Task SeedMovementAsync(CurrentAccountMovement movement)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SPCDbContext>();
        db.CurrentAccountMovements.Add(movement);
        await db.SaveChangesAsync();
    }
}
