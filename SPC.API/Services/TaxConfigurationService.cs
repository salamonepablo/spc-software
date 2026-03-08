using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SPC.API.Data;

namespace SPC.API.Services;

/// <summary>
/// Tax configuration service implementation.
/// Retrieves tax rates from database with fallback to appsettings.
/// </summary>
public class TaxConfigurationService : ITaxConfigurationService
{
    private readonly SPCDbContext _db;
    private readonly IConfiguration _configuration;
    
    // Tax code constants
    public const string VAT_CODE = "VAT";
    public const string IIBB_PREFIX = "IIBB_";
    
    public TaxConfigurationService(SPCDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }
    
    public async Task<decimal> GetDefaultVATRateAsync()
    {
        // Try database first
        var dbRate = await GetTaxRateFromDbAsync(VAT_CODE, DateTime.Now);
        if (dbRate.HasValue)
            return dbRate.Value;
        
        // Fallback to appsettings
        var configRate = _configuration.GetValue<decimal?>("TaxSettings:DefaultVATRate");
        if (configRate.HasValue)
            return configRate.Value;
        
        // Last resort: standard Argentine VAT rate
        // NOTE: This should ideally not be reached - always configure in DB or appsettings
        return 21.00m;
    }
    
    public async Task<decimal?> GetTaxRateAsync(string taxCode)
    {
        return await GetTaxRateFromDbAsync(taxCode, DateTime.Now);
    }
    
    public async Task<decimal> GetVATRateForDateAsync(DateTime date)
    {
        // Try database for historical rate
        var dbRate = await GetTaxRateFromDbAsync(VAT_CODE, date);
        if (dbRate.HasValue)
            return dbRate.Value;
        
        // Fallback to appsettings (no historical support)
        var configRate = _configuration.GetValue<decimal?>("TaxSettings:DefaultVATRate");
        return configRate ?? 21.00m;
    }
    
    public async Task<decimal> GetIIBBRateAsync(string provinceCode)
    {
        var taxCode = $"{IIBB_PREFIX}{provinceCode.ToUpperInvariant()}";
        var rate = await GetTaxRateFromDbAsync(taxCode, DateTime.Now);
        
        if (rate.HasValue)
            return rate.Value;
        
        // Try config section for province-specific IIBB
        var configRate = _configuration.GetValue<decimal?>($"TaxSettings:IIBB:{provinceCode}");
        return configRate ?? 0m;
    }
    
    /// <summary>
    /// Gets tax rate from database for a specific date.
    /// </summary>
    private async Task<decimal?> GetTaxRateFromDbAsync(string taxCode, DateTime effectiveDate)
    {
        var setting = await _db.TaxSettings
            .Where(t => t.TaxCode == taxCode 
                     && t.IsActive 
                     && t.EffectiveFrom <= effectiveDate
                     && (t.EffectiveTo == null || t.EffectiveTo >= effectiveDate))
            .OrderByDescending(t => t.EffectiveFrom)
            .FirstOrDefaultAsync();
        
        return setting?.Rate;
    }
}
