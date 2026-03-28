using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.API.Services;
using SPC.Shared.Models;

namespace SPC.Tests.Unit;

/// <summary>
/// Unit tests for QuoteQueryService (read-only quote operations).
/// Uses InMemory database to verify query logic in isolation.
/// TDD: RED phase — these tests define expected behavior.
/// </summary>
public class QuoteQueryServiceTests : IDisposable
{
    private readonly SPCDbContext _db;
    private readonly IQuoteQueryService _service;

    public QuoteQueryServiceTests()
    {
        var options = new DbContextOptionsBuilder<SPCDbContext>()
            .UseInMemoryDatabase($"QuoteQueryTest_{Guid.NewGuid()}")
            .Options;

        _db = new SPCDbContext(options);
        _service = new QuoteQueryService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task SeedQuotesAsync()
    {
        var customer = new Customer
        {
            Id = 1,
            CompanyName = "Test Customer",
            TradeName = "Test Trade",
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

        var branch = new Branch
        {
            Id = 1,
            Code = "CALLE",
            Name = "Calle",
            PointOfSale = 2,
            IsActive = true
        };

        _db.TaxConditions.Add(new TaxCondition { Id = 1, Code = "RI", Description = "Responsable Inscripto", InvoiceType = "A" });
        _db.Customers.Add(customer);
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

        _db.Quotes.AddRange(
            new Quote
            {
                Id = 1,
                BranchId = 1,
                QuoteNumber = 1,
                QuoteDate = DateTime.Today,
                CustomerId = 1,
                SalesRepId = 1,
                Subtotal = 1210m,
                Total = 1210m,
                IsVoided = false,
                Details = new List<QuoteDetail>
                {
                    new QuoteDetail { Id = 1, ItemNumber = 1, ProductId = 1, Quantity = 1, UnitPrice = 1210m, Subtotal = 1210m }
                }
            },
            new Quote
            {
                Id = 2,
                BranchId = 1,
                QuoteNumber = 2,
                QuoteDate = DateTime.Today.AddDays(-1),
                CustomerId = 1,
                SalesRepId = 1,
                Subtotal = 2420m,
                Total = 2420m,
                IsVoided = false,
                Details = new List<QuoteDetail>
                {
                    new QuoteDetail { Id = 2, ItemNumber = 1, ProductId = 1, Quantity = 2, UnitPrice = 1210m, Subtotal = 2420m }
                }
            },
            new Quote
            {
                Id = 3,
                BranchId = 1,
                QuoteNumber = 3,
                QuoteDate = DateTime.Today.AddMonths(-2),
                CustomerId = 1,
                SalesRepId = 1,
                Subtotal = 605m,
                Total = 605m,
                IsVoided = true,
                Details = new List<QuoteDetail>
                {
                    new QuoteDetail { Id = 3, ItemNumber = 1, ProductId = 1, Quantity = 1, UnitPrice = 605m, Subtotal = 605m }
                }
            }
        );

        await _db.SaveChangesAsync();
    }

    // ===========================================
    // GetAllAsync
    // ===========================================

    [Fact]
    public async Task GetAllAsync_ReturnsAllQuotes_OrderedByDateDescending()
    {
        await SeedQuotesAsync();

        var result = (await _service.GetAllAsync()).ToList();

        result.Should().HaveCount(3);
        result[0].QuoteDate.Should().BeOnOrAfter(result[1].QuoteDate);
    }

    [Fact]
    public async Task GetAllAsync_AppliesPagination()
    {
        await SeedQuotesAsync();

        var result = (await _service.GetAllAsync(skip: 1, take: 1)).ToList();

        result.Should().HaveCount(1);
    }

    // ===========================================
    // GetByIdAsync
    // ===========================================

    [Fact]
    public async Task GetByIdAsync_ReturnsQuoteWithDetails_WhenExists()
    {
        await SeedQuotesAsync();

        var result = await _service.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Details.Should().HaveCount(1);
        result.CustomerName.Should().Be("Test Customer");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        await SeedQuotesAsync();

        var result = await _service.GetByIdAsync(99999);

        result.Should().BeNull();
    }

    // ===========================================
    // GetByCustomerAsync
    // ===========================================

    [Fact]
    public async Task GetByCustomerAsync_ReturnsOnlyCustomerQuotes()
    {
        await SeedQuotesAsync();

        var result = (await _service.GetByCustomerAsync(1)).ToList();

        result.Should().HaveCount(3);
        result.Should().OnlyContain(q => q.CustomerId == 1);
    }

    // ===========================================
    // GetByDateRangeAsync
    // ===========================================

    [Fact]
    public async Task GetByDateRangeAsync_ReturnsQuotesInRange()
    {
        await SeedQuotesAsync();
        var from = DateTime.Today.AddDays(-1);
        var to = DateTime.Today;

        var result = (await _service.GetByDateRangeAsync(from, to)).ToList();

        result.Should().HaveCount(2);
    }

    // ===========================================
    // SearchAsync
    // ===========================================

    [Fact]
    public async Task SearchAsync_FindsByCustomerName()
    {
        await SeedQuotesAsync();

        var result = (await _service.SearchAsync("Test Customer")).ToList();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task SearchAsync_FindsByQuoteNumber()
    {
        await SeedQuotesAsync();

        var result = (await _service.SearchAsync("1")).ToList();

        result.Should().Contain(q => q.QuoteNumber == 1);
    }

    // ===========================================
    // GetSummaryAsync
    // ===========================================

    [Fact]
    public async Task GetSummaryAsync_CalculatesCorrectTotals()
    {
        await SeedQuotesAsync();

        var result = await _service.GetSummaryAsync();

        result.TotalQuotes.Should().Be(3);
        result.QuotesHoy.Should().Be(1); // Only 1 non-voided today
        result.MontoHoy.Should().Be(1210m);
    }

    // ===========================================
    // GetCountAsync
    // ===========================================

    [Fact]
    public async Task GetCountAsync_ReturnsTotalCount()
    {
        await SeedQuotesAsync();

        var count = await _service.GetCountAsync();

        count.Should().Be(3);
    }
}
