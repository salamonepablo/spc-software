using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SPC.API.Data;
using SPC.API.Services.CurrentAccount;
using Microsoft.Extensions.Logging.Abstractions;

namespace SPC.Tests.Integration;

public class DocumentTypeMigrationValidationTests
{
    [Fact]
    public async Task DocumentTypeMasterCatalogUpgrade_MigratesLegacyRows_AndRemainsConsistent()
    {
        var databaseName = $"SPC_DocTypeMigration_{Guid.NewGuid():N}";
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

        try
        {
            await using var arrangeContext = CreateSqlServerContext(connectionString);
            await arrangeContext.Database.EnsureDeletedAsync();

            var migrator = arrangeContext.Database.GetService<IMigrator>();
            await migrator.MigrateAsync("20260320000032_SplitServicesCQRSLite");

            await arrangeContext.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID('dbo.DocumentTypes', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.DocumentTypes
                    (
                        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        Code NVARCHAR(10) NOT NULL,
                        Description NVARCHAR(100) NOT NULL,
                        BalanceImpact INT NOT NULL,
                        IsBillingLine BIT NOT NULL,
                        IsActive BIT NOT NULL
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM DocumentTypes WHERE Code = 'NC')
                    INSERT INTO DocumentTypes (Code, Description, BalanceImpact, IsBillingLine, IsActive)
                    VALUES ('NC', 'Nota de Crédito', -1, 1, 1);

                IF NOT EXISTS (SELECT 1 FROM DocumentTypes WHERE Code = 'ND')
                    INSERT INTO DocumentTypes (Code, Description, BalanceImpact, IsBillingLine, IsActive)
                    VALUES ('ND', 'Nota de Débito', 1, 1, 1);

                IF NOT EXISTS (SELECT 1 FROM DocumentTypes WHERE Code = 'RE')
                    INSERT INTO DocumentTypes (Code, Description, BalanceImpact, IsBillingLine, IsActive)
                    VALUES ('RE', 'Recibo', -1, 1, 1);

                IF NOT EXISTS (SELECT 1 FROM DocumentTypes WHERE Code = 'NDI')
                    INSERT INTO DocumentTypes (Code, Description, BalanceImpact, IsBillingLine, IsActive)
                    VALUES ('NDI', 'Nota de Débito Interna', 1, 0, 1);

                IF NOT EXISTS (SELECT 1 FROM DocumentTypes WHERE Code = 'XX')
                    INSERT INTO DocumentTypes (Code, Description, BalanceImpact, IsBillingLine, IsActive)
                    VALUES ('XX', 'Legacy Unknown', 1, 1, 1);
                """);

            await migrator.MigrateAsync("20260329235000_DocumentTypeMasterCatalogUpgrade");
            await migrator.MigrateAsync("20260329235000_DocumentTypeMasterCatalogUpgrade");

            await using var assertContext = CreateSqlServerContext(connectionString);
            var requiredCodes = new[] { "FA", "FB", "NCA", "NCB", "NDA", "NDB", "PR", "PG", "SI", "OT" };

            var catalog = await assertContext.DocumentTypes.AsNoTracking().ToListAsync();

            catalog.Select(x => x.ShortCode).Should().Contain(requiredCodes);
            catalog.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.LabelEs));
            catalog.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.LabelEn));
            catalog.Select(x => x.ShortCode).Should().OnlyHaveUniqueItems();

            catalog.Should().Contain(x => x.Code == "NCA" && x.ShortCode == "NCA");
            catalog.Should().Contain(x => x.Code == "NDA" && x.ShortCode == "NDA");
            catalog.Should().Contain(x => x.Code == "PG" && x.ShortCode == "PG");
            catalog.Should().Contain(x => x.Code == "NDB" && x.ShortCode == "NDB");
            catalog.Should().Contain(x => x.Code == "OT" && x.ShortCode == "OT");
        }
        finally
        {
            await using var cleanupContext = CreateSqlServerContext(connectionString);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task DocumentTypeCatalogVerifier_PassesAfterMigration_AndFailsWhenRequiredCodeInactive()
    {
        var databaseName = $"SPC_DocTypeVerifier_{Guid.NewGuid():N}";
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

        try
        {
            await using var arrangeContext = CreateSqlServerContext(connectionString);
            await arrangeContext.Database.EnsureDeletedAsync();
            await arrangeContext.Database.MigrateAsync();

            var verifier = new DocumentTypeCatalogVerifier(arrangeContext, NullLogger<DocumentTypeCatalogVerifier>.Instance);
            await verifier.Awaiting(v => v.ValidateRequiredShortCodesAsync())
                .Should().NotThrowAsync();

            var ot = await arrangeContext.DocumentTypes.SingleAsync(x => x.ShortCode == "OT");
            ot.IsActive = false;
            await arrangeContext.SaveChangesAsync();

            await verifier.Awaiting(v => v.ValidateRequiredShortCodesAsync())
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*OT*");
        }
        finally
        {
            await using var cleanupContext = CreateSqlServerContext(connectionString);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    private static SPCDbContext CreateSqlServerContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<SPCDbContext>()
            .UseSqlServer(connectionString)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new SPCDbContext(options);
    }
}
