# ADR-005: Modular License System

## Status

**Accepted** - 2024

## Context

The SPC software needs to support different customer tiers with varying feature sets:

- **Base customers**: Core ERP functionality
- **Premium customers**: Additional features like dual product lines
- **Enterprise customers**: Full feature set including multi-branch support

We needed a licensing system that:
1. Is easy to configure per deployment
2. Doesn't require code changes for each customer
3. Can be extended with new features
4. Is testable

## Decision

Implement a **configuration-based licensing system** with feature flags.

## Rationale

### Approach Comparison

| Approach | Complexity | Flexibility | Security |
|----------|------------|-------------|----------|
| Compile-time flags | Low | None | High |
| License file | Medium | High | Medium |
| **Config-based** | Low | High | Medium |
| License server | High | Very High | High |

### Why Config-Based

1. **Simple deployment** - Change config, restart app
2. **No external dependencies** - No license server needed
3. **Easy testing** - Mock configuration in tests
4. **Transparent** - Customer can see their tier in config
5. **Sufficient for current scale** - Single company with known deployments

## Consequences

### Positive

- Simple to implement and maintain
- Easy to test different license tiers
- No network dependencies
- Clear feature visibility

### Negative

- Config can be modified by end users (low risk for internal ERP)
- No remote license management
- Manual updates required for tier changes

### Acceptable Because

- This is an internal ERP, not commercial software
- Trust level with deployment locations is high
- Scale is limited (single company, few branches)

## Implementation

### License Tiers

| Tier | Features |
|------|----------|
| BASE | Core CRUD, single warehouse, basic invoicing |
| PREMIUM | + Dual product line support |
| ENTERPRISE | + Multi-branch, advanced reporting |

### Configuration

**appsettings.json**
```json
{
  "License": {
    "Tier": "PREMIUM",
    "EnabledFeatures": ["DualLine"]
  }
}
```

### Service Interface

```csharp
public interface ILicenseService
{
    LicenseInfo GetLicenseInfo();
    bool IsFeatureEnabled(string featureName);
}
```

### Service Implementation

```csharp
public class LicenseService : ILicenseService
{
    private readonly IConfiguration _config;

    public LicenseService(IConfiguration config)
    {
        _config = config;
    }

    public LicenseInfo GetLicenseInfo()
    {
        return new LicenseInfo
        {
            Tier = _config["License:Tier"] ?? "BASE",
            EnabledFeatures = _config.GetSection("License:EnabledFeatures")
                .Get<string[]>() ?? Array.Empty<string>()
        };
    }

    public bool IsFeatureEnabled(string featureName)
    {
        var features = GetLicenseInfo().EnabledFeatures;
        return features.Contains(featureName, StringComparer.OrdinalIgnoreCase);
    }
}
```

### API Endpoint

```csharp
app.MapGet("/api/license", (ILicenseService license) =>
{
    return Results.Ok(license.GetLicenseInfo());
});
```

### Usage in Code

```csharp
// Check feature before showing UI element
if (licenseService.IsFeatureEnabled("DualLine"))
{
    // Show dual line product selector
}

// Check feature in API endpoint
app.MapPost("/api/products/dualline", async (...) =>
{
    if (!licenseService.IsFeatureEnabled("DualLine"))
        return Results.Forbid();
    
    // Process dual line product
});
```

### Testing

```csharp
[Fact]
public void IsFeatureEnabled_ReturnsTrue_WhenDualLineEnabledInConfig()
{
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["License:Tier"] = "PREMIUM",
            ["License:EnabledFeatures:0"] = "DualLine"
        })
        .Build();

    var service = new LicenseService(config);

    service.IsFeatureEnabled("DualLine").Should().BeTrue();
}
```

## Future Considerations

- Add license key validation for additional security
- Implement license expiration dates
- Add feature usage analytics
- Consider encrypted license file for commercial distribution

## References

- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Feature Flags in .NET](https://learn.microsoft.com/en-us/azure/azure-app-configuration/use-feature-flags-dotnet-core)
