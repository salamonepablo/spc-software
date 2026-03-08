using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Service for retrieving company settings.
/// </summary>
public interface ICompanySettingsService
{
    /// <summary>
    /// Gets the current company settings.
    /// Returns null if no settings are configured.
    /// </summary>
    Task<CompanySettings?> GetSettingsAsync();
    
    /// <summary>
    /// Checks if the company is an IIBB perception agent.
    /// </summary>
    Task<bool> IsIIBBPerceptionAgentAsync();
    
    /// <summary>
    /// Checks if the company is an IVA withholding agent.
    /// </summary>
    Task<bool> IsIVAWithholdingAgentAsync();
}
