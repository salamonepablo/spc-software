using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using SPC.API.Contracts.CreditNotes;
using SPC.API.Data;
using SPC.API.Services;
using SPC.Shared.Models;

namespace SPC.Tests.Unit;

/// <summary>
/// Unit tests for CreditNoteCommandService (write credit note operations).
/// Uses InMemory database with mocked dependencies.
/// TDD: RED phase — these tests define expected behavior.
/// </summary>
public class CreditNoteCommandServiceTests : IDisposable
{
    private readonly SPCDbContext _db;
    private readonly ICreditNoteCommandService _commandService;
    private readonly ICreditNoteQueryService _queryService;
    private readonly Mock<ICurrentAccountService> _currentAccountServiceMock;

    public CreditNoteCommandServiceTests()
    {
        var options = new DbContextOptionsBuilder<SPCDbContext>()
            .UseInMemoryDatabase($"CreditNoteCommandTest_{Guid.NewGuid()}")
            .Options;

        _db = new SPCDbContext(options);

        // Real PricingService (pure calculation, no DB)
        var pricingService = new PricingService();

        // Real TaxConfigurationService with InMemory DB
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>());
        var config = configBuilder.Build();
        var taxService = new TaxConfigurationService(_db, config);

        // Create query service (needed by command service for GetByIdAsync after create)
        _queryService = new CreditNoteQueryService(_db);

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

        _commandService = new CreditNoteCommandService(
            _db,
            taxService,
            pricingService,
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
        _db.Invoices.Add(new Invoice
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
        });

        await _db.SaveChangesAsync();
    }

    // ===========================================
    // CreateAsync
    // ===========================================

    [Fact]
    public async Task CreateAsync_CreatesCreditNoteA_WithCorrectTaxCalculations()
    {
        // Arrange
        await SeedBaseDataAsync();

        var request = new CreateCreditNoteRequest
        {
            BranchId = 1,
            VoucherType = "A",
            CustomerId = 1,
            InvoiceId = 100,
            DiscountPercent = 0,
            IIBBPercent = 0,
            Details = new List<CreateCreditNoteDetalleRequest>
            {
                new() { ProductId = 1, Quantity = 1, DiscountPercent = 0 }
            }
        };

        // Act
        var result = await _commandService.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.VoucherType.Should().Be("A");
        result.Subtotal.Should().Be(1000m);
        result.VATAmount.Should().Be(210m);
        result.Total.Should().Be(1210m);
        result.Details.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAsync_PersistsAndReturnsComplete()
    {
        // Arrange
        await SeedBaseDataAsync();

        var request = new CreateCreditNoteRequest
        {
            BranchId = 1,
            VoucherType = "A",
            CustomerId = 1,
            DiscountPercent = 0,
            IIBBPercent = 0,
            Details = new List<CreateCreditNoteDetalleRequest>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };

        // Act
        var result = await _commandService.CreateAsync(request);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Details.Should().NotBeEmpty();
        result.CustomerName.Should().Be("Test Customer");
    }

    [Fact]
    public async Task CreateAsync_LinksToSourceInvoice()
    {
        // Arrange
        await SeedBaseDataAsync();

        var request = new CreateCreditNoteRequest
        {
            BranchId = 1,
            VoucherType = "A",
            CustomerId = 1,
            InvoiceId = 100, // Link to invoice
            DiscountPercent = 0,
            IIBBPercent = 0,
            Details = new List<CreateCreditNoteDetalleRequest>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };

        // Act
        var result = await _commandService.CreateAsync(request);

        // Assert
        result.InvoiceId.Should().Be(100);
        var persisted = await _db.CreditNotes.FindAsync(result.Id);
        persisted!.InvoiceId.Should().Be(100);
    }

    [Fact]
    public async Task CreateAsync_GeneratesSequentialNumber()
    {
        // Arrange
        await SeedBaseDataAsync();

        var request = new CreateCreditNoteRequest
        {
            BranchId = 1,
            VoucherType = "A",
            CustomerId = 1,
            DiscountPercent = 0,
            IIBBPercent = 0,
            Details = new List<CreateCreditNoteDetalleRequest>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };

        // Act
        var first = await _commandService.CreateAsync(request);
        var second = await _commandService.CreateAsync(request);

        // Assert
        second.CreditNoteNumber.Should().Be(first.CreditNoteNumber + 1);
    }

    [Fact]
    public async Task CreateAsync_ExplicitZeroDiscount_OverridesCustomerDefault()
    {
        // Arrange
        await SeedBaseDataAsync();

        // Request specifies DiscountPercent = 0 explicitly
        // This overrides the customer's default 10% discount
        // because ResolveDiscount treats 0 as an explicit value
        var request = new CreateCreditNoteRequest
        {
            BranchId = 1,
            VoucherType = "A",
            CustomerId = 1, // Customer has 10% discount
            DiscountPercent = 0, // Explicit 0 overrides customer default
            IIBBPercent = 0,
            Details = new List<CreateCreditNoteDetalleRequest>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };

        // Act
        var result = await _commandService.CreateAsync(request);

        // Assert - Explicit 0 discount, no document-level discount applied
        result.DiscountPercent.Should().Be(0m);
        result.DiscountAmount.Should().Be(0m);
    }

    [Fact]
    public async Task CreateAsync_ThrowsForInvalidCustomer()
    {
        // Arrange
        await SeedBaseDataAsync();

        var request = new CreateCreditNoteRequest
        {
            BranchId = 1,
            VoucherType = "A",
            CustomerId = 99999, // Does not exist
            Details = new List<CreateCreditNoteDetalleRequest>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };

        // Act & Assert
        await _commandService.Invoking(s => s.CreateAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*99999*");
    }

    // ===========================================
    // VoidAsync
    // ===========================================

    [Fact]
    public async Task VoidAsync_MarksCreditNoteAsVoided()
    {
        // Arrange
        await SeedBaseDataAsync();
        var creditNote = new CreditNote
        {
            VoucherType = VoucherType.CreditNoteA,
            BranchId = 1,
            PointOfSale = 2,
            CreditNoteNumber = 1,
            CreditNoteDate = DateTime.Now,
            CustomerId = 1,
            Subtotal = 1000m,
            Total = 1210m,
            IsVoided = false
        };
        _db.CreditNotes.Add(creditNote);
        await _db.SaveChangesAsync();

        // Act
        var result = await _commandService.VoidAsync(creditNote.Id, "Error de facturación");

        // Assert
        result.Should().BeTrue();
        var voided = await _db.CreditNotes.FindAsync(creditNote.Id);
        voided!.IsVoided.Should().BeTrue();
        voided.Notes.Should().Contain("Error de facturación");
    }

    [Fact]
    public async Task VoidAsync_ReturnsFalse_WhenAlreadyVoided()
    {
        // Arrange
        await SeedBaseDataAsync();
        var creditNote = new CreditNote
        {
            VoucherType = VoucherType.CreditNoteA,
            BranchId = 1,
            PointOfSale = 2,
            CreditNoteNumber = 1,
            CreditNoteDate = DateTime.Now,
            CustomerId = 1,
            Subtotal = 1000m,
            Total = 1210m,
            IsVoided = true // Already voided
        };
        _db.CreditNotes.Add(creditNote);
        await _db.SaveChangesAsync();

        // Act
        var result = await _commandService.VoidAsync(creditNote.Id, "Double void");

        // Assert
        result.Should().BeFalse();
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
}
