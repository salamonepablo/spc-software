using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.API.Services;
using SPC.Shared.Models;

namespace SPC.Tests.Unit;

/// <summary>
/// Unit tests for DebitNoteQueryService (read-only debit note operations).
/// Uses InMemory database to verify query logic in isolation.
/// TDD: RED phase — these tests define expected behavior.
/// </summary>
public class DebitNoteQueryServiceTests : IDisposable
{
    private readonly SPCDbContext _db;
    private readonly IDebitNoteQueryService _service;

    public DebitNoteQueryServiceTests()
    {
        var options = new DbContextOptionsBuilder<SPCDbContext>()
            .UseInMemoryDatabase($"DebitNoteQueryTest_{Guid.NewGuid()}")
            .Options;

        _db = new SPCDbContext(options);
        _service = new DebitNoteQueryService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task SeedDebitNotesAsync()
    {
        var customer = new Customer
        {
            Id = 1,
            CompanyName = "Acme Corp",
            CUIT = "20-12345678-9",
            TaxConditionId = 1,
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        var customer2 = new Customer
        {
            Id = 2,
            CompanyName = "Beta Inc",
            CUIT = "20-98765432-1",
            TaxConditionId = 1,
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        var salesRep = new SalesRep
        {
            Id = 1,
            EmployeeCode = "V001",
            FirstName = "Juan",
            IsActive = true
        };

        var branch = new Branch
        {
            Id = 1,
            Code = "CALLE",
            Name = "Calle",
            PointOfSale = 2,
            IsActive = true
        };

        _db.TaxConditions.Add(new TaxCondition { Id = 1, Code = "RI", Description = "Responsable Inscripto", InvoiceType = "A" });
        _db.Customers.AddRange(customer, customer2);
        _db.SalesReps.Add(salesRep);
        _db.Branches.Add(branch);
        _db.Products.Add(new Product
        {
            Id = 1,
            Code = "BAT001",
            Description = "Bateria 12V 65AH",
            InvoicePrice = 1000m,
            QuotePrice = 1210m,
            SalePrice = 1000m,
            VATPercent = 21m,
            CategoryId = 1,
            IsActive = true
        });

        // Debit Note 1: Customer 1, Today
        _db.DebitNotes.AddRange(
            new DebitNote
            {
                Id = 1,
                VoucherType = VoucherType.DebitNoteA,
                BranchId = 1,
                PointOfSale = 2,
                DebitNoteNumber = 1,
                DebitNoteDate = DateTime.Today,
                CustomerId = 1,
                SalesRepId = 1,
                VATPercent = 21m,
                Subtotal = 500m,
                VATAmount = 105m,
                Total = 605m,
                IsVoided = false,
                Details = new List<DebitNoteDetail>
                {
                    new DebitNoteDetail { Id = 1, ItemNumber = 1, ProductId = 1, Quantity = 1, UnitPrice = 500m, Subtotal = 500m }
                }
            },
            // Debit Note 2: Customer 1, Yesterday
            new DebitNote
            {
                Id = 2,
                VoucherType = VoucherType.DebitNoteA,
                BranchId = 1,
                PointOfSale = 2,
                DebitNoteNumber = 2,
                DebitNoteDate = DateTime.Today.AddDays(-1),
                CustomerId = 1,
                SalesRepId = 1,
                VATPercent = 21m,
                Subtotal = 300m,
                VATAmount = 63m,
                Total = 363m,
                IsVoided = false,
                Details = new List<DebitNoteDetail>
                {
                    new DebitNoteDetail { Id = 2, ItemNumber = 1, ProductId = 1, Quantity = 1, UnitPrice = 300m, Subtotal = 300m }
                }
            },
            // Debit Note 3: Customer 2, 2 months ago
            new DebitNote
            {
                Id = 3,
                VoucherType = VoucherType.DebitNoteB,
                BranchId = 1,
                PointOfSale = 2,
                DebitNoteNumber = 3,
                DebitNoteDate = DateTime.Today.AddMonths(-2),
                CustomerId = 2,
                SalesRepId = 1,
                VATPercent = 21m,
                Subtotal = 200m,
                VATAmount = 42m,
                Total = 242m,
                IsVoided = true,
                Details = new List<DebitNoteDetail>
                {
                    new DebitNoteDetail { Id = 3, ItemNumber = 1, ProductId = 1, Quantity = 1, UnitPrice = 200m, Subtotal = 200m }
                }
            }
        );

        await _db.SaveChangesAsync();
    }

    // ===========================================
    // GetAllAsync
    // ===========================================

    [Fact]
    public async Task GetAllAsync_ReturnsAllDebitNotes_OrderedByDateDescending()
    {
        // Arrange
        await SeedDebitNotesAsync();

        // Act
        var result = (await _service.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].DebitNoteDate.Should().BeOnOrAfter(result[1].DebitNoteDate);
    }

    [Fact]
    public async Task GetAllAsync_AppliesPagination()
    {
        // Arrange
        await SeedDebitNotesAsync();

        // Act
        var result = (await _service.GetAllAsync(skip: 1, take: 1)).ToList();

        // Assert
        result.Should().HaveCount(1);
    }

    // ===========================================
    // GetByIdAsync
    // ===========================================

    [Fact]
    public async Task GetByIdAsync_ReturnsDebitNoteWithDetails_WhenExists()
    {
        // Arrange
        await SeedDebitNotesAsync();

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Details.Should().HaveCount(1);
        result.CustomerName.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        await SeedDebitNotesAsync();

        // Act
        var result = await _service.GetByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    // ===========================================
    // GetByCustomerAsync
    // ===========================================

    [Fact]
    public async Task GetByCustomerAsync_ReturnsOnlyCustomerDebitNotes()
    {
        // Arrange
        await SeedDebitNotesAsync();

        // Act
        var result = (await _service.GetByCustomerAsync(1)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(dn => dn.CustomerId == 1);
    }

    [Fact]
    public async Task GetByCustomerAsync_ReturnsEmpty_WhenNoMatches()
    {
        // Arrange
        await SeedDebitNotesAsync();

        // Act
        var result = (await _service.GetByCustomerAsync(99999)).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    // ===========================================
    // GetByDateRangeAsync
    // ===========================================

    [Fact]
    public async Task GetByDateRangeAsync_ReturnsDebitNotesInRange()
    {
        // Arrange
        await SeedDebitNotesAsync();
        var from = DateTime.Today.AddDays(-1);
        var to = DateTime.Today;

        // Act
        var result = (await _service.GetByDateRangeAsync(from, to)).ToList();

        // Assert
        result.Should().HaveCount(2); // Today and yesterday, not 2 months ago
    }

    // ===========================================
    // SearchAsync
    // ===========================================

    [Fact]
    public async Task SearchAsync_FindsByCustomerName()
    {
        // Arrange
        await SeedDebitNotesAsync();

        // Act
        var result = (await _service.SearchAsync("Acme")).ToList();

        // Assert
        result.Should().HaveCount(2); // Acme Corp has 2 debit notes
    }

    [Fact]
    public async Task SearchAsync_FindsByDebitNoteNumber()
    {
        // Arrange
        await SeedDebitNotesAsync();

        // Act
        var result = (await _service.SearchAsync("1")).ToList();

        // Assert
        result.Should().Contain(dn => dn.DebitNoteNumber == 1);
    }

    // ===========================================
    // GetCountAsync
    // ===========================================

    [Fact]
    public async Task GetCountAsync_ReturnsTotalCount()
    {
        // Arrange
        await SeedDebitNotesAsync();

        // Act
        var count = await _service.GetCountAsync();

        // Assert
        count.Should().Be(3);
    }
}
