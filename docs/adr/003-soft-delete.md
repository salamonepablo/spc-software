# ADR-003: Soft Delete for Business Entities

## Status

**Accepted** - 2024

## Context

Business entities like Customers (Clientes) and Products (Productos) need deletion functionality. Two approaches exist:

1. **Hard Delete** - Permanently remove record from database
2. **Soft Delete** - Mark record as inactive, keep in database

In the legacy VB6 system, deletions were hard deletes, which caused issues:
- Lost historical data
- Broken references in invoices and reports
- No audit trail

## Decision

Implement **Soft Delete** for all business entities using an `Activo` (Active) boolean field.

## Rationale

### Why Soft Delete

| Concern | Hard Delete | Soft Delete |
|---------|-------------|-------------|
| Data recovery | Impossible | Easy |
| Historical integrity | Broken references | Preserved |
| Audit compliance | No trail | Full history |
| Accidental deletion | Catastrophic | Recoverable |
| Performance | Slightly better | Minimal impact |

### Business Requirements

1. **Invoices reference customers** - Deleting a customer would orphan invoice records
2. **Stock history references products** - Product history must be preserved
3. **Audit requirements** - Business needs to track what existed at any point
4. **User error recovery** - Users frequently request "undo" for deletions

## Consequences

### Positive

- Data integrity preserved across relationships
- Easy recovery from accidental deletions
- Complete audit trail
- Historical reports remain accurate

### Negative

- Database grows larger over time
- All queries must filter by `Activo = true`
- Need separate "purge" process for true deletion (GDPR compliance)

## Implementation

### Entity Pattern

All soft-deletable entities include:

```csharp
public class Cliente
{
    public int Id { get; set; }
    // ... other properties
    public bool Activo { get; set; } = true;
    public DateTime FechaAlta { get; set; }
    public DateTime? FechaBaja { get; set; }  // When soft-deleted
}
```

### Service Layer

Delete operations set `Activo = false`:

```csharp
public async Task<bool> DeleteAsync(int id)
{
    var cliente = await _db.Clientes.FindAsync(id);
    if (cliente is null || !cliente.Activo)
        return false;
    
    cliente.Activo = false;
    cliente.FechaBaja = DateTime.Now;
    await _db.SaveChangesAsync();
    return true;
}
```

### Query Pattern

All "list" queries filter active records:

```csharp
public async Task<List<ClienteResponse>> GetAllAsync()
{
    return await _db.Clientes
        .Where(c => c.Activo)  // Only active records
        .Select(c => MapToResponse(c))
        .ToListAsync();
}
```

### API Response

DELETE endpoint returns `204 No Content` on success:

```csharp
group.MapDelete("/{id}", async (int id, IClientesService service) =>
{
    var deleted = await service.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
});
```

## Future Considerations

- Add global query filter in EF Core for automatic `Activo` filtering
- Implement "restore" endpoint for recovering deleted records
- Add purge functionality for GDPR "right to be forgotten" compliance

## References

- [EF Core Global Query Filters](https://learn.microsoft.com/en-us/ef/core/querying/filters)
- [CLAUDE.md](../../CLAUDE.md) - "Soft delete instead of physical deletion"
