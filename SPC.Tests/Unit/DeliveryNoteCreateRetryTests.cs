using FluentAssertions;

namespace SPC.Tests.Unit;

public sealed class DeliveryNoteCreateRetryTests
{
    [Fact]
    public void SaveReusesOneIdempotencyKeyForTheFormLifetime()
    {
        var sourcePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../SPC.Web/Components/Pages/Remitos/Create.razor"));
        var source = File.ReadAllText(sourcePath);
        var saveMethod = source[source.IndexOf("private async Task Save()", StringComparison.Ordinal)..];

        source.Should().Contain("private readonly string idempotencyKey = Guid.NewGuid().ToString(\"N\");");
        saveMethod.Should().Contain("CreateDeliveryNoteAsync(request, idempotencyKey)");
        saveMethod.Should().NotContain("Guid.NewGuid()");
    }
}
