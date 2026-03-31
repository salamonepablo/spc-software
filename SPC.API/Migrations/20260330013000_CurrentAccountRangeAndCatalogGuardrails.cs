using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SPC.API.Data;

#nullable disable

namespace SPC.API.Migrations;

[DbContext(typeof(SPCDbContext))]
[Migration("20260330013000_CurrentAccountRangeAndCatalogGuardrails")]
public partial class CurrentAccountRangeAndCatalogGuardrails : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID('dbo.CurrentAccountMovements', 'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_CurrentAccountMovements_CustomerId_MovementDate_Id'
                      AND object_id = OBJECT_ID('dbo.CurrentAccountMovements'))
                BEGIN
                    CREATE INDEX IX_CurrentAccountMovements_CustomerId_MovementDate_Id
                    ON dbo.CurrentAccountMovements(CustomerId, MovementDate, Id);
                END
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID('dbo.CurrentAccountMovements', 'U') IS NOT NULL
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_CurrentAccountMovements_CustomerId_MovementDate_Id'
                      AND object_id = OBJECT_ID('dbo.CurrentAccountMovements'))
                BEGIN
                    DROP INDEX IX_CurrentAccountMovements_CustomerId_MovementDate_Id ON dbo.CurrentAccountMovements;
                END
            END
            """);
    }
}
