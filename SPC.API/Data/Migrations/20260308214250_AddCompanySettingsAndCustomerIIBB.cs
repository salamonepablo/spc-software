using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPC.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanySettingsAndCustomerIIBB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AlicuotaIIBB",
                table: "Clientes",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ProvinciaPadronIIBB",
                table: "Clientes",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CUIT = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    IsIVAWithholdingAgent = table.Column<bool>(type: "bit", nullable: false),
                    IsIIBBPerceptionAgent = table.Column<bool>(type: "bit", nullable: false),
                    IIBBProvince = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IIBBRegistrationNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FiscalActivityStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "AlicuotaIIBB",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "ProvinciaPadronIIBB",
                table: "Clientes");
        }
    }
}
