namespace SPC.API.Contracts.CurrentAccount;

/// <summary>
/// Navigation metadata for Current Account movement targets.
/// Allows UI to render safe open/disabled behaviors consistently.
/// </summary>
public class CurrentAccountNavigationMetadataResponse
{
    /// <summary>
    /// Broad target category (document, payment, initial-balance, other).
    /// </summary>
    public string TargetType { get; set; } = "other";

    /// <summary>
    /// Specific target kind (invoice, quote, credit-note, debit-note, payment, initial-balance, other).
    /// </summary>
    public string TargetKind { get; set; } = "other";

    /// <summary>
    /// Preferred UI route for opening details (or list+search fallback).
    /// </summary>
    public string? TargetRoute { get; set; }

    /// <summary>
    /// Optional target identifier if known (document number fallback when internal ID is unavailable).
    /// </summary>
    public string? TargetId { get; set; }

    /// <summary>
    /// Whether target can be opened from UI.
    /// </summary>
    public bool CanOpen { get; set; }

    /// <summary>
    /// Clear reason when the movement cannot be opened.
    /// </summary>
    public string? DisabledReason { get; set; }
}
