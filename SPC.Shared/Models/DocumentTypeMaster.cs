using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Master table for document types in current account movements.
/// Provides human-readable descriptions and metadata for DocumentType enum values.
/// </summary>
public class DocumentTypeMaster
{
    public int Id { get; set; }

    [Required]
    [StringLength(10)]
    public string Code { get; set; } = "";  // FA, NC, ND, RE, PA, etc.

    [Required]
    [StringLength(100)]
    public string Description { get; set; } = "";  // Factura, Nota de Crédito, etc.

    /// <summary>Impact on current account balance: 1 = increases debt, -1 = decreases debt</summary>
    public int BalanceImpact { get; set; } = 1;

    /// <summary>Indicates if document affects billing line (true) or budget line (false)</summary>
    public bool IsBillingLine { get; set; } = true;

    public bool IsActive { get; set; } = true;
}
