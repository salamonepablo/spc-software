using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// System configuration for tax rates.
/// Allows VAT and other tax rates to be changed without code modifications.
/// </summary>
public class TaxSetting
{
    public int Id { get; set; }
    
    /// <summary>Tax type identifier (e.g., "VAT", "IIBB_BA", "IIBB_CABA")</summary>
    [Required]
    [StringLength(20)]
    public string TaxCode { get; set; } = "";
    
    /// <summary>Human-readable description</summary>
    [Required]
    [StringLength(100)]
    public string Description { get; set; } = "";
    
    /// <summary>Tax rate as percentage (e.g., 21.00 for 21%)</summary>
    public decimal Rate { get; set; }
    
    /// <summary>Is this the default rate for this tax type?</summary>
    public bool IsDefault { get; set; } = false;
    
    /// <summary>Is this setting active?</summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>Effective date (when this rate starts to apply)</summary>
    public DateTime EffectiveFrom { get; set; } = DateTime.Now;
    
    /// <summary>End date (null = no end date)</summary>
    public DateTime? EffectiveTo { get; set; }
}
