using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using SPC.API.Migrations;
using SPC.Migration;

namespace SPC.Tests.Unit;

public class DeliveryNoteStatusSemanticsTests
{
    [Theory]
    [InlineData("Facturado", true, false)]
    [InlineData("Anulado", false, true)]
    public void LegacyStatusMapsToCurrentDeliveryNoteSemantics(string legacyStatus, bool isInvoiced, bool isVoided)
    {
        var result = legacyStatus == "Facturado"
            ? DeliveryNoteImportMapper.MapStatus("S", null)
            : DeliveryNoteImportMapper.MapStatus(null, "S");

        Assert.Equal(isInvoiced, result.IsInvoiced);
        Assert.Equal(isVoided, result.IsVoided);
    }

    [Fact]
    public void CorrectiveMigrationSwapsColumnNamesThroughTemporaryName()
    {
        var migration = new CorrectDeliveryNoteStatusColumnNames();
        var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        typeof(Microsoft.EntityFrameworkCore.Migrations.Migration).GetMethod("Up", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(migration, [builder]);

        var renames = builder.Operations.OfType<RenameColumnOperation>()
            .Where(operation => operation.Table == "DeliveryNotes")
            .Select(operation => (operation.Name, operation.NewName))
            .ToArray();

        Assert.Equal(new[]
        {
            ("IsVoided", "__DeliveryNoteStatusSwap"),
            ("IsInvoiced", "IsVoided"),
            ("__DeliveryNoteStatusSwap", "IsInvoiced")
        }, renames);
    }
}
