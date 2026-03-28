using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SPC.API.Contracts.Quotes;
using SPC.API.Data;
using SPC.API.Services;
using SPC.Shared.Licensing;
using SPC.Shared.Models;

namespace SPC.Tests.Unit;

/// <summary>
/// Unit tests for QuoteCommandService (write quote operations).
/// Uses InMemory database with real dependencies.
/// TDD: RED phase — these tests define expected behavior.
/// </summary>
public class QuoteCommandServiceTests : IDisposable
{
    private readonly SPCDbContext _db;
    private readonly IQuoteCommandService _commandService;
    private readonly IQuoteQueryService _queryService;

    public QuoteCommandServiceTests()
    {
        var options = new DbContextOptionsBuilder<SPCDbContext>()
            .UseInMemoryDatabase($"QuoteCommandTest_{Guid.NewGuid()}")
            .Options;

        _db = new SPCDbContext(options);

        var pricingService = new PricingService();

        // CurrentAccountService needs ILicenseService
        var licensingOptions = Options.Create(new LicensingOptions());
        var logger = Mock.Of<ILogger<LicenseService>>();
        var licenseService = new LicenseService(licensingOptions, logger);
        var currentAccountService = new CurrentAccountService(_db, licenseService);

        _queryService = new QuoteQueryService(_db);

        _commandService = new QuoteCommandService(
            _db,
            pricingService,
            currentAccountService,
            _queryService);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task SeedBaseDataAsync()
    {
        _db.TaxConditions.Add(new TaxCondition { Id = 1, Code = "RI", Description = "Responsable Inscripto", InvoiceType = "A" });
        _db.Branches.Add(new Branch { Id = 1, Code = "CALLE", Name = "Calle", PointOfSale = 2, IsActive = true });
        _db.Customers.Add(new Customer
        {
            Id = 1,
            CompanyName = "Test Customer",
            CUIT = "20-12345678-9",
            TaxConditionId = 1,
            DiscountPercent = 10m,
            IsActive = true,
            CreatedDate = DateTime.Now
        });
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

        await _db.SaveChangesAsync();
    }

    // ===========================================
    // CreateAsync
    // ===========================================

    [Fact]
    public async Task CreateAsync_CreatesQuote_WithCorrectPricing()
    {
        await SeedBaseDataAsync();

        var request = new CreateQuoteRequest
        {
            BranchId = 1,
            CustomerId = 1,
            DiscountPercent = 0,
            Details = new List<CreateQuoteDetalleRequest>
            {
                new() { ProductId = 1, Quantity = 1, DiscountPercent = 0 }
            }
        };

        var result = await _commandService.CreateAsync(request);

        result.Should().NotBeNull();
        result.Total.Should().Be(1210m); // QuotePrice
        result.Details.Should().HaveCount(1);
        result.Details[0].UnitPrice.Should().Be(1210m);
    }

    [Fact]
    public async Task CreateAsync_AppliesLineAndDocumentDiscounts()
    {
        await SeedBaseDataAsync();

        var request = new CreateQuoteRequest
        {
            BranchId = 1,
            CustomerId = 1,
            DiscountPercent = 10,
            Details = new List<CreateQuoteDetalleRequest>
            {
                new() { ProductId = 1, Quantity = 1, DiscountPercent = 10 }
            }
        };

        var result = await _commandService.CreateAsync(request);

        // QuotePrice = 1210, Line -10% = 1089, Doc -10% = 108.90, Total = 980.10
        result.Total.Should().Be(980.10m);
    }

    [Fact]
    public async Task CreateAsync_ThrowsForInvalidCustomer()
    {
        await SeedBaseDataAsync();

        var request = new CreateQuoteRequest
        {
            BranchId = 1,
            CustomerId = 99999,
            Details = new List<CreateQuoteDetalleRequest>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };

        await _commandService.Invoking(s => s.CreateAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*99999*");
    }

    [Fact]
    public async Task CreateAsync_ThrowsForInvalidProduct()
    {
        await SeedBaseDataAsync();

        var request = new CreateQuoteRequest
        {
            BranchId = 1,
            CustomerId = 1,
            Details = new List<CreateQuoteDetalleRequest>
            {
                new() { ProductId = 99999, Quantity = 1 }
            }
        };

        await _commandService.Invoking(s => s.CreateAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*99999*");
    }

    [Fact]
    public async Task CreateAsync_ThrowsForZeroTotal()
    {
        await SeedBaseDataAsync();

        var request = new CreateQuoteRequest
        {
            BranchId = 1,
            CustomerId = 1,
            Details = new List<CreateQuoteDetalleRequest>
            {
                new() { ProductId = 1, Quantity = 1, UnitPrice = 0 }
            }
        };

        await _commandService.Invoking(s => s.CreateAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public async Task CreateAsync_AssignsSequentialQuoteNumber()
    {
        await SeedBaseDataAsync();

        var request = new CreateQuoteRequest
        {
            BranchId = 1,
            CustomerId = 1,
            DiscountPercent = 0,
            Details = new List<CreateQuoteDetalleRequest>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };

        var first = await _commandService.CreateAsync(request);
        var second = await _commandService.CreateAsync(request);

        second.QuoteNumber.Should().Be(first.QuoteNumber + 1);
    }

    // ===========================================
    // VoidAsync
    // ===========================================

    [Fact]
    public async Task VoidAsync_MarksQuoteAsVoided()
    {
        await SeedBaseDataAsync();
        var quote = new Quote
        {
            BranchId = 1,
            QuoteNumber = 1,
            QuoteDate = DateTime.Now,
            CustomerId = 1,
            Subtotal = 1210m,
            Total = 1210m,
            IsVoided = false
        };
        _db.Quotes.Add(quote);
        await _db.SaveChangesAsync();

        var result = await _commandService.VoidAsync(quote.Id, "Test void reason");

        result.Should().BeTrue();
        var voided = await _db.Quotes.FindAsync(quote.Id);
        voided!.IsVoided.Should().BeTrue();
        voided.Notes.Should().Contain("Test void reason");
    }

    [Fact]
    public async Task VoidAsync_ReturnsFalse_WhenNotFound()
    {
        await SeedBaseDataAsync();

        var result = await _commandService.VoidAsync(99999, "Not found");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task VoidAsync_ReturnsFalse_WhenAlreadyVoided()
    {
        await SeedBaseDataAsync();
        var quote = new Quote
        {
            BranchId = 1,
            QuoteNumber = 1,
            QuoteDate = DateTime.Now,
            CustomerId = 1,
            Subtotal = 1210m,
            Total = 1210m,
            IsVoided = true
        };
        _db.Quotes.Add(quote);
        await _db.SaveChangesAsync();

        var result = await _commandService.VoidAsync(quote.Id, "Double void");

        result.Should().BeFalse();
    }
}
