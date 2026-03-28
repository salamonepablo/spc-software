using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SPC.API.Data;
using SPC.API.Services;
using SPC.Shared.Licensing;
using SPC.Shared.Models;

namespace SPC.Tests.Unit;

/// <summary>
/// Unit tests for CurrentAccountService.
/// Tests dual-line current account logic (Billing L1 + Budget L2).
/// </summary>
public class CurrentAccountServiceTests : IDisposable
{
    private readonly SPCDbContext _db;
    private readonly Mock<ILicenseService> _licenseServiceMock;

    public CurrentAccountServiceTests()
    {
        var options = new DbContextOptionsBuilder<SPCDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _db = new SPCDbContext(options);
        _licenseServiceMock = new Mock<ILicenseService>();
        
        // Seed a test customer
        _db.Customers.Add(new Customer 
        { 
            Id = 1, 
            CompanyName = "Test Customer",
            CUIT = "20-12345678-9"
        });
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private CurrentAccountService CreateService()
    {
        return new CurrentAccountService(_db, _licenseServiceMock.Object);
    }

    // ===========================================
    // GetOrCreateAccountAsync Tests
    // ===========================================

    [Fact]
    public async Task GetOrCreateAccountAsync_CreatesNewAccount_WhenNotExists()
    {
        // Arrange
        var service = CreateService();

        // Act
        var account = await service.GetOrCreateAccountAsync(customerId: 1);

        // Assert
        account.Should().NotBeNull();
        account.CustomerId.Should().Be(1);
        account.BillingBalance.Should().Be(0);
        account.BudgetBalance.Should().Be(0);
        account.TotalBalance.Should().Be(0);
    }

    [Fact]
    public async Task GetOrCreateAccountAsync_ReturnsExistingAccount_WhenExists()
    {
        // Arrange
        var existingAccount = new CurrentAccount
        {
            CustomerId = 1,
            BillingBalance = 1000,
            BudgetBalance = 500,
            TotalBalance = 1500,
            LastUpdated = DateTime.Now.AddDays(-1)
        };
        _db.CurrentAccounts.Add(existingAccount);
        await _db.SaveChangesAsync();
        
        var service = CreateService();

        // Act
        var account = await service.GetOrCreateAccountAsync(customerId: 1);

        // Assert
        account.Should().NotBeNull();
        account.BillingBalance.Should().Be(1000);
        account.BudgetBalance.Should().Be(500);
        account.TotalBalance.Should().Be(1500);
    }

    // ===========================================
    // IsDualLineEnabled Tests
    // ===========================================

    [Fact]
    public void IsDualLineEnabled_ReturnsTrue_WhenLicenseHasFeature()
    {
        // Arrange
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(Features.DualLineCurrentAccount))
            .Returns(true);
        var service = CreateService();

        // Act
        var result = service.IsDualLineEnabled();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsDualLineEnabled_ReturnsFalse_WhenLicenseDoesNotHaveFeature()
    {
        // Arrange
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(Features.DualLineCurrentAccount))
            .Returns(false);
        var service = CreateService();

        // Act
        var result = service.IsDualLineEnabled();

        // Assert
        result.Should().BeFalse();
    }

    // ===========================================
    // RecordMovementAsync - Budget (L2) Tests
    // ===========================================

    [Fact]
    public async Task RecordMovementAsync_UpdatesBudgetBalance_WhenDualLineEnabled()
    {
        // Arrange
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(Features.DualLineCurrentAccount))
            .Returns(true);
        var service = CreateService();

        // Act
        var account = await service.RecordMovementAsync(
            customerId: 1,
            documentType: DocumentType.Quote,
            documentNumber: 1,
            billingAmount: 0,
            budgetAmount: 1000,
            description: "Test Quote");

        // Assert
        account.BillingBalance.Should().Be(0);
        account.BudgetBalance.Should().Be(1000);
        account.TotalBalance.Should().Be(1000);
    }

    [Fact]
    public async Task RecordMovementAsync_DoesNotUpdateBudgetBalance_WhenDualLineDisabled()
    {
        // Arrange
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(Features.DualLineCurrentAccount))
            .Returns(false);
        var service = CreateService();

        // Act
        var account = await service.RecordMovementAsync(
            customerId: 1,
            documentType: DocumentType.Quote,
            documentNumber: 1,
            billingAmount: 0,
            budgetAmount: 1000,
            description: "Test Quote");

        // Assert
        account.BillingBalance.Should().Be(0);
        account.BudgetBalance.Should().Be(0); // Not updated because feature is disabled
        account.TotalBalance.Should().Be(0);
    }

    // ===========================================
    // RecordMovementAsync - Billing (L1) Tests
    // ===========================================

    [Fact]
    public async Task RecordMovementAsync_UpdatesBillingBalance_Always()
    {
        // Arrange - even with dual line disabled, billing should update
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(Features.DualLineCurrentAccount))
            .Returns(false);
        var service = CreateService();

        // Act
        var account = await service.RecordMovementAsync(
            customerId: 1,
            documentType: DocumentType.InvoiceA,
            documentNumber: 1,
            billingAmount: 5000,
            budgetAmount: 0,
            description: "Test Invoice");

        // Assert
        account.BillingBalance.Should().Be(5000);
        account.TotalBalance.Should().Be(5000);
    }

    [Fact]
    public async Task RecordMovementAsync_UpdatesBothBalances_WhenDualLineEnabled()
    {
        // Arrange
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(Features.DualLineCurrentAccount))
            .Returns(true);
        var service = CreateService();

        // Act - First an invoice (billing)
        await service.RecordMovementAsync(
            customerId: 1,
            documentType: DocumentType.InvoiceA,
            documentNumber: 1,
            billingAmount: 5000,
            budgetAmount: 0,
            description: "Invoice");

        // Then a quote (budget)
        var account = await service.RecordMovementAsync(
            customerId: 1,
            documentType: DocumentType.Quote,
            documentNumber: 1,
            billingAmount: 0,
            budgetAmount: 2000,
            description: "Quote");

        // Assert
        account.BillingBalance.Should().Be(5000);
        account.BudgetBalance.Should().Be(2000);
        account.TotalBalance.Should().Be(7000);
    }

    // ===========================================
    // RecordMovementAsync - Movement History Tests
    // ===========================================

    [Fact]
    public async Task RecordMovementAsync_CreatesMovementRecord()
    {
        // Arrange
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(Features.DualLineCurrentAccount))
            .Returns(true);
        var service = CreateService();

        // Act
        await service.RecordMovementAsync(
            customerId: 1,
            documentType: DocumentType.Quote,
            documentNumber: 12345,
            billingAmount: 0,
            budgetAmount: 1500,
            description: "Presupuesto 0001-00012345");

        // Assert
        var movements = await _db.CurrentAccountMovements.ToListAsync();
        movements.Should().HaveCount(1);
        
        var movement = movements.First();
        movement.CustomerId.Should().Be(1);
        movement.DocumentType.Should().Be(DocumentType.Quote);
        movement.DocumentNumber.Should().Be(12345);
        movement.BillingAmount.Should().Be(0);
        movement.BudgetAmount.Should().Be(1500);
        movement.BudgetRunningBalance.Should().Be(1500);
        movement.Description.Should().Be("Presupuesto 0001-00012345");
    }

    [Fact]
    public async Task RecordMovementAsync_MovementHasZeroBudgetAmount_WhenDualLineDisabled()
    {
        // Arrange
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(Features.DualLineCurrentAccount))
            .Returns(false);
        var service = CreateService();

        // Act
        await service.RecordMovementAsync(
            customerId: 1,
            documentType: DocumentType.Quote,
            documentNumber: 1,
            billingAmount: 0,
            budgetAmount: 1000,
            description: "Test");

        // Assert
        var movement = await _db.CurrentAccountMovements.FirstAsync();
        movement.BudgetAmount.Should().Be(0); // Not recorded because feature is disabled
    }

    // ===========================================
    // RecordMovementAsync - Running Balance Tests
    // ===========================================

    [Fact]
    public async Task RecordMovementAsync_TracksRunningBalance_ForMultipleMovements()
    {
        // Arrange
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(Features.DualLineCurrentAccount))
            .Returns(true);
        var service = CreateService();

        // Act - Multiple quotes
        await service.RecordMovementAsync(1, DocumentType.Quote, 1, 0, 1000, "Quote 1");
        await service.RecordMovementAsync(1, DocumentType.Quote, 2, 0, 2000, "Quote 2");
        await service.RecordMovementAsync(1, DocumentType.Quote, 3, 0, 500, "Quote 3");

        // Assert
        var movements = await _db.CurrentAccountMovements
            .OrderBy(m => m.Id)
            .ToListAsync();
        
        movements.Should().HaveCount(3);
        movements[0].BudgetRunningBalance.Should().Be(1000);
        movements[1].BudgetRunningBalance.Should().Be(3000);
        movements[2].BudgetRunningBalance.Should().Be(3500);
    }

    // ===========================================
    // RecordMovementAsync - Void/Reversal Tests
    // ===========================================

    [Fact]
    public async Task RecordMovementAsync_ReducesBudgetBalance_WhenQuoteVoided()
    {
        // Arrange
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(Features.DualLineCurrentAccount))
            .Returns(true);
        var service = CreateService();

        // First create a quote
        await service.RecordMovementAsync(1, DocumentType.Quote, 1, 0, 1000, "Quote");

        // Act - Void the quote (negative amount)
        var account = await service.RecordMovementAsync(
            customerId: 1,
            documentType: DocumentType.QuoteVoid,
            documentNumber: 1,
            billingAmount: 0,
            budgetAmount: -1000, // Negative to reverse
            description: "Anulación Quote");

        // Assert
        account.BudgetBalance.Should().Be(0);
        account.TotalBalance.Should().Be(0);
    }

    [Fact]
    public async Task RecordMovementAsync_ReducesBillingBalance_WhenCreditNoteApplied()
    {
        // Arrange
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(Features.DualLineCurrentAccount))
            .Returns(true);
        var service = CreateService();

        // First create an invoice
        await service.RecordMovementAsync(1, DocumentType.InvoiceA, 1, 5000, 0, "Invoice");

        // Act - Apply credit note (negative billing)
        var account = await service.RecordMovementAsync(
            customerId: 1,
            documentType: DocumentType.CreditNoteA,
            documentNumber: 1,
            billingAmount: -1000, // Negative to reduce debt
            budgetAmount: 0,
            description: "Credit Note");

        // Assert
        account.BillingBalance.Should().Be(4000);
        account.TotalBalance.Should().Be(4000);
    }

    // ===========================================
    // GetMovementsAsync Tests
    // ===========================================

    [Fact]
    public async Task GetMovementsAsync_ReturnsMovementsOrderedByDateDescending()
    {
        // Arrange
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(Features.DualLineCurrentAccount))
            .Returns(true);
        var service = CreateService();

        await service.RecordMovementAsync(1, DocumentType.Quote, 1, 0, 100, "First");
        await service.RecordMovementAsync(1, DocumentType.Quote, 2, 0, 200, "Second");
        await service.RecordMovementAsync(1, DocumentType.Quote, 3, 0, 300, "Third");

        // Act
        var movements = await service.GetMovementsAsync(customerId: 1);

        // Assert
        var list = movements.ToList();
        list.Should().HaveCount(3);
        list[0].Description.Should().Be("Third");  // Most recent first
        list[1].Description.Should().Be("Second");
        list[2].Description.Should().Be("First");
    }

    [Fact]
    public async Task GetMovementsAsync_SupportsPagination()
    {
        // Arrange
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(Features.DualLineCurrentAccount))
            .Returns(true);
        var service = CreateService();

        for (int i = 1; i <= 10; i++)
        {
            await service.RecordMovementAsync(1, DocumentType.Quote, i, 0, i * 100, $"Quote {i}");
        }

        // Act
        var page1 = await service.GetMovementsAsync(customerId: 1, skip: 0, take: 3);
        var page2 = await service.GetMovementsAsync(customerId: 1, skip: 3, take: 3);

        // Assert
        page1.Should().HaveCount(3);
        page2.Should().HaveCount(3);
        page1.First().DocumentNumber.Should().Be(10); // Most recent
        page2.First().DocumentNumber.Should().Be(7);
    }

    // ===========================================
    // GetAccountAsync Tests (Task 2.1)
    // ===========================================

    [Fact]
    public async Task GetAccountAsync_ReturnsAccount_WhenExists()
    {
        // Arrange
        var existingAccount = new CurrentAccount
        {
            CustomerId = 1,
            BillingBalance = 5000,
            BudgetBalance = 3000,
            TotalBalance = 8000,
            LastUpdated = DateTime.Now
        };
        _db.CurrentAccounts.Add(existingAccount);
        await _db.SaveChangesAsync();

        var service = CreateService();

        // Act
        var account = await service.GetAccountAsync(customerId: 1);

        // Assert
        account.Should().NotBeNull();
        account!.BillingBalance.Should().Be(5000);
        account.BudgetBalance.Should().Be(3000);
        account.TotalBalance.Should().Be(8000);
    }

    [Fact]
    public async Task GetAccountAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        var service = CreateService();

        // Act
        var account = await service.GetAccountAsync(customerId: 999);

        // Assert
        account.Should().BeNull();
    }

    [Fact]
    public async Task GetAccountAsync_IncludesCustomerData()
    {
        // Arrange
        _db.CurrentAccounts.Add(new CurrentAccount
        {
            CustomerId = 1,
            BillingBalance = 1000,
            BudgetBalance = 500,
            TotalBalance = 1500,
            LastUpdated = DateTime.Now
        });
        await _db.SaveChangesAsync();

        var service = CreateService();

        // Act
        var account = await service.GetAccountAsync(customerId: 1);

        // Assert
        account.Should().NotBeNull();
        account!.Customer.Should().NotBeNull();
        account.Customer!.CompanyName.Should().Be("Test Customer");
        account.Customer.CUIT.Should().Be("20-12345678-9");
    }

    // ===========================================
    // GetMovementsFilteredAsync Tests (Task 2.4)
    // ===========================================

    [Fact]
    public async Task GetMovementsFilteredAsync_FiltersByDateRange()
    {
        // Arrange
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(Features.DualLineCurrentAccount))
            .Returns(true);

        // Add movements with different dates directly
        _db.CurrentAccountMovements.AddRange(
            new CurrentAccountMovement { CustomerId = 1, MovementDate = new DateTime(2025, 10, 1), DocumentType = DocumentType.Quote, DocumentNumber = 1, BudgetAmount = 100 },
            new CurrentAccountMovement { CustomerId = 1, MovementDate = new DateTime(2025, 10, 15), DocumentType = DocumentType.Quote, DocumentNumber = 2, BudgetAmount = 200 },
            new CurrentAccountMovement { CustomerId = 1, MovementDate = new DateTime(2025, 11, 1), DocumentType = DocumentType.Quote, DocumentNumber = 3, BudgetAmount = 300 }
        );
        await _db.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetMovementsFilteredAsync(
            customerId: 1,
            dateFrom: new DateTime(2025, 10, 1),
            dateTo: new DateTime(2025, 10, 31),
            line: null,
            skip: 0,
            take: 50);

        // Assert
        result.Movements.Should().HaveCount(2);
        result.Movements.All(m => m.MovementDate.Month == 10).Should().BeTrue();
    }

    [Fact]
    public async Task GetMovementsFilteredAsync_FiltersByLine1_BillingOnly()
    {
        // Arrange
        _db.CurrentAccountMovements.AddRange(
            new CurrentAccountMovement { CustomerId = 1, MovementDate = DateTime.Now, DocumentType = DocumentType.InvoiceA, DocumentNumber = 1, BillingAmount = 1000, BudgetAmount = 0 },
            new CurrentAccountMovement { CustomerId = 1, MovementDate = DateTime.Now, DocumentType = DocumentType.Quote, DocumentNumber = 2, BillingAmount = 0, BudgetAmount = 500 }
        );
        await _db.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetMovementsFilteredAsync(
            customerId: 1,
            dateFrom: null,
            dateTo: null,
            line: 1, // Billing only
            skip: 0,
            take: 50);

        // Assert
        result.Movements.Should().HaveCount(1);
        result.Movements.First().BillingAmount.Should().Be(1000);
    }

    [Fact]
    public async Task GetMovementsFilteredAsync_FiltersByLine2_BudgetOnly()
    {
        // Arrange
        _db.CurrentAccountMovements.AddRange(
            new CurrentAccountMovement { CustomerId = 1, MovementDate = DateTime.Now, DocumentType = DocumentType.InvoiceA, DocumentNumber = 1, BillingAmount = 1000, BudgetAmount = 0 },
            new CurrentAccountMovement { CustomerId = 1, MovementDate = DateTime.Now, DocumentType = DocumentType.Quote, DocumentNumber = 2, BillingAmount = 0, BudgetAmount = 500 }
        );
        await _db.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetMovementsFilteredAsync(
            customerId: 1,
            dateFrom: null,
            dateTo: null,
            line: 2, // Budget only
            skip: 0,
            take: 50);

        // Assert
        result.Movements.Should().HaveCount(1);
        result.Movements.First().BudgetAmount.Should().Be(500);
    }

    [Fact]
    public async Task GetMovementsFilteredAsync_ReturnsTotalCount()
    {
        // Arrange
        for (int i = 0; i < 25; i++)
        {
            _db.CurrentAccountMovements.Add(new CurrentAccountMovement
            {
                CustomerId = 1,
                MovementDate = DateTime.Now,
                DocumentType = DocumentType.Quote,
                DocumentNumber = i,
                BudgetAmount = 100
            });
        }
        await _db.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetMovementsFilteredAsync(
            customerId: 1,
            dateFrom: null,
            dateTo: null,
            line: null,
            skip: 0,
            take: 10);

        // Assert
        result.Movements.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
    }

    [Fact]
    public async Task GetMovementsFilteredAsync_IncludesCurrentBalances()
    {
        // Arrange
        _db.CurrentAccounts.Add(new CurrentAccount
        {
            CustomerId = 1,
            BillingBalance = 5000,
            BudgetBalance = 3000,
            TotalBalance = 8000,
            LastUpdated = DateTime.Now
        });
        await _db.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetMovementsFilteredAsync(
            customerId: 1,
            dateFrom: null,
            dateTo: null,
            line: null,
            skip: 0,
            take: 50);

        // Assert
        result.BillingBalance.Should().Be(5000);
        result.BudgetBalance.Should().Be(3000);
        result.TotalBalance.Should().Be(8000);
    }
}
