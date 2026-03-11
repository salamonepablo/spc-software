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
            RazonSocial = "Test Customer",
            AlicuotaIIBB = 3.5m
        };

        // Assert
        cliente.AlicuotaIIBB.Should().Be(3.5m);
    }

    [Fact]
    public void Customer_AlicuotaIIBB_DefaultsToZero()
    {
        // Arrange & Act
        var cliente = new Customer
        {
            RazonSocial = "Test Customer"
        };

        // Assert - Default is 0 (no perception)
        cliente.AlicuotaIIBB.Should().Be(0m);
    }

    [Fact]
    public void Customer_AlicuotaIIBB_CanBeSetFromPadron()
    {
        // Arrange - Simulate loading from ARBA padrón
        var cliente = new Customer
        {
            RazonSocial = "DAVID ALFONSO ALVAREZ",
            CUIT = "20-08345589-7",
            AlicuotaIIBB = 4m  // From ARBA padrón
        };

        // Assert
        cliente.AlicuotaIIBB.Should().Be(4m);
    }

    [Fact]
    public void Customer_AlicuotaIIBB_ExentoIsZero()
    {
        // Arrange - Customer is exento in padrón
        var cliente = new Customer
        {
            RazonSocial = "Customer Exento IIBB",
            AlicuotaIIBB = 0m
        };

        // Assert
        cliente.AlicuotaIIBB.Should().Be(0m);
    }

    [Fact]
    public void Customer_HasProvinciaPadron_ForIIBB()
    {
        // Arrange - Province determines which padrón applies
        var cliente = new Customer
        {
            RazonSocial = "Test",
            Provincia = "Buenos Aires",
            AlicuotaIIBB = 3m,
            ProvinciaPadronIIBB = "BA"  // ARBA
        };

        // Assert
        cliente.ProvinciaPadronIIBB.Should().Be("BA");
    }
}
