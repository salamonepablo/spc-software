using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services.CurrentAccount;

public class DocumentTypeResolver : IDocumentTypeResolver
{
    private readonly SPCDbContext _db;
    private readonly ILogger<DocumentTypeResolver>? _logger;
    private Dictionary<string, DocumentTypeMaster>? _catalogByShortCode;

    private static readonly Dictionary<string, (string LabelEs, string LabelEn)> CanonicalLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FA"] = ("Factura A", "Invoice A"),
        ["FB"] = ("Factura B", "Invoice B"),
        ["NCA"] = ("Nota de Crédito A", "Credit Note A"),
        ["NCB"] = ("Nota de Crédito B", "Credit Note B"),
        ["NDA"] = ("Nota de Débito A", "Debit Note A"),
        ["NDB"] = ("Nota de Débito B", "Debit Note B"),
        ["PR"] = ("Presupuesto", "Quote"),
        ["PG"] = ("Pago", "Payment"),
        ["SI"] = ("Saldo inicial", "Initial balance"),
        ["OT"] = ("Otros", "Other")
    };

    private static readonly Dictionary<DocumentType, string> DirectMap = new()
    {
        [DocumentType.InvoiceA] = "FA",
        [DocumentType.InvoiceB] = "FB",
        [DocumentType.CreditNoteA] = "NCA",
        [DocumentType.CreditNoteB] = "NCB",
        [DocumentType.DebitNoteA] = "NDA",
        [DocumentType.DebitNoteB] = "NDB",
        [DocumentType.Quote] = "PR",
        [DocumentType.QuoteVoid] = "PR",
        [DocumentType.Payment] = "PG",
        [DocumentType.Receipt] = "PG",
        [DocumentType.PaymentBilling] = "PG",
        [DocumentType.PaymentVoidBilling] = "PG",
        [DocumentType.PaymentBudget] = "PG",
        [DocumentType.PaymentVoidBudget] = "PG"
    };

    private static readonly Dictionary<DocumentType, string> LegacyMap = new()
    {
        [DocumentType.Invoice] = "FA",
        [DocumentType.CreditNote] = "NCA",
        [DocumentType.DebitNote] = "NDA",
        [DocumentType.InternalDebitNote] = "NDB",
        [DocumentType.InternalDebitA] = "NDA",
        [DocumentType.InternalDebitB] = "NDB",
        [DocumentType.Other] = "OT"
    };

    public DocumentTypeResolver(SPCDbContext db, ILogger<DocumentTypeResolver>? logger = null)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DocumentTypeResolution> ResolveAsync(CurrentAccountMovement movement, CancellationToken cancellationToken = default)
    {
        var catalog = await GetCatalogAsync(cancellationToken);

        if (IsTrustedInitialBalance(movement))
        {
            if (catalog.TryGetValue("SI", out var initialBalanceType))
            {
                return ToResolution(initialBalanceType, movement);
            }

            return ToResolution(CreateFallbackType("SI"), movement);
        }

        string? resolvedCode = null;

        if (DirectMap.TryGetValue(movement.DocumentType, out var directCode) &&
            catalog.TryGetValue(directCode, out var directType))
        {
            return ToResolution(directType, movement);
        }
        if (DirectMap.TryGetValue(movement.DocumentType, out directCode))
        {
            resolvedCode = directCode;
        }

        // Try description inference BEFORE applying DocumentType.Other fallback
        // This allows descriptions to override the generic "OT" classification
        var inferredCode = InferFromDescription(movement.Description);
        if (inferredCode is not null && catalog.TryGetValue(inferredCode, out var inferredType))
        {
            return ToResolution(inferredType, movement);
        }

        if (LegacyMap.TryGetValue(movement.DocumentType, out var mappedCode) &&
            catalog.TryGetValue(mappedCode, out var mappedType))
        {
            return ToResolution(mappedType, movement);
        }
        if (resolvedCode is null && LegacyMap.TryGetValue(movement.DocumentType, out mappedCode))
        {
            resolvedCode = mappedCode;
        }

        resolvedCode ??= inferredCode;

        if (!string.IsNullOrWhiteSpace(resolvedCode) && !string.Equals(resolvedCode, "OT", StringComparison.OrdinalIgnoreCase))
        {
            _logger?.LogWarning(
                "Document type catalog missing/inactive short code {ShortCode}; applying deterministic fallback for movement {MovementId} ({LegacyType})",
                resolvedCode,
                movement.Id,
                movement.DocumentType);

            return ToResolution(CreateFallbackType(resolvedCode), movement);
        }

        if (!catalog.TryGetValue("OT", out var otherType))
        {
            otherType = CreateFallbackType("OT");
        }

        return ToResolution(otherType, movement);
    }

    private async Task<Dictionary<string, DocumentTypeMaster>> GetCatalogAsync(CancellationToken cancellationToken)
    {
        if (_catalogByShortCode is not null)
        {
            return _catalogByShortCode;
        }

        _catalogByShortCode = await _db.DocumentTypes
            .Where(d => d.IsActive)
            .ToDictionaryAsync(d => d.ShortCode, StringComparer.OrdinalIgnoreCase, cancellationToken);

        if (!_catalogByShortCode.ContainsKey("OT"))
        {
            _catalogByShortCode["OT"] = new DocumentTypeMaster
            {
                Code = "OT",
                ShortCode = "OT",
                Description = "Otros",
                LabelEs = "Otros",
                LabelEn = "Other",
                IsActive = true
            };
        }

        return _catalogByShortCode;
    }

    private static bool IsTrustedInitialBalance(CurrentAccountMovement movement)
    {
        return movement.DocumentType == DocumentType.Other
            && movement.DocumentNumber == 0
            && string.Equals(movement.Description?.Trim(), "Saldo inicial", StringComparison.OrdinalIgnoreCase);
    }

    private static string? InferFromDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var text = description.ToLowerInvariant();

        // Precedence order: Factura > Nota de Credito > Nota de Debito > Presupuesto > Pago
        // When multiple types are detected, the one with highest precedence wins

        if (ContainsAny(text, "factura", "invoice", "fact"))
        {
            return InferBySubtype("FA", "FB", text);
        }

        if (ContainsAny(text, "nota de credito", "nota de crédito", "crédito", "credito", "credit note", "nc "))
        {
            return InferBySubtype("NCA", "NCB", text);
        }

        if (ContainsAny(text, "nota de debito", "nota de débito", "débito", "debito", "debit note", "nd "))
        {
            return InferBySubtype("NDA", "NDB", text);
        }

        if (ContainsAny(text, "presupuesto", "quote"))
        {
            return "PR";
        }

        if (ContainsAny(text, "pago", "recibo", "payment", "receipt"))
        {
            return "PG";
        }

        return null;
    }

    private static bool ContainsAny(string text, params string[] tokens)
    {
        return tokens.Any(text.Contains);
    }

    private static string InferBySubtype(string codeA, string codeB, string text)
    {
        if (text.Contains(" tipo a") || text.Contains(" a ") || text.EndsWith(" a"))
        {
            return codeA;
        }

        if (text.Contains(" tipo b") || text.Contains(" b ") || text.EndsWith(" b"))
        {
            return codeB;
        }

        return codeA;
    }

    private static DocumentTypeResolution ToResolution(DocumentTypeMaster type, CurrentAccountMovement movement)
    {
        return new DocumentTypeResolution(
            type.ShortCode,
            type.LabelEs,
            type.LabelEn,
            movement.DocumentType.ToString(),
            (int)movement.DocumentType);
    }

    private static DocumentTypeMaster CreateFallbackType(string shortCode)
    {
        var normalized = shortCode.ToUpperInvariant();
        var labels = CanonicalLabels.TryGetValue(normalized, out var value)
            ? value
            : CanonicalLabels["OT"];

        return new DocumentTypeMaster
        {
            Code = normalized,
            ShortCode = normalized,
            Description = labels.LabelEs,
            LabelEs = labels.LabelEs,
            LabelEn = labels.LabelEn,
            IsActive = true
        };
    }
}
