using SPC.Shared.Models;

namespace SPC.API.Services.CurrentAccount;

public interface IDocumentTypeResolver
{
    Task<DocumentTypeResolution> ResolveAsync(CurrentAccountMovement movement, CancellationToken cancellationToken = default);
}

public sealed record DocumentTypeResolution(
    string ShortCode,
    string? Label,
    string? Tooltip,
    string LegacyName,
    int LegacyCode
);
