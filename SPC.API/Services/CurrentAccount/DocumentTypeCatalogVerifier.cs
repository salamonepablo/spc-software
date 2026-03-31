using Microsoft.EntityFrameworkCore;
using SPC.API.Data;

namespace SPC.API.Services.CurrentAccount;

public interface IDocumentTypeCatalogVerifier
{
    Task ValidateRequiredShortCodesAsync(CancellationToken cancellationToken = default);
}

public class DocumentTypeCatalogVerifier : IDocumentTypeCatalogVerifier
{
    private static readonly string[] RequiredCodes = ["FA", "FB", "NCA", "NCB", "NDA", "NDB", "PR", "PG", "SI", "OT"];

    private readonly SPCDbContext _db;
    private readonly ILogger<DocumentTypeCatalogVerifier> _logger;

    public DocumentTypeCatalogVerifier(SPCDbContext db, ILogger<DocumentTypeCatalogVerifier> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ValidateRequiredShortCodesAsync(CancellationToken cancellationToken = default)
    {
        var activeShortCodes = await _db.DocumentTypes
            .Where(x => x.IsActive)
            .Select(x => x.ShortCode)
            .ToListAsync(cancellationToken);

        var missing = RequiredCodes
            .Except(activeShortCodes, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        var missingText = string.Join(", ", missing);
        _logger.LogWarning("Current account document type catalog is missing active short codes: {MissingCodes}", missingText);
        throw new InvalidOperationException($"Missing required active document type short codes: {missingText}");
    }
}
