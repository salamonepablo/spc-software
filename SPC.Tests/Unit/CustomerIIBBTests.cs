using FluentAssertions;
using SPC.Shared.Models;

namespace SPC.Tests.Unit;

/// <summary>
/// TDD Tests for Customer IIBB rate from AFIP/ARCA padrón.
/// 
/// Business Rules:
/// - Each customer has an IIBB perception rate from the ARBA/AGIP padrón
/// - Rate varies by province and customer
/// - Rate can be 0% (exento), 1.5%, 3%, 4%, etc.
/// - This rate is provided by AFIP/ARCA and stored per customer
/// </summary>
public class CustomerIIBBTests
{
    [Fact]
    public void Customer_HasAlicuotaIIBB_Field()
    {
        // Arrange & Act
        var cliente = new Customer
        {
            CompanyName = "Test Customer",
            IIBBPercent = 3.5m
        };

        // Assert
        cliente.IIBBPercent.Should().Be(3.5m);
    }

    [Fact]
    public void Customer_AlicuotaIIBB_DefaultsToZero()
    {
        // Arrange & Act
        var cliente = new Customer
        {
            CompanyName = "Test Customer"
        };

        // Assert - Default is 0 (no perception)
        cliente.IIBBPercent.Should().Be(0m);
    }

    [Fact]
    public void Customer_AlicuotaIIBB_CanBeSetFromPadron()
    {
        // Arrange - Simulate loading from ARBA padrón
        var cliente = new Customer
        {
            CompanyName = "DAVID ALFONSO ALVAREZ",
            CUIT = "20-08345589-7",
            IIBBPercent = 4m  // From ARBA padrón
        };

        // Assert
        cliente.IIBBPercent.Should().Be(4m);
    }

    [Fact]
    public void Customer_AlicuotaIIBB_ExentoIsZero()
    {
        // Arrange - Customer is exento in padrón
        var cliente = new Customer
        {
            CompanyName = "Customer Exento IIBB",
            IIBBPercent = 0m
        };

        // Assert
        cliente.IIBBPercent.Should().Be(0m);
    }

    [Fact]
    public void Customer_HasProvinciaPadron_ForIIBB()
    {
        // Arrange - Province determines which padrón applies
        var cliente = new Customer
        {
            CompanyName = "Test",
            Province = "Buenos Aires",
            IIBBPercent = 3m,
            IIBBRegistryProvince = "BA"  // ARBA
        };

        // Assert
        cliente.IIBBRegistryProvince.Should().Be("BA");
    }
}
