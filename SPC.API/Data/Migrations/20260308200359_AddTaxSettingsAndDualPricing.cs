using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SPC.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxSettingsAndDualPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PrecioFactura",
                table: "Productos",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioPresupuesto",
                table: "Productos",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "TaxSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaxCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "TaxSettings",
                columns: new[] { "Id", "Description", "EffectiveFrom", "EffectiveTo", "IsActive", "IsDefault", "Rate", "TaxCode" },
                values: new object[,]
                {
                    { 1, "IVA General", new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, true, 21.00m, "VAT" },
                    { 2, "IVA Reducido", new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, 10.50m, "VAT_REDUCED" },
                    { 3, "IVA Exento", new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, 0.00m, "VAT_EXEMPT" },
                    { 4, "IIBB Buenos Aires", new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, 3.00m, "IIBB_BA" },
                    { 5, "IIBB CABA", new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, 3.00m, "IIBB_CABA" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaxSettings_TaxCode_EffectiveFrom",
                table: "TaxSettings",
                columns: new[] { "TaxCode", "EffectiveFrom" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxSettings");

            migrationBuilder.DropColumn(
                name: "PrecioFactura",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "PrecioPresupuesto",
                table: "Productos");
        }
    }
}
