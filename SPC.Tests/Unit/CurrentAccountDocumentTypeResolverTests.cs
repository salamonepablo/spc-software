using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.API.Services.CurrentAccount;
using SPC.Shared.Models;

namespace SPC.Tests.Unit;

public class CurrentAccountDocumentTypeResolverTests
{
    [Fact]
    public async Task ResolveAsync_ReturnsSI_OnlyForTrustedInitialBalance()
    {
        using var db = CreateDbContext();
        var resolver = new DocumentTypeResolver(db);

        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.Other,
            DocumentNumber = 0,
            Description = "Saldo inicial"
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("SI");
    }

    [Fact]
    public async Task ResolveAsync_DoesNotInferSI_ForNonTrustedMovement()
    {
        using var db = CreateDbContext();
        var resolver = new DocumentTypeResolver(db);

        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.Invoice,
            DocumentNumber = 123,
            Description = "Saldo inicial ajustado por factura"
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().NotBe("SI");
    }

    [Fact]
    public async Task ResolveAsync_PrioritizesMapping_OverDescriptionInference()
    {
        using var db = CreateDbContext();
        var resolver = new DocumentTypeResolver(db);

        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.InvoiceA,
            DocumentNumber = 2001,
            Description = "Pago recibido"
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("FA");
    }

    [Fact]
    public async Task ResolveAsync_ResolvesByPrecedence_WhenDescriptionContainsMultipleTypes()
    {
        // With the new precedence logic, "Factura con nota de crédito" should resolve to FA
        // because Factura has higher precedence than Nota de Crédito
        using var db = CreateDbContext();
        var resolver = new DocumentTypeResolver(db);

        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.Other,
            DocumentNumber = 3001,
            Description = "Factura con nota de crédito"
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("FA", "factura has higher precedence than nota de crédito");
    }

    [Fact]
    public async Task ResolveAsync_ReturnsOT_ForUnknownInput()
    {
        using var db = CreateDbContext();
        var resolver = new DocumentTypeResolver(db);

        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.Other,
            DocumentNumber = 9999,
            Description = "movimiento legacy sin clasificar"
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("OT");
    }

    [Fact]
    public async Task ResolveAsync_UsesDeterministicKnownTypeFallback_WhenCatalogShortCodeIsMissing()
    {
        using var db = CreateDbContext();
        var faRow = await db.DocumentTypes.SingleAsync(d => d.ShortCode == "FA");
        db.DocumentTypes.Remove(faRow);
        await db.SaveChangesAsync();

        var resolver = new DocumentTypeResolver(db);
        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.InvoiceA,
            DocumentNumber = 1450,
            Description = "Factura A"
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("FA");
        resolved.Label.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ResolveAsync_UsesDeterministicKnownTypeFallback_WhenCatalogShortCodeIsInactive()
    {
        using var db = CreateDbContext();
        var ncaRow = await db.DocumentTypes.SingleAsync(d => d.ShortCode == "NCA");
        ncaRow.IsActive = false;
        await db.SaveChangesAsync();

        var resolver = new DocumentTypeResolver(db);
        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.CreditNoteA,
            DocumentNumber = 501,
            Description = "Nota de crédito A"
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("NCA");
        resolved.ShortCode.Should().NotBe("OT");
    }

    private static SPCDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SPCDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new SPCDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
