using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SPC.API.Data;

#nullable disable

namespace SPC.API.Migrations;

[DbContext(typeof(SPCDbContext))]
[Migration("20260329235000_DocumentTypeMasterCatalogUpgrade")]
public partial class DocumentTypeMasterCatalogUpgrade : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID('dbo.DocumentTypes', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.DocumentTypes
                (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Code NVARCHAR(10) NOT NULL,
                    Description NVARCHAR(100) NOT NULL,
                    BalanceImpact INT NOT NULL CONSTRAINT DF_DocumentTypes_BalanceImpact DEFAULT(0),
                    IsBillingLine BIT NOT NULL CONSTRAINT DF_DocumentTypes_IsBillingLine DEFAULT(1),
                    IsActive BIT NOT NULL CONSTRAINT DF_DocumentTypes_IsActive DEFAULT(1),
                    ShortCode NVARCHAR(10) NOT NULL CONSTRAINT DF_DocumentTypes_ShortCode DEFAULT(''),
                    LabelEs NVARCHAR(120) NOT NULL CONSTRAINT DF_DocumentTypes_LabelEs DEFAULT(''),
                    LabelEn NVARCHAR(120) NOT NULL CONSTRAINT DF_DocumentTypes_LabelEn DEFAULT('')
                );
            END
            ELSE
            BEGIN
                IF COL_LENGTH('dbo.DocumentTypes', 'ShortCode') IS NULL
                    ALTER TABLE dbo.DocumentTypes ADD ShortCode NVARCHAR(10) NOT NULL CONSTRAINT DF_DocumentTypes_ShortCode DEFAULT('');

                IF COL_LENGTH('dbo.DocumentTypes', 'LabelEs') IS NULL
                    ALTER TABLE dbo.DocumentTypes ADD LabelEs NVARCHAR(120) NOT NULL CONSTRAINT DF_DocumentTypes_LabelEs DEFAULT('');

                IF COL_LENGTH('dbo.DocumentTypes', 'LabelEn') IS NULL
                    ALTER TABLE dbo.DocumentTypes ADD LabelEn NVARCHAR(120) NOT NULL CONSTRAINT DF_DocumentTypes_LabelEn DEFAULT('');
            END
            """);

        migrationBuilder.Sql("""
            UPDATE DocumentTypes
            SET ShortCode = CASE
                WHEN Code = 'NC' THEN 'NCA'
                WHEN Code = 'ND' THEN 'NDA'
                WHEN Code = 'RE' THEN 'PG'
                WHEN Code = 'NDI' THEN 'NDB'
                WHEN Code IS NULL OR LTRIM(RTRIM(Code)) = '' THEN 'OT'
                ELSE Code
            END
            WHERE ShortCode = '' OR ShortCode IS NULL;
            """);

        migrationBuilder.Sql("""
            UPDATE DocumentTypes
            SET LabelEs = CASE ShortCode
                WHEN 'FA' THEN 'Factura A'
                WHEN 'FB' THEN 'Factura B'
                WHEN 'NCA' THEN 'Nota de Crédito A'
                WHEN 'NCB' THEN 'Nota de Crédito B'
                WHEN 'NDA' THEN 'Nota de Débito A'
                WHEN 'NDB' THEN 'Nota de Débito B'
                WHEN 'PR' THEN 'Presupuesto'
                WHEN 'PG' THEN 'Pago'
                WHEN 'SI' THEN 'Saldo inicial'
                ELSE 'Otros'
            END,
            LabelEn = CASE ShortCode
                WHEN 'FA' THEN 'Invoice A'
                WHEN 'FB' THEN 'Invoice B'
                WHEN 'NCA' THEN 'Credit Note A'
                WHEN 'NCB' THEN 'Credit Note B'
                WHEN 'NDA' THEN 'Debit Note A'
                WHEN 'NDB' THEN 'Debit Note B'
                WHEN 'PR' THEN 'Quote'
                WHEN 'PG' THEN 'Payment'
                WHEN 'SI' THEN 'Initial balance'
                ELSE 'Other'
            END;
            """);

        migrationBuilder.Sql("""
            MERGE DocumentTypes AS target
            USING (VALUES
                ('FA','FA','Factura A','Factura A','Invoice A',1,1,1),
                ('FB','FB','Factura B','Factura B','Invoice B',1,1,1),
                ('NCA','NCA','Nota de Crédito A','Nota de Crédito A','Credit Note A',-1,1,1),
                ('NCB','NCB','Nota de Crédito B','Nota de Crédito B','Credit Note B',-1,1,1),
                ('NDA','NDA','Nota de Débito A','Nota de Débito A','Debit Note A',1,1,1),
                ('NDB','NDB','Nota de Débito B','Nota de Débito B','Debit Note B',1,1,1),
                ('PR','PR','Presupuesto','Presupuesto','Quote',1,0,1),
                ('PG','PG','Pago','Pago','Payment',-1,1,1),
                ('SI','SI','Saldo Inicial','Saldo inicial','Initial balance',1,1,1),
                ('OT','OT','Otros','Otros','Other',0,1,1)
            ) AS source (Code, ShortCode, Description, LabelEs, LabelEn, BalanceImpact, IsBillingLine, IsActive)
            ON target.ShortCode = source.ShortCode
            WHEN MATCHED THEN UPDATE SET
                target.Code = source.Code,
                target.Description = source.Description,
                target.LabelEs = source.LabelEs,
                target.LabelEn = source.LabelEn,
                target.BalanceImpact = source.BalanceImpact,
                target.IsBillingLine = source.IsBillingLine,
                target.IsActive = source.IsActive
            WHEN NOT MATCHED THEN INSERT
                (Code, ShortCode, Description, LabelEs, LabelEn, BalanceImpact, IsBillingLine, IsActive)
                VALUES (source.Code, source.ShortCode, source.Description, source.LabelEs, source.LabelEn, source.BalanceImpact, source.IsBillingLine, source.IsActive);
            """);

        migrationBuilder.Sql("""
            UPDATE DocumentTypes
            SET ShortCode = 'OT',
                Code = 'OT',
                Description = 'Otros',
                LabelEs = 'Otros',
                LabelEn = 'Other',
                BalanceImpact = 0
            WHERE ShortCode NOT IN ('FA','FB','NCA','NCB','NDA','NDB','PR','PG','SI','OT');
            """);

        migrationBuilder.Sql("""
            ;WITH DedupByCode AS
            (
                SELECT Id, ROW_NUMBER() OVER (PARTITION BY Code ORDER BY Id) AS RowNum
                FROM dbo.DocumentTypes
            )
            DELETE FROM DedupByCode WHERE RowNum > 1;

            ;WITH DedupByShortCode AS
            (
                SELECT Id, ROW_NUMBER() OVER (PARTITION BY ShortCode ORDER BY Id) AS RowNum
                FROM dbo.DocumentTypes
            )
            DELETE FROM DedupByShortCode WHERE RowNum > 1;
            """);

        migrationBuilder.Sql("""
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentTypes_Code' AND object_id = OBJECT_ID('dbo.DocumentTypes'))
                CREATE UNIQUE INDEX IX_DocumentTypes_Code ON dbo.DocumentTypes(Code);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentTypes_ShortCode' AND object_id = OBJECT_ID('dbo.DocumentTypes'))
                CREATE UNIQUE INDEX IX_DocumentTypes_ShortCode ON dbo.DocumentTypes(ShortCode);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID('dbo.DocumentTypes', 'U') IS NOT NULL
            BEGIN
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentTypes_ShortCode' AND object_id = OBJECT_ID('dbo.DocumentTypes'))
                    DROP INDEX IX_DocumentTypes_ShortCode ON dbo.DocumentTypes;

                IF COL_LENGTH('dbo.DocumentTypes', 'LabelEn') IS NOT NULL
                    ALTER TABLE dbo.DocumentTypes DROP COLUMN LabelEn;

                IF COL_LENGTH('dbo.DocumentTypes', 'LabelEs') IS NOT NULL
                    ALTER TABLE dbo.DocumentTypes DROP COLUMN LabelEs;

                IF COL_LENGTH('dbo.DocumentTypes', 'ShortCode') IS NOT NULL
                    ALTER TABLE dbo.DocumentTypes DROP COLUMN ShortCode;
            END
            """);
    }
}
