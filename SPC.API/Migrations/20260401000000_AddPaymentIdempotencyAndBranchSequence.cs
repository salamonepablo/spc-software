using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPC.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentIdempotencyAndBranchSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Payments",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestFingerprint",
                table: "Payments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BranchPaymentSequences",
                columns: table => new
                {
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    NextPaymentNumber = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchPaymentSequences", x => x.BranchId);
                    table.ForeignKey(
                        name: "FK_BranchPaymentSequences_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO dbo.BranchPaymentSequences (BranchId, NextPaymentNumber)
                SELECT b.Id, ISNULL(MAX(p.PaymentNumber), 0) + 1
                FROM dbo.Branches b
                LEFT JOIN dbo.Payments p ON p.BranchId = b.Id
                GROUP BY b.Id;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BranchId_IdempotencyKey",
                table: "Payments",
                columns: new[] { "BranchId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BranchPaymentSequences");

            migrationBuilder.DropIndex(
                name: "IX_Payments_BranchId_IdempotencyKey",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RequestFingerprint",
                table: "Payments");
        }
    }
}
