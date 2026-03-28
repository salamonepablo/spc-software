using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.API.Services;
using SPC.Shared.Models;

namespace SPC.Tests.Unit;

/// <summary>
/// Unit tests for CustomersService search behavior.
/// </summary>
public class CustomersServiceTests : IDisposable
{
    private readonly SPCDbContext _db;
    private readonly ICustomersService _service;

    public CustomersServiceTests()
    {
        var options = new DbContextOptionsBuilder<SPCDbContext>()
            .UseInMemoryDatabase($"CustomersServiceTest_{Guid.NewGuid()}")
            .Options;

        _db = new SPCDbContext(options);
        _service = new CustomersService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task SearchAsync_ReturnsMatches_ByIdOrName()
    {
        // Arrange
        _db.Customers.AddRange(
            new Customer { Id = 101, CompanyName = "Baterias Norte SRL", TradeName = "Baterias Norte", IsActive = true },
            new Customer { Id = 202, CompanyName = "Accesorios Sur SA", TradeName = "Sur Acc", IsActive = true },
            new Customer { Id = 303, CompanyName = "Inactive SA", TradeName = "Inactivo", IsActive = false }
        );
        await _db.SaveChangesAsync();

        // Act
        var byId = await _service.SearchAsync("101");
        var byCompanyName = await _service.SearchAsync("Accesorios");
        var byTradeName = await _service.SearchAsync("Baterias Norte");

        // Assert
        byId.Should().ContainSingle(c => c.Id == 101);
        byCompanyName.Should().ContainSingle(c => c.Id == 202);
        byTradeName.Should().ContainSingle(c => c.Id == 101);
        byId.Should().NotContain(c => c.Id == 303);
    }

    [Fact]
    public async Task SearchAsync_DoesNotMatch_UnrelatedFields()
    {
        // Arrange
        _db.Customers.AddRange(
            new Customer { Id = 1, CompanyName = "Alpha SRL", CUIT = "30-99999999-9", IsActive = true },
            new Customer { Id = 2, CompanyName = "Beta SA", Notes = "VIP-123", IsActive = true }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.SearchAsync("VIP-123");

        // Assert
        result.Should().BeEmpty();
    }
}
