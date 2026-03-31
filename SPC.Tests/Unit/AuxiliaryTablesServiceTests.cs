using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.API.Contracts.AuxiliaryTables;
using SPC.API.Services;
using SPC.Shared.Models;

namespace SPC.Tests.Unit;

/// <summary>
/// Unit tests for AuxiliaryTablesService.
/// Uses InMemory database to verify query logic in isolation.
/// </summary>
public class AuxiliaryTablesServiceTests : IDisposable
{
    private readonly SPCDbContext _db;
    private readonly IAuxiliaryTablesService _service;

    public AuxiliaryTablesServiceTests()
    {
        var options = new DbContextOptionsBuilder<SPCDbContext>()
            .UseInMemoryDatabase($"AuxTablesTest_{Guid.NewGuid()}")
            .Options;

        _db = new SPCDbContext(options);
        _service = new AuxiliaryTablesService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // ===========================================
    // Tax Conditions
    // ===========================================

    [Fact]
    public async Task GetTaxConditionsAsync_ReturnsAllTaxConditions()
    {
        // Arrange
        _db.TaxConditions.AddRange(
            new TaxCondition { Id = 1, Code = "RI", Description = "Responsable Inscripto", InvoiceType = "A" },
            new TaxCondition { Id = 2, Code = "MO", Description = "Monotributo", InvoiceType = "B" }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetTaxConditionsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(tc => tc.Code == "RI");
        result.Should().Contain(tc => tc.Code == "MO");
    }

    // ===========================================
    // Sales Reps
    // ===========================================

    [Fact]
    public async Task GetSalesRepsAsync_ReturnsOnlyActive_OrderedByName()
    {
        // Arrange
        _db.SalesReps.AddRange(
            new SalesRep { Id = 1, EmployeeCode = "V001", FirstName = "Zara", IsActive = true },
            new SalesRep { Id = 2, EmployeeCode = "V002", FirstName = "Ana", IsActive = true },
            new SalesRep { Id = 3, EmployeeCode = "V003", FirstName = "Inactive", IsActive = false }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetSalesRepsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.First().FirstName.Should().Be("Ana");
        result.Last().FirstName.Should().Be("Zara");
    }

    [Fact]
    public async Task GetSalesRepsAsync_ReturnsEmpty_WhenNoActiveReps()
    {
        // Arrange
        _db.SalesReps.Add(new SalesRep { Id = 1, EmployeeCode = "V001", FirstName = "Inactive", IsActive = false });
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetSalesRepsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateSalesRepAsync_SetsActiveTrue_AndPersists()
    {
        // Arrange
        var salesRep = new SalesRep
        {
            EmployeeCode = "V010",
            FirstName = "Juan",
            LastName = "Perez",
            CommissionPercent = 5.0m
        };

        // Act
        var result = await _service.CreateSalesRepAsync(salesRep);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.IsActive.Should().BeTrue();
        result.EmployeeCode.Should().Be("V010");

        // Verify persisted
        var persisted = await _db.SalesReps.FindAsync(result.Id);
        persisted.Should().NotBeNull();
        persisted!.FirstName.Should().Be("Juan");
    }

    // ===========================================
    // Sales Zones
    // ===========================================

    [Fact]
    public async Task GetSalesZonesAsync_ReturnsOnlyActive_OrderedByName()
    {
        // Arrange
        _db.SalesZones.AddRange(
            new SalesZone { Id = 1, Name = "Zona Sur", IsActive = true },
            new SalesZone { Id = 2, Name = "Zona Norte", IsActive = true },
            new SalesZone { Id = 3, Name = "Zona Desactivada", IsActive = false }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetSalesZonesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.First().Name.Should().Be("Zona Norte");
        result.Last().Name.Should().Be("Zona Sur");
    }

    // ===========================================
    // Categories
    // ===========================================

    [Fact]
    public async Task GetCategoriesAsync_ReturnsOnlyActive_OrderedByName()
    {
        // Arrange
        _db.Categories.AddRange(
            new Category { Id = 1, Name = "Baterias Camion", IsActive = true },
            new Category { Id = 2, Name = "Accesorios", IsActive = true },
            new Category { Id = 3, Name = "Obsoleta", IsActive = false }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetCategoriesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.First().Name.Should().Be("Accesorios");
        result.Last().Name.Should().Be("Baterias Camion");
    }

    // ===========================================
    // Warehouses
    // ===========================================

    [Fact]
    public async Task GetWarehousesAsync_ReturnsOnlyActive_OrderedByName()
    {
        // Arrange
        _db.Warehouses.AddRange(
            new Warehouse { Id = 1, Name = "Deposito Principal", IsActive = true },
            new Warehouse { Id = 2, Name = "Camioneta V1", IsActive = true },
            new Warehouse { Id = 3, Name = "Cerrado", IsActive = false }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetWarehousesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.First().Name.Should().Be("Camioneta V1");
        result.Last().Name.Should().Be("Deposito Principal");
    }

    // ===========================================
    // Branches (moved from SucursalesEndpoints direct DbContext)
    // ===========================================

    [Fact]
    public async Task GetBranchesAsync_ReturnsOnlyActive()
    {
        // Arrange
        _db.Branches.AddRange(
            new Branch { Id = 1, Code = "CALLE", Name = "Calle", PointOfSale = 2, IsActive = true },
            new Branch { Id = 2, Code = "DIST", Name = "Distribuidora", PointOfSale = 5, IsActive = true },
            new Branch { Id = 3, Code = "CLOSED", Name = "Cerrada", PointOfSale = 9, IsActive = false }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetBranchesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(b => b.Code == "CALLE");
        result.Should().Contain(b => b.Code == "DIST");
        result.Should().NotContain(b => b.Code == "CLOSED");
    }
}

public class SalesRepResponseTests
{
    [Fact]
    public void From_TrimsMissingNameParts()
    {
        // Arrange
        var onlyFirstName = new SalesRep { Id = 1, EmployeeCode = "V001", FirstName = "Ana", LastName = null };
        var onlyLastName = new SalesRep { Id = 2, EmployeeCode = "V002", FirstName = "", LastName = "Perez" };

        // Act
        var firstNameResponse = SalesRepResponse.From(onlyFirstName);
        var lastNameResponse = SalesRepResponse.From(onlyLastName);

        // Assert
        firstNameResponse.Name.Should().Be("Ana");
        lastNameResponse.Name.Should().Be("Perez");
    }
}
