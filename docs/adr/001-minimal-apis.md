# ADR-001: Use Minimal APIs over Controllers

## Status

**Accepted** - 2024

## Context

ASP.NET Core offers two approaches for building REST APIs:

1. **Controllers** (MVC pattern) - Class-based, introduced in .NET Core 1.0 (2016)
2. **Minimal APIs** - Functional, lambda-based, introduced in .NET 6 (2021)

We needed to choose an approach for the SPC ERP migration from VB6 to .NET.

## Decision

We will use **Minimal APIs** for the SPC REST API.

## Rationale

### Advantages for this project

1. **Modern approach** - Aligns with current industry trends and .NET direction
2. **Less boilerplate** - Simpler code structure, easier to learn for VB6 transition
3. **Sufficient for scope** - Project has ~20-30 endpoints, well within Minimal APIs' sweet spot
4. **Portfolio value** - Demonstrates knowledge of current .NET practices
5. **Performance** - Slightly better performance due to reduced middleware
6. **Flexibility** - Can migrate to Controllers later if needed

### When we would reconsider

- If the API grows to 50+ endpoints
- If we need complex per-controller filters or middleware
- If a large team requires clear file separation

## Consequences

### Positive

- Cleaner `Program.cs` with endpoint modules
- Faster development for simple CRUD operations
- Easier onboarding for developers new to .NET

### Negative

- Less familiar pattern for developers coming from traditional MVC
- Need to organize endpoints manually into modules (we created `/Endpoints` folder)
- Some advanced features require more setup (e.g., complex model binding)

## Implementation

Endpoints are organized in modular files:

```
SPC.API/
├── Endpoints/
│   ├── ClientesEndpoints.cs
│   └── ProductosEndpoints.cs
└── Program.cs (calls MapXxxEndpoints())
```

Each module exposes an extension method:

```csharp
public static class ClientesEndpoints
{
    public static IEndpointRouteBuilder MapClientesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clientes").WithTags("Clientes");
        // ... endpoints
        return app;
    }
}
```

## References

- [Minimal APIs Overview](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [docs/minimal-apis-vs-controllers.md](../minimal-apis-vs-controllers.md) - Detailed comparison
