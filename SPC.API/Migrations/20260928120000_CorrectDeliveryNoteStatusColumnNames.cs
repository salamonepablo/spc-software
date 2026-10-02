using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPC.API.Migrations;

public partial class CorrectDeliveryNoteStatusColumnNames : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "IsVoided",
            table: "DeliveryNotes",
            newName: "__DeliveryNoteStatusSwap");
        migrationBuilder.RenameColumn(
            name: "IsInvoiced",
            table: "DeliveryNotes",
            newName: "IsVoided");
        migrationBuilder.RenameColumn(
            name: "__DeliveryNoteStatusSwap",
            table: "DeliveryNotes",
            newName: "IsInvoiced");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "IsInvoiced",
            table: "DeliveryNotes",
            newName: "__DeliveryNoteStatusSwap");
        migrationBuilder.RenameColumn(
            name: "IsVoided",
            table: "DeliveryNotes",
            newName: "IsInvoiced");
        migrationBuilder.RenameColumn(
            name: "__DeliveryNoteStatusSwap",
            table: "DeliveryNotes",
            newName: "IsVoided");
    }
}
