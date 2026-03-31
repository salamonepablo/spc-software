# ADR-002: Clean Architecture with Services and DTOs

## Status

**Accepted** - 2024

## Context

The initial implementation had endpoints directly in `Program.cs` with business logic mixed into route handlers. This worked for prototyping but violated several principles:

- Single Responsibility Principle (endpoints doing too much)
- Separation of Concerns (no clear layers)
- Testability (hard to unit test business logic)
- EF entities exposed directly in API responses (coupling)

## Decision

Implement **Clean Architecture** with three distinct layers in `SPC.API`:

1. **Endpoints** (`/Endpoints`) - HTTP request/response handling only
2. **Services** (`/Services`) - Business logic and data access
3. **Contracts** (`/Contracts`) - DTOs for API input/output

## Rationale

### Layer Responsibilities

| Layer | Responsibility | Dependencies |
|-------|---------------|--------------|
| Endpoints | Route handling, HTTP concerns | Services |
| Services | Business logic, validation, EF operations | DbContext, Entities |
| Contracts | Data transfer objects | None |

### Benefits

1. **Testability** - Services can be unit tested without HTTP context
2. **Maintainability** - Changes isolated to appropriate layer
3. **Security** - DTOs prevent over-posting attacks
4. **Flexibility** - Entity changes don't break API contracts
5. **SOLID compliance** - Each class has single responsibility

## Consequences

### Positive

- Clear separation of concerns
- Easy to add validation logic in services
- API contracts independent from database schema
- Better code organization for scaling

### Negative

- More files to maintain
- Mapping overhead between entities and DTOs
- Slightly more complex initial setup

## Implementation

### Folder Structure

```
SPC.API/
├── Contracts/
│   ├── Clientes/
│   │   ├── ClienteResponse.cs
│   │   ├── CreateClienteRequest.cs
│   │   └── UpdateClienteRequest.cs
│   └── Productos/
│       ├── ProductoResponse.cs
│       ├── CreateProductoRequest.cs
│       └── UpdateProductoRequest.cs
├── Endpoints/
│   ├── ClientesEndpoints.cs
│   └── ProductosEndpoints.cs
├── Services/
│   ├── IClientesService.cs
│   ├── ClientesService.cs
│   ├── IProductosService.cs
│   └── ProductosService.cs
└── Program.cs
```

### Code Pattern

**Endpoint (thin)**
```csharp
group.MapGet("/{id}", async (int id, IClientesService service) =>
{
    var cliente = await service.GetByIdAsync(id);
    return cliente is not null 
        ? Results.Ok(cliente) 
        : Results.NotFound();
});
```

**Service (business logic)**
```csharp
public async Task<ClienteResponse?> GetByIdAsync(int id)
{
    var cliente = await _db.Clientes
        .Include(c => c.CondicionIva)
        .FirstOrDefaultAsync(c => c.Id == id && c.Activo);
    
    return cliente is null ? null : MapToResponse(cliente);
}
```

**DTO (contract)**
```csharp
public record ClienteResponse(
    int Id,
    string RazonSocial,
    string CUIT,
    string CondicionIva,
    // ... only fields needed by API consumers
);
```

### Dependency Injection

Services are registered in `Program.cs`:

```csharp
builder.Services.AddScoped<IClientesService, ClientesService>();
builder.Services.AddScoped<IProductosService, ProductosService>();
```

## References

- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CLAUDE.md](../../CLAUDE.md) - Project architecture guidelines
