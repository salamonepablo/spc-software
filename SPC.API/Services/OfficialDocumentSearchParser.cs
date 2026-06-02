using System.Text.RegularExpressions;

namespace SPC.API.Services;

internal static partial class OfficialDocumentSearchParser
{
    public static OfficialDocumentSearch Parse(string term)
    {
        var trimmedTerm = term.Trim();
        var match = OfficialDocumentRegex().Match(trimmedTerm.ToUpperInvariant());
        if (match.Success)
        {
            return new OfficialDocumentSearch(
                NormalizeType(match.Groups["type"].Value),
                int.Parse(match.Groups["point"].Value),
                long.Parse(match.Groups["number"].Value));
        }

        var digits = DigitsOnlyRegex().Replace(trimmedTerm, "");
        if (!string.IsNullOrWhiteSpace(digits) && long.TryParse(digits, out var documentNumber))
        {
            return new OfficialDocumentSearch(null, null, documentNumber);
        }

        return new OfficialDocumentSearch(null, null, null);
    }

    private static string NormalizeType(string type)
    {
        return type[^1].ToString();
    }

    [GeneratedRegex(@"\b(?:FA|FB|NC|ND)?\s*(?<type>[AB])\s+(?<point>\d{4})-(?<number>\d{8})\b", RegexOptions.IgnoreCase)]
    private static partial Regex OfficialDocumentRegex();

    [GeneratedRegex(@"\D")]
    private static partial Regex DigitsOnlyRegex();
}

internal sealed record OfficialDocumentSearch(string? Type, int? PointOfSale, long? Number);
