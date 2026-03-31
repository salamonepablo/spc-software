using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SPC.API.Data;

namespace SPC.Tests.Unit;

public class DocumentTypeCatalogValidationTests
{
    private static readonly string[] RequiredShortCodes =
    [
        "FA", "FB", "NCA", "NCB", "NDA", "NDB", "PR", "PG", "SI", "OT"
    ];

    [Fact]
    public void DocumentTypeCatalog_ContainsMinimumRequiredShortCodes()
    {
        using var db = CreateDbContext();

        var shortCodes = db.DocumentTypes
            .Select(d => d.ShortCode)
            .ToList();

        shortCodes.Should().Contain(RequiredShortCodes);
    }

    [Fact]
    public void DocumentTypeCatalog_HasBilingualLabels_ForAllRequiredShortCodes()
    {
        using var db = CreateDbContext();

        var catalog = db.DocumentTypes
            .Where(d => RequiredShortCodes.Contains(d.ShortCode))
            .ToList();

        catalog.Should().HaveCount(RequiredShortCodes.Length);
        catalog.Should().OnlyContain(d => !string.IsNullOrWhiteSpace(d.LabelEs));
        catalog.Should().OnlyContain(d => !string.IsNullOrWhiteSpace(d.LabelEn));
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
