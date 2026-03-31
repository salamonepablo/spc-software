namespace SPC.API.Services;

/// <summary>
/// Service for retrieving tax configuration at runtime.
/// Supports database-driven tax rates with fallback to appsettings.
/// </summary>
public interface ITaxConfigurationService
{
    /// <summary>
    /// Gets the current default VAT rate.
    /// </summary>
    /// <returns>VAT rate as decimal percentage (e.g., 21.00 for 21%)</returns>
    Task<decimal> GetDefaultVATRateAsync();
    
    /// <summary>
    /// Gets a specific tax rate by code.
    /// </summary>
    /// <param name="taxCode">Tax code (e.g., "VAT", "IIBB_BA")</param>
    /// <returns>Tax rate or null if not found</returns>
    Task<decimal?> GetTaxRateAsync(string taxCode);
    
    /// <summary>
    /// Gets the VAT rate effective on a specific date.
    /// Used for historical document lookup.
    /// </summary>
    /// <param name="date">The date to check</param>
    /// <returns>VAT rate effective on that date</returns>
    Task<decimal> GetVATRateForDateAsync(DateTime date);
    
    /// <summary>
    /// Gets the IIBB perception rate for a province.
    /// </summary>
    /// <param name="provinceCode">Province code (e.g., "BA", "CABA")</param>
    /// <returns>IIBB rate or 0 if not applicable</returns>
    Task<decimal> GetIIBBRateAsync(string provinceCode);
}
