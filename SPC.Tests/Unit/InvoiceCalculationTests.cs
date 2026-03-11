using FluentAssertions;
using SPC.API.Services;

namespace SPC.Tests.Unit;

/// <summary>
/// TDD Tests for Invoice calculation rules.
/// 
/// Business Rules:
/// 1. Invoice A: Prices are NET (without VAT), VAT is added and discriminated
/// 2. Invoice B: Prices are FINAL (with VAT included), shows "IVA Contenido"
/// 3. IIBB perception only applies if company is "Agente de Percepción"
/// 4. IIBB rate comes from customer (AFIP/ARCA padrón)
/// </summary>
public class InvoiceCalculationTests
{
    private readonly PricingService _pricingService;

    public InvoiceCalculationTests()
    {
        _pricingService = new PricingService();
    }

    // ===========================================
    // FACTURA A TESTS (Net prices + VAT discriminated)
    // ===========================================

    [Fact]
    public void InvoiceA_CalculatesVATOnNetPrice()
    {
        // Arrange - Invoice A uses PrecioInvoice (net, without VAT)
        // Product price: 1000 (net)
        // Expected: Subtotal=1000, VAT=210, Total=1210
        
        var lines = new[]
        {
            new LineCalculationResult { Subtotal = 1000m, VATPercent = 21m }
        };

        // Act
        var result = _pricingService.CalculateDocumentTypeA(lines, 0, 21, 0, false);

        // Assert
        result.NetSubtotal.Should().Be(1000m);
        result.VATAmount.Should().Be(210m);
        result.Total.Should().Be(1210m);
    }

    [Fact]
    public void InvoiceA_WithMultipleLines_SumsCorrectly()
    {
        // Arrange - Multiple products, Invoice A
        var lines = new[]
        {
            new LineCalculationResult { Subtotal = 81485m, VATPercent = 21m },  // BATERIA 12 X 45
            new LineCalculationResult { Subtotal = 357955m, VATPercent = 21m }, // 5x BATERIA 65AH
        };

        // Act
        var result = _pricingService.CalculateDocumentTypeA(lines, 0, 21, 0, false);

        // Assert
        result.NetSubtotal.Should().Be(439440m);
        result.VATAmount.Should().Be(92282.40m);
        result.Total.Should().Be(531722.40m);
    }

    // ===========================================
    // FACTURA B TESTS (Final prices with VAT included)
    // ===========================================

    [Fact]
    public void InvoiceB_UsesIncludedVAT_DoesNotAddVAT()
    {
        // Arrange - Invoice B: price already includes VAT
        // Product price: 1210 (with VAT included)
        // Expected: Total=1210, IVAContenido=210
        
        var lines = new[]
        {
            new LineCalculationResult { Subtotal = 1210m, VATPercent = 21m }
        };

        // Act
        var result = _pricingService.CalculateDocumentTypeB(lines, 0, 21);

        // Assert
        result.Total.Should().Be(1210m);
        result.VATContained.Should().Be(210m); // IVA Contenido (Ley 27.743)
        result.NetSubtotal.Should().Be(1000m); // Net before VAT for reporting
    }

    [Fact]
    public void InvoiceB_CalculatesVATContained_Correctly()
    {
        // Arrange - From PDF example: Total=32748.72, IVA Contenido=5683.66
        // VAT contained = Total / 1.21 * 0.21 = Total * 0.21 / 1.21
        var lines = new[]
        {
            new LineCalculationResult { Subtotal = 32748.72m, VATPercent = 21m }
        };

        // Act
        var result = _pricingService.CalculateDocumentTypeB(lines, 0, 21);

        // Assert
        result.Total.Should().Be(32748.72m);
        result.VATContained.Should().BeApproximately(5683.66m, 0.01m);
    }

    [Fact]
    public void InvoiceB_WithDiscount_AppliesBeforeVATCalculation()
    {
        // Arrange - Invoice B with 10% discount
        var lines = new[]
        {
            new LineCalculationResult { Subtotal = 1210m, VATPercent = 21m }
        };

        // Act - 10% document discount
        var result = _pricingService.CalculateDocumentTypeB(lines, 10, 21);

        // Assert
        // Original: 1210, Discount: 121, Final: 1089
        // VAT Contained = 1089 / 1.21 * 0.21 = 189
        result.Total.Should().Be(1089m);
        result.DocumentDiscountAmount.Should().Be(121m);
        result.VATContained.Should().BeApproximately(189m, 0.01m);
    }

    // ===========================================
    // IIBB PERCEPTION TESTS
    // ===========================================

    [Fact]
    public void IIBB_OnlyApplies_WhenCompanyIsPerceptionAgent()
    {
        // Arrange - Company is NOT an IIBB perception agent
        var lines = new[]
        {
            new LineCalculationResult { Subtotal = 1000m, VATPercent = 21m }
        };

        // Act - isIIBBPerceptionAgent = false
        var result = _pricingService.CalculateDocumentTypeA(lines, 0, 21, 4, isIIBBPerceptionAgent: false);

        // Assert - No IIBB should be calculated
        result.IIBBAmount.Should().Be(0m);
        result.Total.Should().Be(1210m); // Only subtotal + VAT
    }

    [Fact]
    public void IIBB_Calculates_WhenCompanyIsPerceptionAgent()
    {
        // Arrange - Company IS an IIBB perception agent
        var lines = new[]
        {
            new LineCalculationResult { Subtotal = 1000m, VATPercent = 21m }
        };

        // Act - isIIBBPerceptionAgent = true, customer IIBB rate = 4%
        var result = _pricingService.CalculateDocumentTypeA(lines, 0, 21, 4, isIIBBPerceptionAgent: true);

        // Assert - IIBB should be calculated on (subtotal + VAT)
        // IIBB base = 1210, IIBB = 48.40
        result.IIBBPercent.Should().Be(4m);
        result.IIBBAmount.Should().Be(48.40m);
        result.Total.Should().Be(1258.40m);
    }

    [Fact]
    public void IIBB_UsesCustomerRate_FromPadron()
    {
        // Arrange - Customer has 3% IIBB rate from ARBA padrón
        var lines = new[]
        {
            new LineCalculationResult { Subtotal = 1000m, VATPercent = 21m }
        };

        // Act
        var result = _pricingService.CalculateDocumentTypeA(lines, 0, 21, 3, isIIBBPerceptionAgent: true);

        // Assert
        result.IIBBPercent.Should().Be(3m);
        result.IIBBAmount.Should().Be(36.30m); // (1000 + 210) * 0.03
    }

    [Fact]
    public void IIBB_CustomerWithZeroRate_NoPerception()
    {
        // Arrange - Customer has 0% IIBB (exento in padrón)
        var lines = new[]
        {
            new LineCalculationResult { Subtotal = 1000m, VATPercent = 21m }
        };

        // Act
        var result = _pricingService.CalculateDocumentTypeA(lines, 0, 21, 0, isIIBBPerceptionAgent: true);

        // Assert
        result.IIBBAmount.Should().Be(0m);
        result.Total.Should().Be(1210m);
    }

    // ===========================================
    // COMBINED SCENARIOS
    // ===========================================

    [Fact]
    public void InvoiceA_WithAllDiscountsAndIIBB_CalculatesCorrectly()
    {
        // Arrange - Complex scenario from real invoice
        // Line: 1000 with 10% line discount = 900
        // Document discount: 5%
        // VAT: 21%
        // IIBB: 4%
        var lines = new[]
        {
            new LineCalculationResult 
            { 
                Subtotal = 900m, // Already has 10% line discount applied
                VATPercent = 21m 
            }
        };

        // Act
        var result = _pricingService.CalculateDocumentTypeA(lines, 5, 21, 4, isIIBBPerceptionAgent: true);

        // Assert
        // Lines: 900
        // Doc discount: 45 -> Net: 855
        // VAT: 179.55
        // IIBB base: 1034.55, IIBB: 41.38
        // Total: 855 + 179.55 + 41.38 = 1075.93
        result.NetSubtotal.Should().Be(855m);
        result.VATAmount.Should().Be(179.55m);
        result.IIBBAmount.Should().Be(41.38m);
        result.Total.Should().Be(1075.93m);
    }
}
