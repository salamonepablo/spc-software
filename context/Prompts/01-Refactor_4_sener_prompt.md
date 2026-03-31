# SPC Software Project — CRUD Implementation Task

## Context

You are working inside an existing .NET solution.

Main projects:

* **SPC.API** → ASP.NET Minimal API backend
* **SPC.Shared** → domain models (entities)
* **SPC.Tests** → xUnit unit + integration tests
* **SPC.Web** → Blazor UI
* **SPC.Migration** → migration tooling

Database stack:

* EF Core
* SQLite
* `SPCDbContext` located in `SPC.API/Data`

Existing infrastructure:

* Minimal APIs
* Integration test factory (`SPCWebApplicationFactory`)
* Licensing service
* Domain entities already defined in `SPC.Shared/Models`

Examples include:

* `Cliente`
* `Producto`
* `Factura`
* etc.

The solution builds and tests already run successfully.

---

# Objective

Implement the **first real business CRUD APIs** for:

* `Clientes`
* `Productos`

while keeping the architecture clean and scalable.

---

# Architectural Constraints

### Program.cs must stay minimal

Do NOT implement endpoints directly in `Program.cs`.

Instead create endpoint modules.

Folder:

```
SPC.API/Endpoints
```

Files:

```
ClientesEndpoints.cs
ProductosEndpoints.cs
```

Each file must expose:

```
public static IEndpointRouteBuilder MapClientesEndpoints(this IEndpointRouteBuilder app)
```

and

```
public static IEndpointRouteBuilder MapProductosEndpoints(this IEndpointRouteBuilder app)
```

Program.cs should only call these methods.

---

# Service Layer

Business logic must NOT live in endpoints.

Create services inside:

```
SPC.API/Services
```

Files:

```
IClientesService.cs
ClientesService.cs

IProductosService.cs
ProductosService.cs
```

Responsibilities of services:

* database interaction
* business rules
* entity → DTO mapping
* validation if needed

Endpoints should only:

* receive request
* call service
* return HTTP result

---

# DTO Layer

Do NOT expose EF entities directly from the API.

Create DTOs in:

```
SPC.API/Contracts
```

Structure:

```
Contracts/
    Clientes/
        CreateClienteRequest.cs
        UpdateClienteRequest.cs
        ClienteResponse.cs

    Productos/
        CreateProductoRequest.cs
        UpdateProductoRequest.cs
        ProductoResponse.cs
```

DTO rules:

Request DTOs:

* used by POST / PUT

Response DTOs:

* returned by API

Entities from `SPC.Shared` must not be returned directly.

---

# Endpoints to Implement

## Clientes

Base route:

```
/api/clientes
```

Endpoints:

GET `/api/clientes/{id}`

GET `/api/clientes`

POST `/api/clientes`

PUT `/api/clientes/{id}`

Optional future extension:

DELETE or soft-delete.

---

## Productos

Base route:

```
/api/productos
```

Endpoints:

GET `/api/productos/{id}`

GET `/api/productos`

POST `/api/productos`

PUT `/api/productos/{id}`

---

# EF Core Integration

Use the existing DbContext:

```
SPC.API/Data/SPCDbContext.cs
```

Inject it into services.

Example pattern:

```
public class ClientesService : IClientesService
{
    private readonly SPCDbContext _db;

    public ClientesService(SPCDbContext db)
    {
        _db = db;
    }
}
```

Use async EF operations.

---

# Minimal API Structure Example

Endpoints should use grouping:

```
var group = app.MapGroup("/api/clientes")
               .WithTags("Clientes");
```

Return appropriate HTTP responses:

* 200 OK
* 201 Created
* 404 NotFound
* 400 BadRequest when applicable

---

# Integration Tests

Add tests in:

```
SPC.Tests/Integration
```

Files:

```
ClientesEndpointsTests.cs
ProductosEndpointsTests.cs
```

Use existing `SPCWebApplicationFactory`.

Minimum tests required:

Clientes:

* create cliente
* get cliente by id

Productos:

* create producto
* get producto by id

Tests must run using the in-memory or test database configuration already used by the project.

---

# Acceptance Criteria

The implementation is complete when:

* Solution builds successfully
* `dotnet test` passes
* CRUD endpoints work
* DTOs are used instead of EF entities
* Program.cs remains minimal
* Endpoints are separated into modules
* Services encapsulate business logic

---

# Important Constraints

Do NOT:

* break existing tests
* modify domain entities unnecessarily
* add heavy frameworks
* introduce complex patterns not already used in the project

Keep the implementation simple, readable, and idiomatic for ASP.NET Minimal APIs.
