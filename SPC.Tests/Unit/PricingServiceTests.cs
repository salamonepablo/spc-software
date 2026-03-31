using FluentAssertions;
using SPC.API.Services;

namespace SPC.Tests.Unit;

/// <summary>
/// Unit tests for PricingService business calculations.
/// </summary>
public class PricingServiceTests
{
    private readonly PricingService _service;

    public PricingServiceTests()
    {
        _service = new PricingService();
    }

    // ===========================================
    // LINE CALCULATION TESTS
    // ===========================================

    [Fact]
    public void CalculateLine_WithNoDiscount_ReturnsCorrectSubtotal()
    {
        // Arrange
        decimal unitPrice = 100m;
        decimal quantity = 2m;
        decimal discountPercent = 0m;
        decimal vatPercent = 21m;

        // Act
        var result = _service.CalculateLine(unitPrice, quantity, discountPercent, vatPercent);

        // Assert
        result.GrossAmount.Should().Be(200m);
        result.DiscountAmount.Should().Be(0m);
        result.Subtotal.Should().Be(200m);
    }

    [Fact]
    public void CalculateLine_WithDiscount_CalculatesCorrectly()
    {
        // Arrange
        decimal unitPrice = 100m;
        decimal quantity = 1m;
        decimal discountPercent = 10m;  // 10% discount
        decimal vatPercent = 21m;

        // Act
        var result = _service.CalculateLine(unitPrice, quantity, discountPercent, vatPercent);

        // Assert
        result.GrossAmount.Should().Be(100m);
        result.DiscountAmount.Should().Be(10m);
        result.Subtotal.Should().Be(90m);
    }

    [Fact]
    public void CalculateLine_PreservesVATPercentage()
    {
        // Arrange
        decimal unitPrice = 100m;
        decimal quantity = 1m;
        decimal discountPercent = 0m;
        decimal vatPercent = 10.5m;  // Reduced VAT

        // Act
        var result = _service.CalculateLine(unitPrice, quantity, discountPercent, vatPercent);

        // Assert
        result.VATPercent.Should().Be(10.5m);
    }

    [Fact]
    public void CalculateLine_RoundsToTwoDecimals()
    {
        // Arrange - Create a scenario that would produce more than 2 decimals
        decimal unitPrice = 99.99m;
        decimal quantity = 3m;
        decimal discountPercent = 7m;  // 7% discount

        // Act
        var result = _service.CalculateLine(unitPrice, quantity, discountPercent, 21m);

        // Assert
        // Gross = 99.99 * 3 = 299.97
        // Discount = 299.97 * 0.07 = 20.9979 -> rounded to 21.00
        // Subtotal = 299.97 - 21.00 = 278.97
        result.GrossAmount.Should().Be(299.97m);
        result.DiscountAmount.Should().Be(21.00m);
        result.Subtotal.Should().Be(278.97m);
    }

    // ===========================================
    // DOCUMENT CALCULATION TESTS
    // ===========================================

    [Fact]
    public void CalculateDocument_WithNoDiscountsOrTaxes_ReturnsSumOfLines()
    {
        // Arrange
        var lines = new[]
        {
            new LineCalculationResult { Subtotal = 100m, VATPercent = 21m },
            new LineCalculationResult { Subtotal = 200m, VATPercent = 21m }
        };

        // Act
        var result = _service.CalculateDocument(lines, 0, 0, 0);

        // Assert
        result.LinesSubtotal.Should().Be(300m);
        result.DocumentDiscountAmount.Should().Be(0m);
        result.NetSubtotal.Should().Be(300m);
        result.VATAmount.Should().Be(0m);
        result.IIBBAmount.Should().Be(0m);
        result.Total.Should().Be(300m);
    }

    [Fact]
    public void CalculateDocument_WithVAT_CalculatesCorrectly()
    {
        // Arrange
        var lines = new[]
        {
            new LineCalculationResult { Subtotal = 1000m, VATPercent = 21m }
        };

        // Act
        var result = _service.CalculateDocument(lines, 0, 21, 0);

        // Assert
        result.NetSubtotal.Should().Be(1000m);
        result.VATPercent.Should().Be(21m);
        result.VATAmount.Should().Be(210m);
        result.Total.Should().Be(1210m);
    }

    [Fact]
    public void CalculateDocument_WithDocumentDiscount_AppliesBeforeVAT()
    {
        // Arrange
        var lines = new[]
        {
            new LineCalculationResult { Subtotal = 1000m, VATPercent = 21m }
        };

        // Act - 10% document discount
        var result = _service.CalculateDocument(lines, 10, 21, 0);

        // Assert
        // Lines subtotal = 1000
        // Document discount = 100
        // Net = 900
        // VAT = 900 * 21% = 189
        // Total = 900 + 189 = 1089
        result.LinesSubtotal.Should().Be(1000m);
        result.DocumentDiscountAmount.Should().Be(100m);
        result.NetSubtotal.Should().Be(900m);
        result.VATAmount.Should().Be(189m);
        result.Total.Should().Be(1089m);
    }

    [Fact]
    public void CalculateDocument_WithIIBB_CalculatesOnTotalWithVAT()
    {
        // Arrange
        var lines = new[]
        {
            new LineCalculationResult { Subtotal = 1000m, VATPercent = 21m }
        };

        // Act - 3% IIBB
        var result = _service.CalculateDocument(lines, 0, 21, 3);

        // Assert
        // Net = 1000
        // VAT = 210
        // IIBB base = 1210, IIBB = 36.30
        // Total = 1000 + 210 + 36.30 = 1246.30
        result.IIBBPercent.Should().Be(3m);
        result.IIBBAmount.Should().Be(36.30m);
        result.Total.Should().Be(1246.30m);
    }

    [Fact]
    public void CalculateDocument_WithAllComponents_CalculatesCorrectly()
    {
        // Arrange - Complex scenario with all discounts and taxes
        var lines = new[]
        {
            new LineCalculationResult { Subtotal = 900m, VATPercent = 21m },  // Already has 10% line discount
            new LineCalculationResult { Subtotal = 1000m, VATPercent = 21m }
        };

        // Act - 5% doc discount, 21% VAT, 3% IIBB
        var result = _service.CalculateDocument(lines, 5, 21, 3);

        // Assert
        // Lines subtotal = 1900
        // Doc discount = 95
        // Net = 1805
        // VAT = 1805 * 21% = 379.05
        // IIBB base = 2184.05, IIBB = 65.52 (rounded)
        // Total = 1805 + 379.05 + 65.52 = 2249.57
        result.LinesSubtotal.Should().Be(1900m);
        result.DocumentDiscountAmount.Should().Be(95m);
        result.NetSubtotal.Should().Be(1805m);
        result.VATAmount.Should().Be(379.05m);
        result.IIBBAmount.Should().Be(65.52m);
        result.Total.Should().Be(2249.57m);
    }

    // ===========================================
    // RESOLVE DISCOUNT TESTS
    // ===========================================

    [Fact]
    public void ResolveDiscount_WithExplicitValue_ReturnsExplicitValue()
    {
        // Act
        var result = _service.ResolveDiscount(15m, 10m);

        // Assert
        result.Should().Be(15m);
    }

    [Fact]
    public void ResolveDiscount_WithNull_ReturnsCustomerDefault()
    {
        // Act
        var result = _service.ResolveDiscount(null, 10m);

        // Assert
        result.Should().Be(10m);
    }

    [Fact]
    public void ResolveDiscount_WithExplicitZero_ReturnsZero()
    {
        // Explicitly setting 0 should override customer default
        // Act
        var result = _service.ResolveDiscount(0m, 10m);

        // Assert
        result.Should().Be(0m);
    }
}
