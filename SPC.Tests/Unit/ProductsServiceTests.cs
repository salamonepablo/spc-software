using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.API.Services;
using SPC.Shared.Models;

namespace SPC.Tests.Unit;

/// <summary>
/// Unit tests for ProductsService search behavior.
/// </summary>
public class ProductsServiceTests : IDisposable
{
    private readonly SPCDbContext _db;
    private readonly IProductsService _service;

    public ProductsServiceTests()
    {
        var options = new DbContextOptionsBuilder<SPCDbContext>()
            .UseInMemoryDatabase($"ProductsServiceTest_{Guid.NewGuid()}")
            .Options;

        _db = new SPCDbContext(options);
        _service = new ProductsService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task SearchAsync_ReturnsMatches_ByCodeOrDescription()
    {
        // Arrange
        _db.Products.AddRange(
            new Product { Id = 1, Code = "BAT-100", Description = "Bateria 60Ah", IsActive = true },
            new Product { Id = 2, Code = "ACC-200", Description = "Cable arranque", IsActive = true },
            new Product { Id = 3, Code = "OFF", Description = "Obsoleto", IsActive = false }
        );
        await _db.SaveChangesAsync();

        // Act
        var byCode = await _service.SearchAsync("ACC-200");
        var byDescription = await _service.SearchAsync("Bateria");

        // Assert
        byCode.Should().ContainSingle(p => p.Id == 2);
        byDescription.Should().ContainSingle(p => p.Id == 1);
        byDescription.Should().NotContain(p => p.Id == 3);
    }

    [Fact]
    public async Task SearchAsync_DoesNotMatch_SupplierCodeOnly()
    {
        // Arrange
        _db.Products.AddRange(
            new Product { Id = 1, Code = "BAT-300", Description = "Bateria 75Ah", SupplierCode = "SUP-999", IsActive = true },
            new Product { Id = 2, Code = "BAT-400", Description = "Bateria 90Ah", SupplierCode = "SUP-123", IsActive = true }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.SearchAsync("SUP-999");

        // Assert
        result.Should().BeEmpty();
    }
}
