using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.API.Services;
using SPC.Shared.Models;

namespace SPC.Tests.Unit;

/// <summary>
/// Unit tests for CreditNoteQueryService (read-only credit note operations).
/// Uses InMemory database to verify query logic in isolation.
/// TDD: RED phase — these tests define expected behavior.
/// </summary>
public class CreditNoteQueryServiceTests : IDisposable
{
    private readonly SPCDbContext _db;
    private readonly ICreditNoteQueryService _service;

    public CreditNoteQueryServiceTests()
    {
        var options = new DbContextOptionsBuilder<SPCDbContext>()
            .UseInMemoryDatabase($"CreditNoteQueryTest_{Guid.NewGuid()}")
            .Options;

        _db = new SPCDbContext(options);
        _service = new CreditNoteQueryService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task SeedCreditNotesAsync()
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

        var invoice = new Invoice
        {
            Id = 100,
            BranchId = 1,
            InvoiceType = "A",
            PointOfSale = 2,
            InvoiceNumber = 1,
            InvoiceDate = DateTime.Today,
            CustomerId = 1,
            Subtotal = 1000m,
            Total = 1210m,
            IsVoided = false
        };

        _db.TaxConditions.Add(new TaxCondition { Id = 1, Code = "RI", Description = "Responsable Inscripto", InvoiceType = "A" });
        _db.Customers.AddRange(customer, customer2);
        _db.SalesReps.Add(salesRep);
        _db.Branches.Add(branch);
        _db.Invoices.Add(invoice);
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

        // Credit Note 1: Customer 1, Invoice 100, Today
        _db.CreditNotes.AddRange(
            new CreditNote
            {
                Id = 1,
                VoucherType = VoucherType.CreditNoteA,
                BranchId = 1,
                PointOfSale = 2,
                CreditNoteNumber = 1,
                CreditNoteDate = DateTime.Today,
                CustomerId = 1,
                SalesRepId = 1,
                InvoiceId = 100,
                VATPercent = 21m,
                Subtotal = 500m,
                VATAmount = 105m,
                Total = 605m,
                IsVoided = false,
                Details = new List<CreditNoteDetail>
                {
                    new CreditNoteDetail { Id = 1, ItemNumber = 1, ProductId = 1, Quantity = 1, UnitPrice = 500m, Subtotal = 500m }
                }
            },
            // Credit Note 2: Customer 1, Invoice 100, Yesterday
            new CreditNote
            {
                Id = 2,
                VoucherType = VoucherType.CreditNoteA,
                BranchId = 1,
                PointOfSale = 2,
                CreditNoteNumber = 2,
                CreditNoteDate = DateTime.Today.AddDays(-1),
                CustomerId = 1,
                SalesRepId = 1,
                InvoiceId = 100,
                VATPercent = 21m,
                Subtotal = 300m,
                VATAmount = 63m,
                Total = 363m,
                IsVoided = false,
                Details = new List<CreditNoteDetail>
                {
                    new CreditNoteDetail { Id = 2, ItemNumber = 1, ProductId = 1, Quantity = 1, UnitPrice = 300m, Subtotal = 300m }
                }
            },
            // Credit Note 3: Customer 2 (different customer), 2 months ago
            new CreditNote
            {
                Id = 3,
                VoucherType = VoucherType.CreditNoteB,
                BranchId = 1,
                PointOfSale = 2,
                CreditNoteNumber = 3,
                CreditNoteDate = DateTime.Today.AddMonths(-2),
                CustomerId = 2,
                SalesRepId = 1,
                InvoiceId = null,
                VATPercent = 21m,
                Subtotal = 200m,
                VATAmount = 42m,
                Total = 242m,
                IsVoided = true,
                Details = new List<CreditNoteDetail>
                {
                    new CreditNoteDetail { Id = 3, ItemNumber = 1, ProductId = 1, Quantity = 1, UnitPrice = 200m, Subtotal = 200m }
                }
            }
        );

        await _db.SaveChangesAsync();
    }

    // ===========================================
    // GetAllAsync
    // ===========================================

    [Fact]
    public async Task GetAllAsync_ReturnsAllCreditNotes_OrderedByDateDescending()
    {
        // Arrange
        await SeedCreditNotesAsync();

        // Act
        var result = (await _service.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].CreditNoteDate.Should().BeOnOrAfter(result[1].CreditNoteDate);
    }

    [Fact]
    public async Task GetAllAsync_AppliesPagination()
    {
        // Arrange
        await SeedCreditNotesAsync();

        // Act
        var result = (await _service.GetAllAsync(skip: 1, take: 1)).ToList();

        // Assert
        result.Should().HaveCount(1);
    }

    // ===========================================
    // GetByIdAsync
    // ===========================================

    [Fact]
    public async Task GetByIdAsync_ReturnsCreditNoteWithDetails_WhenExists()
    {
        // Arrange
        await SeedCreditNotesAsync();

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
        await SeedCreditNotesAsync();

        // Act
        var result = await _service.GetByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    // ===========================================
    // GetByCustomerAsync
    // ===========================================

    [Fact]
    public async Task GetByCustomerAsync_ReturnsOnlyCustomerCreditNotes()
    {
        // Arrange
        await SeedCreditNotesAsync();

        // Act
        var result = (await _service.GetByCustomerAsync(1)).ToList();

        // Assert
        result.Should().HaveCount(2); // Customer 1 has 2 credit notes
        result.Should().OnlyContain(cn => cn.CustomerId == 1);
    }

    [Fact]
    public async Task GetByCustomerAsync_ReturnsEmpty_WhenNoMatches()
    {
        // Arrange
        await SeedCreditNotesAsync();

        // Act
        var result = (await _service.GetByCustomerAsync(99999)).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    // ===========================================
    // GetByInvoiceAsync
    // ===========================================

    [Fact]
    public async Task GetByInvoiceAsync_ReturnsOnlyInvoiceCreditNotes()
    {
        // Arrange
        await SeedCreditNotesAsync();

        // Act
        var result = (await _service.GetByInvoiceAsync(100)).ToList();

        // Assert
        result.Should().HaveCount(2); // Invoice 100 has 2 credit notes
        result.Should().OnlyContain(cn => cn.InvoiceId == 100);
    }

    // ===========================================
    // GetByDateRangeAsync
    // ===========================================

    [Fact]
    public async Task GetByDateRangeAsync_ReturnsCreditNotesInRange()
    {
        // Arrange
        await SeedCreditNotesAsync();
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
        await SeedCreditNotesAsync();

        // Act
        var result = (await _service.SearchAsync("Acme")).ToList();

        // Assert
        result.Should().HaveCount(2); // Acme Corp has 2 credit notes
    }

    [Fact]
    public async Task SearchAsync_FindsByCreditNoteNumber()
    {
        // Arrange
        await SeedCreditNotesAsync();

        // Act
        var result = (await _service.SearchAsync("1")).ToList();

        // Assert
        result.Should().Contain(cn => cn.CreditNoteNumber == 1);
    }

    // ===========================================
    // GetCountAsync
    // ===========================================

    [Fact]
    public async Task GetCountAsync_ReturnsTotalCount()
    {
        // Arrange
        await SeedCreditNotesAsync();

        // Act
        var count = await _service.GetCountAsync();

        // Assert
        count.Should().Be(3);
    }
}
