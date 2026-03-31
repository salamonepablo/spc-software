using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using SPC.API.Contracts.Invoices;
using SPC.API.Data;
using SPC.API.Services;
using SPC.Shared.Licensing;
using SPC.Shared.Models;

namespace SPC.Tests.Unit;

/// <summary>
/// Unit tests for InvoiceCommandService (write invoice operations).
/// Uses InMemory database with mocked dependencies.
/// TDD: RED phase — these tests define expected behavior.
/// </summary>
public class InvoiceCommandServiceTests : IDisposable
{
    private readonly SPCDbContext _db;
    private readonly IInvoiceCommandService _commandService;
    private readonly IInvoiceQueryService _queryService;
    private readonly Mock<ICurrentAccountService> _currentAccountServiceMock;

    public InvoiceCommandServiceTests()
    {
        var options = new DbContextOptionsBuilder<SPCDbContext>()
            .UseInMemoryDatabase($"InvoiceCommandTest_{Guid.NewGuid()}")
            .Options;

        _db = new SPCDbContext(options);

        // Real PricingService (pure calculation, no DB)
        var pricingService = new PricingService();

        // Real TaxConfigurationService with InMemory DB
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>());
        var config = configBuilder.Build();
        var taxService = new TaxConfigurationService(_db, config);

        // Real CompanySettingsService with InMemory DB
        var companySettingsService = new CompanySettingsService(_db);

        // Create query service (needed by command service for GetByIdAsync after create)
        _queryService = new InvoiceQueryService(_db);

        // Mock CurrentAccountService (no-op for existing tests)
        _currentAccountServiceMock = new Mock<ICurrentAccountService>();
        _currentAccountServiceMock
            .Setup(x => x.RecordMovementAsync(
                It.IsAny<int>(),
                It.IsAny<DocumentType>(),
                It.IsAny<long>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new CurrentAccount());

        _commandService = new InvoiceCommandService(
            _db,
            taxService,
            pricingService,
            companySettingsService,
            _queryService,
            _currentAccountServiceMock.Object);
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
        _db.TaxSettings.Add(new TaxSetting
        {
            Id = 1,
            TaxCode = "VAT",
            Description = "IVA General",
            Rate = 21.00m,
            IsDefault = true,
            IsActive = true,
            EffectiveFrom = new DateTime(2000, 1, 1)
        });
        _db.CompanySettings.Add(new CompanySettings
        {
            Id = 1,
            CompanyName = "SPC Test",
            CUIT = "30-12345678-9",
            IsIIBBPerceptionAgent = false,
            IsIVAWithholdingAgent = false,
            IsActive = true
        });

        await _db.SaveChangesAsync();
    }

    // ===========================================
    // CreateAsync
    // ===========================================

    [Fact]
    public async Task CreateAsync_CreatesInvoiceA_WithCorrectCalculations()
    {
        // Arrange
        await SeedBaseDataAsync();

        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            InvoiceType = "A",
            CustomerId = 1,
            DiscountPercent = 0,
            Details = new List<CreateInvoiceDetailRequest>
            {
                new() { ProductId = 1, Quantity = 1, DiscountPercent = 0 }
            }
        };

        // Act
        var result = await _commandService.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.InvoiceType.Should().Be("A");
        result.Subtotal.Should().Be(1000m);
        result.VATAmount.Should().Be(210m);
        result.Total.Should().Be(1210m);
        result.Details.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAsync_CreatesInvoiceB_WithVATIncluded()
    {
        // Arrange
        await SeedBaseDataAsync();

        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            InvoiceType = "B",
            CustomerId = 1,
            DiscountPercent = 0,
            Details = new List<CreateInvoiceDetailRequest>
            {
                new() { ProductId = 1, Quantity = 1, DiscountPercent = 0 }
            }
        };

        // Act
        var result = await _commandService.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.InvoiceType.Should().Be("B");
        result.Total.Should().Be(1210m);
        result.Subtotal.Should().Be(result.Total); // In B, Subtotal = Total
        result.VATAmount.Should().Be(0m); // Not discriminated in B
    }

    [Fact]
    public async Task CreateAsync_ExplicitZeroDiscount_OverridesCustomerDefault()
    {
        // Arrange
        await SeedBaseDataAsync();

        // Request specifies DiscountPercent = 0 explicitly
        // This overrides the customer's default 10% discount
        // because ResolveDiscount treats 0 as an explicit value (decimal, not decimal?)
        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            InvoiceType = "A",
            CustomerId = 1,
            DiscountPercent = 0,
            Details = new List<CreateInvoiceDetailRequest>
            {
                new() { ProductId = 1, Quantity = 1, DiscountPercent = 0 }
            }
        };

        // Act
        var result = await _commandService.CreateAsync(request);

        // Assert - Explicit 0 discount, no document-level discount applied
        result.DiscountAmount.Should().Be(0m);
        result.Subtotal.Should().Be(1000m); // No discount
        result.Total.Should().Be(1210m); // 1000 + 210 VAT
    }

    [Fact]
    public async Task CreateAsync_ThrowsForInvalidCustomer()
    {
        // Arrange
        await SeedBaseDataAsync();

        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            InvoiceType = "A",
            CustomerId = 99999, // Does not exist
            Details = new List<CreateInvoiceDetailRequest>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };

        // Act & Assert
        await _commandService.Invoking(s => s.CreateAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*99999*");
    }

    [Fact]
    public async Task CreateAsync_ThrowsForInvalidProduct()
    {
        // Arrange
        await SeedBaseDataAsync();

        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            InvoiceType = "A",
            CustomerId = 1,
            Details = new List<CreateInvoiceDetailRequest>
            {
                new() { ProductId = 99999, Quantity = 1 }
            }
        };

        // Act & Assert
        await _commandService.Invoking(s => s.CreateAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*99999*");
    }

    [Fact]
    public async Task CreateAsync_AssignsSequentialInvoiceNumber()
    {
        // Arrange
        await SeedBaseDataAsync();

        var request = new CreateInvoiceRequest
        {
            BranchId = 1,
            InvoiceType = "A",
            CustomerId = 1,
            DiscountPercent = 0,
            Details = new List<CreateInvoiceDetailRequest>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };

        // Act
        var first = await _commandService.CreateAsync(request);
        var second = await _commandService.CreateAsync(request);

        // Assert
        second.InvoiceNumber.Should().Be(first.InvoiceNumber + 1);
    }

    // ===========================================
    // VoidAsync
    // ===========================================

    [Fact]
    public async Task VoidAsync_MarksInvoiceAsVoided()
    {
        // Arrange
        await SeedBaseDataAsync();
        var invoice = new Invoice
        {
            BranchId = 1,
            InvoiceType = "A",
            PointOfSale = 2,
            InvoiceNumber = 1,
            InvoiceDate = DateTime.Now,
            CustomerId = 1,
            Subtotal = 1000m,
            Total = 1210m,
            IsVoided = false
        };
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        // Act
        var result = await _commandService.VoidAsync(invoice.Id, "Test void reason");

        // Assert
        result.Should().BeTrue();
        var voided = await _db.Invoices.FindAsync(invoice.Id);
        voided!.IsVoided.Should().BeTrue();
        voided.Notes.Should().Contain("Test void reason");
    }

    [Fact]
    public async Task VoidAsync_ReturnsFalse_WhenNotFound()
    {
        // Arrange
        await SeedBaseDataAsync();

        // Act
        var result = await _commandService.VoidAsync(99999, "Not found");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task VoidAsync_ReturnsFalse_WhenAlreadyVoided()
    {
        // Arrange
        await SeedBaseDataAsync();
        var invoice = new Invoice
        {
            BranchId = 1,
            InvoiceType = "A",
            PointOfSale = 2,
            InvoiceNumber = 1,
            InvoiceDate = DateTime.Now,
            CustomerId = 1,
            Subtotal = 1000m,
            Total = 1210m,
            IsVoided = true // Already voided
        };
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        // Act
        var result = await _commandService.VoidAsync(invoice.Id, "Double void");

        // Assert
        result.Should().BeFalse();
    }
}
