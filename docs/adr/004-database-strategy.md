# ADR-004: SQLite for Development, SQL Server for Production

## Status

**Accepted** - 2024

## Context

The legacy system used Microsoft Access as its database. For the migration to .NET, we needed to choose:

1. A **development database** that is easy to set up and portable
2. A **production database** that is robust and scalable

## Decision

Use **SQLite** for development and testing, **SQL Server** for production.

## Rationale

### SQLite for Development

| Benefit | Description |
|---------|-------------|
| Zero configuration | No server installation needed |
| Portable | Single file, easy to share/reset |
| Fast | In-process, no network overhead |
| Cross-platform | Works on Windows, Mac, Linux |
| CI/CD friendly | No database server in pipelines |

### SQL Server for Production

| Benefit | Description |
|---------|-------------|
| Enterprise-grade | Proven reliability at scale |
| Windows Auth | Seamless integration with AD |
| Backup/Recovery | Professional tooling |
| Performance | Optimized for concurrent users |
| Existing infrastructure | Company already uses SQL Server |

### EF Core Abstraction

Entity Framework Core abstracts database differences:

```csharp
// Same code works for both databases
var clientes = await _db.Clientes
    .Where(c => c.Activo)
    .Include(c => c.CondicionIva)
    .ToListAsync();
```

## Consequences

### Positive

- Developers don't need SQL Server locally
- Fast test execution (in-memory option available)
- Easy onboarding for new developers
- Consistent with production via EF migrations

### Negative

- Some SQL Server features not available in SQLite
- Need to test migrations on SQL Server before production
- Minor syntax differences in raw SQL (avoid when possible)

### Mitigations

- Use EF Core queries instead of raw SQL
- Run integration tests against SQL Server in CI before release
- Document any SQL Server-specific features used

## Implementation

### Configuration

**appsettings.Development.json** (SQLite)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=SPC.db"
  }
}
```

**appsettings.Production.json** (SQL Server)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=SPC;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### DbContext Configuration

```csharp
// Program.cs
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<SPCDbContext>(options =>
        options.UseSqlite(connectionString));
}
else
{
    builder.Services.AddDbContext<SPCDbContext>(options =>
        options.UseSqlServer(connectionString));
}
```

### Test Configuration

Tests use InMemory database for isolation:

```csharp
// SPCWebApplicationFactory.cs
services.AddDbContext<SPCDbContext>(options =>
    options.UseInMemoryDatabase("TestDb"));
```

## Migration Path

1. **Development**: SQLite file (`SPC.db`)
2. **Testing**: InMemory database (per test class)
3. **Staging**: SQL Server (separate database)
4. **Production**: SQL Server (main database)

## Future Considerations

- Consider PostgreSQL as open-source alternative
- Azure SQL for cloud deployment
- Read replicas for reporting workloads

## References

- [EF Core Database Providers](https://learn.microsoft.com/en-us/ef/core/providers/)
- [SQLite Limitations](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations)
