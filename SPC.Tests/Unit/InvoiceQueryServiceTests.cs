using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.API.Services;
using SPC.Shared.Models;

namespace SPC.Tests.Unit;

/// <summary>
/// Unit tests for InvoiceQueryService (read-only invoice operations).
/// Uses InMemory database to verify query logic in isolation.
/// TDD: RED phase — these tests define expected behavior.
/// </summary>
public class InvoiceQueryServiceTests : IDisposable
{
    private readonly SPCDbContext _db;
    private readonly IInvoiceQueryService _service;

    public InvoiceQueryServiceTests()
    {
        var options = new DbContextOptionsBuilder<SPCDbContext>()
            .UseInMemoryDatabase($"InvoiceQueryTest_{Guid.NewGuid()}")
            .Options;

        _db = new SPCDbContext(options);
        _service = new InvoiceQueryService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task SeedInvoicesAsync()
    {
        var customer = new Customer
        {
            Id = 1,
            CompanyName = "Test Customer",
            CUIT = "20-12345678-9",
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

        _db.TaxConditions.Add(new TaxCondition { Id = 1, Code = "RI", Description = "Responsable Inscripto", InvoiceType = "A" });
        _db.Customers.Add(customer);
        _db.SalesReps.Add(salesRep);
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

        _db.Invoices.AddRange(
            new Invoice
            {
                Id = 1,
                BranchId = 1,
                InvoiceType = "A",
                PointOfSale = 2,
                InvoiceNumber = 1,
                InvoiceDate = DateTime.Today,
                CustomerId = 1,
                SalesRepId = 1,
                Subtotal = 1000m,
                VATAmount = 210m,
                Total = 1210m,
                IsVoided = false,
                Details = new List<InvoiceDetail>
                {
                    new InvoiceDetail { Id = 1, ItemNumber = 1, ProductId = 1, Quantity = 1, UnitPrice = 1000m, VATPercent = 21m, Subtotal = 1000m }
                }
            },
            new Invoice
            {
                Id = 2,
                BranchId = 1,
                InvoiceType = "B",
                PointOfSale = 2,
                InvoiceNumber = 2,
                InvoiceDate = DateTime.Today.AddDays(-1),
                CustomerId = 1,
                SalesRepId = 1,
                Subtotal = 1210m,
                VATAmount = 0m,
                Total = 1210m,
                IsVoided = false,
                Details = new List<InvoiceDetail>
                {
                    new InvoiceDetail { Id = 2, ItemNumber = 1, ProductId = 1, Quantity = 1, UnitPrice = 1210m, VATPercent = 21m, Subtotal = 1210m }
                }
            },
            new Invoice
            {
                Id = 3,
                BranchId = 1,
                InvoiceType = "A",
                PointOfSale = 2,
                InvoiceNumber = 3,
                InvoiceDate = DateTime.Today.AddMonths(-2),
                CustomerId = 1,
                SalesRepId = 1,
                Subtotal = 500m,
                VATAmount = 105m,
                Total = 605m,
                IsVoided = true,
                Details = new List<InvoiceDetail>
                {
                    new InvoiceDetail { Id = 3, ItemNumber = 1, ProductId = 1, Quantity = 1, UnitPrice = 500m, VATPercent = 21m, Subtotal = 500m }
                }
            }
        );

        await _db.SaveChangesAsync();
    }

    // ===========================================
    // GetAllAsync
    // ===========================================

    [Fact]
    public async Task GetAllAsync_ReturnsAllInvoices_OrderedByDateDescending()
    {
        // Arrange
        await SeedInvoicesAsync();

        // Act
        var result = (await _service.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].InvoiceDate.Should().BeOnOrAfter(result[1].InvoiceDate);
    }

    [Fact]
    public async Task GetAllAsync_AppliesPagination()
    {
        // Arrange
        await SeedInvoicesAsync();

        // Act
        var result = (await _service.GetAllAsync(skip: 1, take: 1)).ToList();

        // Assert
        result.Should().HaveCount(1);
    }

    // ===========================================
    // GetByIdAsync
    // ===========================================

    [Fact]
    public async Task GetByIdAsync_ReturnsInvoiceWithDetails_WhenExists()
    {
        // Arrange
        await SeedInvoicesAsync();

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Details.Should().HaveCount(1);
        result.CustomerCompanyName.Should().Be("Test Customer");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        await SeedInvoicesAsync();

        // Act
        var result = await _service.GetByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    // ===========================================
    // GetByCustomerAsync
    // ===========================================

    [Fact]
    public async Task GetByCustomerAsync_ReturnsOnlyCustomerInvoices()
    {
        // Arrange
        await SeedInvoicesAsync();

        // Act
        var result = (await _service.GetByCustomerAsync(1)).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(i => i.CustomerId == 1);
    }

    // ===========================================
    // GetByDateRangeAsync
    // ===========================================

    [Fact]
    public async Task GetByDateRangeAsync_ReturnsInvoicesInRange()
    {
        // Arrange
        await SeedInvoicesAsync();
        var from = DateTime.Today.AddDays(-1);
        var to = DateTime.Today;

        // Act
        var result = (await _service.GetByDateRangeAsync(from, to)).ToList();

        // Assert
        result.Should().HaveCount(2); // Only today and yesterday, not 2 months ago
    }

    // ===========================================
    // SearchAsync
    // ===========================================

    [Fact]
    public async Task SearchAsync_FindsByCustomerName()
    {
        // Arrange
        await SeedInvoicesAsync();

        // Act
        var result = (await _service.SearchAsync("Test Customer")).ToList();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task SearchAsync_FindsByInvoiceNumber()
    {
        // Arrange
        await SeedInvoicesAsync();

        // Act
        var result = (await _service.SearchAsync("1")).ToList();

        // Assert
        result.Should().Contain(i => i.InvoiceNumber == 1);
    }

    // ===========================================
    // GetSummaryAsync
    // ===========================================

    [Fact]
    public async Task GetSummaryAsync_CalculatesCorrectTotals()
    {
        // Arrange
        await SeedInvoicesAsync();

        // Act
        var result = await _service.GetSummaryAsync();

        // Assert
        result.TotalInvoices.Should().Be(3);
        // Today's non-voided: Invoice 1 (1210m)
        result.InvoicesHoy.Should().Be(1);
        result.MontoHoy.Should().Be(1210m);
    }

    // ===========================================
    // GetCountAsync
    // ===========================================

    [Fact]
    public async Task GetCountAsync_ReturnsTotalCount()
    {
        // Arrange
        await SeedInvoicesAsync();

        // Act
        var count = await _service.GetCountAsync();

        // Assert
        count.Should().Be(3);
    }
}
