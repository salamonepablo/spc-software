using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPC.API.Migrations;

public partial class AddDeliveryNoteIdempotencyAndBranchSequence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("IdempotencyKey", "DeliveryNotes", type: "nvarchar(128)", maxLength: 128, nullable: true);
        migrationBuilder.AddColumn<string>("RequestFingerprint", "DeliveryNotes", type: "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.CreateTable("BranchDeliveryNoteSequences", table => new
        {
            BranchId = table.Column<int>(type: "int", nullable: false),
            NextDeliveryNoteNumber = table.Column<long>(type: "bigint", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_BranchDeliveryNoteSequences", x => x.BranchId);
            table.ForeignKey("FK_BranchDeliveryNoteSequences_Branches_BranchId", x => x.BranchId,
                "Branches", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.Sql("INSERT INTO dbo.BranchDeliveryNoteSequences (BranchId, NextDeliveryNoteNumber) SELECT b.Id, ISNULL(MAX(d.DeliveryNoteNumber),0)+1 FROM dbo.Branches b LEFT JOIN dbo.DeliveryNotes d ON d.BranchId=b.Id GROUP BY b.Id");
        migrationBuilder.CreateIndex("IX_DeliveryNotes_BranchId_IdempotencyKey", "DeliveryNotes",
            new[] { "BranchId", "IdempotencyKey" }, unique: true, filter: "[IdempotencyKey] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_DeliveryNotes_BranchId_IdempotencyKey", "DeliveryNotes");
        migrationBuilder.DropTable("BranchDeliveryNoteSequences");
        migrationBuilder.DropColumn("IdempotencyKey", "DeliveryNotes");
        migrationBuilder.DropColumn("RequestFingerprint", "DeliveryNotes");
    }
}
