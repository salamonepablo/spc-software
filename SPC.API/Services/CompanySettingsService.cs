using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Company settings service implementation.
/// </summary>
public class CompanySettingsService : ICompanySettingsService
{
    private readonly SPCDbContext _db;

    public CompanySettingsService(SPCDbContext db)
    {
        _db = db;
    }

    public async Task<CompanySettings?> GetSettingsAsync()
    {
        // Get the first active company settings
        return await _db.CompanySettings
            .Where(c => c.IsActive)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsIIBBPerceptionAgentAsync()
    {
        var settings = await GetSettingsAsync();
        return settings?.IsIIBBPerceptionAgent ?? false;
    }

    public async Task<bool> IsIVAWithholdingAgentAsync()
    {
        var settings = await GetSettingsAsync();
        return settings?.IsIVAWithholdingAgent ?? false;
    }
}
