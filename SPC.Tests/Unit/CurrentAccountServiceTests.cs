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
    public async Task GetMovementsAsync_ReturnsMovementsOrderedByDateAscending()
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

        // Assert - Oldest first for proper running balance display
        var list = movements.ToList();
        list.Should().HaveCount(3);
        list[0].Description.Should().Be("First");   // Oldest first
        list[1].Description.Should().Be("Second");
        list[2].Description.Should().Be("Third");
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

        // Assert - Ascending order (oldest first)
        page1.Should().HaveCount(3);
        page2.Should().HaveCount(3);
        page1.First().DocumentNumber.Should().Be(1); // Oldest first
        page2.First().DocumentNumber.Should().Be(4);
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

    // ===========================================
    // SetInitialBalanceAsync Tests
    // ===========================================

    [Fact]
    public async Task SetInitialBalanceAsync_CreatesAccountWithInitialBalance()
    {
        // Arrange
        var service = CreateService();

        // Act
        var account = await service.SetInitialBalanceAsync(
            customerId: 1,
            billingBalance: 5000,
            budgetBalance: 2000);

        // Assert
        account.Should().NotBeNull();
        account.BillingBalance.Should().Be(5000);
        account.BudgetBalance.Should().Be(2000);
        account.TotalBalance.Should().Be(7000);
    }

    [Fact]
    public async Task SetInitialBalanceAsync_CreatesSaldoInicialMovement()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.SetInitialBalanceAsync(
            customerId: 1,
            billingBalance: 5000,
            budgetBalance: 2000);

        // Assert
        var movements = await _db.CurrentAccountMovements.ToListAsync();
        movements.Should().HaveCount(1);
        movements[0].Description.Should().Be("Saldo inicial");
        movements[0].BillingAmount.Should().Be(5000);
        movements[0].BudgetAmount.Should().Be(2000);
        movements[0].BillingRunningBalance.Should().Be(5000);
        movements[0].BudgetRunningBalance.Should().Be(2000);
    }

    [Fact]
    public async Task SetInitialBalanceAsync_ThrowsIfMovementsExist()
    {
        // Arrange
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(Features.DualLineCurrentAccount))
            .Returns(true);
        var service = CreateService();

        // Create a movement first
        await service.RecordMovementAsync(1, DocumentType.Quote, 1, 0, 100, "Existing");

        // Act & Assert
        await service.Invoking(s => s.SetInitialBalanceAsync(1, 5000, 2000))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has movements*");
    }

    [Fact]
    public async Task SetInitialBalanceAsync_UsesProvidedDate()
    {
        // Arrange
        var service = CreateService();
        var specificDate = new DateTime(2025, 1, 1);

        // Act
        await service.SetInitialBalanceAsync(
            customerId: 1,
            billingBalance: 5000,
            budgetBalance: 2000,
            asOfDate: specificDate);

        // Assert
        var movement = await _db.CurrentAccountMovements.FirstAsync();
        movement.MovementDate.Should().Be(specificDate);
    }

    // ===========================================
    // Period Balance Calculation Tests (Task 2.1-2.5)
    // ===========================================

    [Fact]
    public async Task GetMovementsFilteredAsync_CalculatesInitialBalance_WhenDateFromProvided()
    {
        // Arrange: Create movements before and during the period
        _db.CurrentAccountMovements.AddRange(
            // Before period (should be summed into initial balance)
            new CurrentAccountMovement { CustomerId = 1, MovementDate = new DateTime(2025, 9, 1), DocumentType = DocumentType.InvoiceA, DocumentNumber = 1, BillingAmount = 1000, BudgetAmount = 0, BillingRunningBalance = 1000, BudgetRunningBalance = 0 },
            new CurrentAccountMovement { CustomerId = 1, MovementDate = new DateTime(2025, 9, 15), DocumentType = DocumentType.Quote, DocumentNumber = 2, BillingAmount = 0, BudgetAmount = 500, BillingRunningBalance = 1000, BudgetRunningBalance = 500 },
            // During period (should be returned with recalculated running balance)
            new CurrentAccountMovement { CustomerId = 1, MovementDate = new DateTime(2025, 10, 5), DocumentType = DocumentType.InvoiceA, DocumentNumber = 3, BillingAmount = 2000, BudgetAmount = 0, BillingRunningBalance = 3000, BudgetRunningBalance = 500 },
            new CurrentAccountMovement { CustomerId = 1, MovementDate = new DateTime(2025, 10, 20), DocumentType = DocumentType.Quote, DocumentNumber = 4, BillingAmount = 0, BudgetAmount = 300, BillingRunningBalance = 3000, BudgetRunningBalance = 800 }
        );
        await _db.SaveChangesAsync();

        var service = CreateService();

        // Act: Filter October only
        var result = await service.GetMovementsFilteredAsync(
            customerId: 1,
            dateFrom: new DateTime(2025, 10, 1),
            dateTo: new DateTime(2025, 10, 31),
            line: null,
            skip: 0,
            take: 50);

        // Assert: Initial balance should be sum of movements BEFORE October
        result.InitialBillingBalance.Should().Be(1000);  // Sum of September billing
        result.InitialBudgetBalance.Should().Be(500);    // Sum of September budget
        result.InitialTotalBalance.Should().Be(1500);    // Sum total
    }

    [Fact]
    public async Task GetMovementsFilteredAsync_RecalculatesRunningBalance_FromInitialBalance()
    {
        // Arrange: Create movements before and during the period
        _db.CurrentAccountMovements.AddRange(
            // Before period
            new CurrentAccountMovement { CustomerId = 1, MovementDate = new DateTime(2025, 9, 1), DocumentType = DocumentType.InvoiceA, DocumentNumber = 1, BillingAmount = 1000, BudgetAmount = 0, BillingRunningBalance = 1000, BudgetRunningBalance = 0 },
            new CurrentAccountMovement { CustomerId = 1, MovementDate = new DateTime(2025, 9, 15), DocumentType = DocumentType.Quote, DocumentNumber = 2, BillingAmount = 0, BudgetAmount = 500, BillingRunningBalance = 1000, BudgetRunningBalance = 500 },
            // During period
            new CurrentAccountMovement { CustomerId = 1, MovementDate = new DateTime(2025, 10, 5), DocumentType = DocumentType.InvoiceA, DocumentNumber = 3, BillingAmount = 2000, BudgetAmount = 0, BillingRunningBalance = 3000, BudgetRunningBalance = 500 },
            new CurrentAccountMovement { CustomerId = 1, MovementDate = new DateTime(2025, 10, 20), DocumentType = DocumentType.Quote, DocumentNumber = 4, BillingAmount = 0, BudgetAmount = 300, BillingRunningBalance = 3000, BudgetRunningBalance = 800 }
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

        // Assert: Running balances should be recalculated from initial balance
        result.Movements.Should().HaveCount(2);

        // First movement in period: initial(1000,500) + amount(2000,0) = (3000,500)
        result.Movements[0].BillingRunningBalance.Should().Be(3000);
        result.Movements[0].BudgetRunningBalance.Should().Be(500);

        // Second movement: (3000,500) + (0,300) = (3000,800)
        result.Movements[1].BillingRunningBalance.Should().Be(3000);
        result.Movements[1].BudgetRunningBalance.Should().Be(800);
    }

    [Fact]
    public async Task GetMovementsFilteredAsync_CalculatesFinalBalance_FromLastMovement()
    {
        // Arrange
        _db.CurrentAccountMovements.AddRange(
            new CurrentAccountMovement { CustomerId = 1, MovementDate = new DateTime(2025, 9, 1), DocumentType = DocumentType.InvoiceA, DocumentNumber = 1, BillingAmount = 1000, BudgetAmount = 0 },
            new CurrentAccountMovement { CustomerId = 1, MovementDate = new DateTime(2025, 10, 5), DocumentType = DocumentType.InvoiceA, DocumentNumber = 2, BillingAmount = 2000, BudgetAmount = 0 },
            new CurrentAccountMovement { CustomerId = 1, MovementDate = new DateTime(2025, 10, 20), DocumentType = DocumentType.Quote, DocumentNumber = 3, BillingAmount = 0, BudgetAmount = 300 }
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

        // Assert: Final balance should be the running balance after last movement
        result.FinalBillingBalance.Should().Be(3000);  // 1000 initial + 2000
        result.FinalBudgetBalance.Should().Be(300);    // 0 initial + 300
        result.FinalTotalBalance.Should().Be(3300);
    }

    [Fact]
    public async Task GetMovementsFilteredAsync_InitialBalanceIsZero_WhenNoMovementsBeforePeriod()
    {
        // Arrange: Only movements during the period
        _db.CurrentAccountMovements.AddRange(
            new CurrentAccountMovement { CustomerId = 1, MovementDate = new DateTime(2025, 10, 5), DocumentType = DocumentType.InvoiceA, DocumentNumber = 1, BillingAmount = 2000, BudgetAmount = 0 }
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

        // Assert: Initial balance should be 0 (no prior movements)
        result.InitialBillingBalance.Should().Be(0);
        result.InitialBudgetBalance.Should().Be(0);
        result.InitialTotalBalance.Should().Be(0);
    }

    [Fact]
    public async Task GetMovementsFilteredAsync_InitialEqualsCurrentBalance_WhenNoDateFromFilter()
    {
        // Arrange
        _db.CurrentAccountMovements.AddRange(
            new CurrentAccountMovement { CustomerId = 1, MovementDate = new DateTime(2025, 10, 5), DocumentType = DocumentType.InvoiceA, DocumentNumber = 1, BillingAmount = 2000, BudgetAmount = 0 }
        );
        await _db.SaveChangesAsync();

        var service = CreateService();

        // Act: No dateFrom filter (show all)
        var result = await service.GetMovementsFilteredAsync(
            customerId: 1,
            dateFrom: null,
            dateTo: null,
            line: null,
            skip: 0,
            take: 50);

        // Assert: Initial balance should be 0 when showing all movements from start
        result.InitialBillingBalance.Should().Be(0);
        result.InitialBudgetBalance.Should().Be(0);
        result.InitialTotalBalance.Should().Be(0);
    }

    [Fact]
    public async Task GetMovementsByRangeAsync_ReturnsAllMovementsInPeriod_WithoutFixed50Cap()
    {
        // Arrange
        var baseDate = new DateTime(2025, 1, 1);
        for (int i = 0; i < 75; i++)
        {
            _db.CurrentAccountMovements.Add(new CurrentAccountMovement
            {
                CustomerId = 1,
                MovementDate = baseDate.AddDays(i),
                DocumentType = DocumentType.InvoiceA,
                DocumentNumber = i + 1,
                BillingAmount = 10
            });
        }

        await _db.SaveChangesAsync();
        var service = CreateService();

        // Act
        var result = await service.GetMovementsByRangeAsync(
            customerId: 1,
            dateFrom: baseDate,
            dateTo: baseDate.AddDays(74),
            line: null,
            cancellationToken: CancellationToken.None);

        // Assert
        result.Movements.Should().HaveCount(75);
        result.TotalCount.Should().Be(75);
        result.GuardrailApplied.Should().BeFalse();
    }

    [Fact]
    public async Task GetMovementsByRangeAsync_AppliesRangeGuardrail_WhenRequestedSpanExceedsConfiguredMaxDays()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetMovementsByRangeAsync(
            customerId: 1,
            dateFrom: new DateTime(2020, 1, 1),
            dateTo: new DateTime(2025, 1, 1),
            line: null,
            cancellationToken: CancellationToken.None);

        // Assert
        result.GuardrailApplied.Should().BeTrue();
        result.GuardrailMode.Should().Be("rejected");
        result.WarningCode.Should().Be("RANGE_TOO_WIDE");
        result.Movements.Should().BeEmpty();
    }
}
