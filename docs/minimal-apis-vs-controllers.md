# Minimal APIs vs Controllers in ASP.NET Core

## Overview

ASP.NET Core offers two approaches for building REST APIs:

| Approach | Introduced | Style |
|----------|------------|-------|
| **Controllers** | .NET Core 1.0 (2016) | MVC pattern, class-based |
| **Minimal APIs** | .NET 6 (2021) | Functional, lambda-based |

**SPC Project uses: Minimal APIs**

---

## Side-by-Side Comparison

### GET All Customers

**Controllers:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly SPCDbContext _db;
    
    public ClientesController(SPCDbContext db)
    {
        _db = db;
    }
    
    [HttpGet]
    public async Task<ActionResult<List<Cliente>>> GetAll()
    {
        var clientes = await _db.Clientes
            .Include(c => c.CondicionIva)
            .Where(c => c.Activo)
            .ToListAsync();
        return Ok(clientes);
    }
}
```

**Minimal APIs:**
```csharp
app.MapGet("/api/clientes", async (SPCDbContext db) =>
{
    var clientes = await db.Clientes
        .Include(c => c.CondicionIva)
        .Where(c => c.Activo)
        .ToListAsync();
    return Results.Ok(clientes);
});
```

---

### GET by ID

**Controllers:**
```csharp
[HttpGet("{id}")]
public async Task<ActionResult<Cliente>> GetById(int id)
{
    var cliente = await _db.Clientes.FindAsync(id);
    
    if (cliente == null)
        return NotFound(new { error = "Cliente no encontrado" });
    
    return Ok(cliente);
}
```

**Minimal APIs:**
```csharp
app.MapGet("/api/clientes/{id}", async (int id, SPCDbContext db) =>
{
    var cliente = await db.Clientes.FindAsync(id);
    
    return cliente != null 
        ? Results.Ok(cliente) 
        : Results.NotFound(new { error = "Cliente no encontrado" });
});
```

---

### POST (Create)

**Controllers:**
```csharp
[HttpPost]
public async Task<ActionResult<Cliente>> Create(Cliente cliente)
{
    cliente.FechaAlta = DateTime.Now;
    cliente.Activo = true;
    
    _db.Clientes.Add(cliente);
    await _db.SaveChangesAsync();
    
    return CreatedAtAction(nameof(GetById), new { id = cliente.Id }, cliente);
}
```

**Minimal APIs:**
```csharp
app.MapPost("/api/clientes", async (Cliente cliente, SPCDbContext db) =>
{
    cliente.FechaAlta = DateTime.Now;
    cliente.Activo = true;
    
    db.Clientes.Add(cliente);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/clientes/{cliente.Id}", cliente);
});
```

---

### PUT (Update)

**Controllers:**
```csharp
[HttpPut("{id}")]
public async Task<ActionResult<Cliente>> Update(int id, Cliente clienteActualizado)
{
    var cliente = await _db.Clientes.FindAsync(id);
    
    if (cliente == null)
        return NotFound();
    
    cliente.RazonSocial = clienteActualizado.RazonSocial;
    cliente.CUIT = clienteActualizado.CUIT;
    // ... more fields
    
    await _db.SaveChangesAsync();
    return Ok(cliente);
}
```

**Minimal APIs:**
```csharp
app.MapPut("/api/clientes/{id}", async (int id, Cliente clienteActualizado, SPCDbContext db) =>
{
    var cliente = await db.Clientes.FindAsync(id);
    
    if (cliente == null)
        return Results.NotFound();
    
    cliente.RazonSocial = clienteActualizado.RazonSocial;
    cliente.CUIT = clienteActualizado.CUIT;
    // ... more fields
    
    await db.SaveChangesAsync();
    return Results.Ok(cliente);
});
```

---

### DELETE (Soft Delete)

**Controllers:**
```csharp
[HttpDelete("{id}")]
public async Task<IActionResult> Delete(int id)
{
    var cliente = await _db.Clientes.FindAsync(id);
    
    if (cliente == null)
        return NotFound();
    
    cliente.Activo = false;  // Soft delete
    await _db.SaveChangesAsync();
    
    return NoContent();
}
```

**Minimal APIs:**
```csharp
app.MapDelete("/api/clientes/{id}", async (int id, SPCDbContext db) =>
{
    var cliente = await db.Clientes.FindAsync(id);
    
    if (cliente == null)
        return Results.NotFound();
    
    cliente.Activo = false;  // Soft delete
    await db.SaveChangesAsync();
    
    return Results.NoContent();
});
```

---

## File Structure Comparison

### Controllers Approach
```
SPC.API/
├── Controllers/
│   ├── ClientesController.cs
│   ├── ProductosController.cs
│   ├── FacturasController.cs
│   └── VendedoresController.cs
├── Data/
│   └── SPCDbContext.cs
└── Program.cs (minimal setup)
```

### Minimal APIs Approach (Current)
```
SPC.API/
├── Data/
│   └── SPCDbContext.cs
└── Program.cs (all endpoints here)
```

---

## When to Use Each

### Use Minimal APIs when:
- Small to medium APIs
- Microservices
- Learning/prototyping
- New projects (.NET 6+)
- You want less boilerplate

### Use Controllers when:
- Large APIs (50+ endpoints)
- Large teams (clear separation)
- Need complex filters per controller
- Maintaining legacy code
- Prefer OOP patterns

---

## Hybrid Approach

You can use both in the same project:

```csharp
// Program.cs
var app = builder.Build();

// Minimal APIs for simple endpoints
app.MapGet("/health", () => "OK");
app.MapGet("/api/config", () => new { version = "1.0" });

// Controllers for complex entities
app.MapControllers();  // This enables controller routing
```

---

## Response Helpers

| Action | Controllers | Minimal APIs |
|--------|-------------|--------------|
| 200 OK | `Ok(data)` | `Results.Ok(data)` |
| 201 Created | `CreatedAtAction(...)` | `Results.Created(url, data)` |
| 204 No Content | `NoContent()` | `Results.NoContent()` |
| 400 Bad Request | `BadRequest(error)` | `Results.BadRequest(error)` |
| 404 Not Found | `NotFound()` | `Results.NotFound()` |
| 500 Error | `StatusCode(500)` | `Results.StatusCode(500)` |

---

## SPC Project Decision

**We chose Minimal APIs because:**

1. Modern approach (industry trend)
2. Less code to learn initially
3. Sufficient for project scope
4. Good for portfolio (shows current knowledge)
5. Easy to refactor to Controllers later if needed

---

## Resources

- [Minimal APIs Overview](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [Controllers Overview](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [Minimal APIs vs Controllers](https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api)
