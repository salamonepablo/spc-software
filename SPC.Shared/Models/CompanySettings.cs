using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Company-level settings for tax agent status.
/// Determines if the company acts as withholding/perception agent for taxes.
/// </summary>
public class CompanySettings
{
    public int Id { get; set; }
    
    /// <summary>Company name</summary>
    [Required]
    [StringLength(200)]
    public string CompanyName { get; set; } = "";
    
    /// <summary>Company CUIT</summary>
    [Required]
    [StringLength(13)]
    public string CUIT { get; set; } = "";
    
    /// <summary>
    /// Is this company an IVA Withholding Agent (Agente de Retención IVA)?
    /// If true, must withhold IVA from payments to certain suppliers.
    /// </summary>
    public bool IsIVAWithholdingAgent { get; set; } = false;
    
    /// <summary>
    /// Is this company an IIBB Perception Agent (Agente de Percepción IIBB)?
    /// If true, must add IIBB perception to invoices for applicable customers.
    /// The perception rate comes from each customer's padrón (AlicuotaIIBB).
    /// </summary>
    public bool IsIIBBPerceptionAgent { get; set; } = false;
    
    /// <summary>
    /// Province where the company is registered for IIBB.
    /// Determines which padrón applies (ARBA, AGIP, etc.)
    /// </summary>
    [StringLength(50)]
    public string? IIBBProvince { get; set; }
    
    /// <summary>IIBB registration number</summary>
    [StringLength(20)]
    public string? IIBBRegistrationNumber { get; set; }
    
    /// <summary>Start of fiscal activities</summary>
    public DateTime? FiscalActivityStartDate { get; set; }
    
    /// <summary>Is active?</summary>
    public bool IsActive { get; set; } = true;
}
