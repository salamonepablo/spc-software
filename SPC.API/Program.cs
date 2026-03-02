// =============================================
// SPC - Sistema de Gestión Comercial
// API REST con ASP.NET Core
// =============================================

using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.API.Services;
using SPC.Shared.Licensing;
using SPC.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

// Configurar JSON para ignorar referencias circulares
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// Configurar Licensing (feature flags)
builder.Services.Configure<LicensingOptions>(
    builder.Configuration.GetSection(LicensingOptions.SectionName));
builder.Services.AddSingleton<ILicenseService, LicenseService>();

// Configurar Entity Framework con SQL Server (LocalDB en desarrollo)
builder.Services.AddDbContext<SPCDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Habilitar CORS para que Blazor pueda consumir la API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Swagger/OpenAPI para documentación
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// Habilitar Swagger UI en desarrollo
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "SPC API v1");
        options.RoutePrefix = "swagger";
    });
}

// Aplicar migraciones pendientes automaticamente (solo para bases de datos relacionales)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SPCDbContext>();
    db.Database.Migrate();
}

app.UseCors("AllowBlazor");

// =============================================
// ENDPOINT RAIZ
// =============================================
app.MapGet("/", (ILicenseService license) => new 
{ 
    sistema = "SPC - Sistema de Gestion Comercial",
    version = "1.0",
    license = license.GetLicenseInfo().Tier,
    endpoints = new[] { "/api/clientes", "/api/productos", "/api/condicionesiva", "/api/license" }
});

// =============================================
// ENDPOINTS - LICENSE INFO
// =============================================
app.MapGet("/api/license", (ILicenseService license) =>
{
    var info = license.GetLicenseInfo();
    return Results.Ok(new
    {
        tier = info.Tier,
        customerId = info.CustomerId,
        isValid = info.IsValid,
        expiresAt = info.ExpiresAt,
        features = new
        {
            dualLineCurrentAccount = license.IsFeatureEnabled(Features.DualLineCurrentAccount),
            multiBranch = license.IsFeatureEnabled(Features.MultiBranch)
        }
    });
});


// =============================================
// ENDPOINTS - CLIENTES
// =============================================

// GET /api/clientes - Listar todos
app.MapGet("/api/clientes", async (SPCDbContext db) =>
{
    var clientes = await db.Clientes
        .Include(c => c.CondicionIva)
        .Include(c => c.Vendedor)
        .Include(c => c.ZonaVenta)
        .Where(c => c.Activo)
        .OrderBy(c => c.RazonSocial)
        .ToListAsync();
    
    return Results.Ok(clientes);
});

// GET /api/clientes/{id} - Obtener uno
app.MapGet("/api/clientes/{id}", async (int id, SPCDbContext db) =>
{
    var cliente = await db.Clientes
        .Include(c => c.CondicionIva)
        .Include(c => c.Vendedor)
        .Include(c => c.ZonaVenta)
        .FirstOrDefaultAsync(c => c.Id == id);
    
    return cliente != null 
        ? Results.Ok(cliente) 
        : Results.NotFound(new { error = "Cliente no encontrado" });
});

// GET /api/clientes/buscar?nombre=xxx - Buscar por nombre
app.MapGet("/api/clientes/buscar", async (string? nombre, SPCDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(nombre))
        return Results.BadRequest(new { error = "Debe proporcionar un nombre para buscar" });
    
    var clientes = await db.Clientes
        .Include(c => c.CondicionIva)
        .Where(c => c.Activo && 
               (c.RazonSocial.Contains(nombre) || 
                (c.NombreFantasia != null && c.NombreFantasia.Contains(nombre))))
        .OrderBy(c => c.RazonSocial)
        .ToListAsync();
    
    return Results.Ok(clientes);
});

// POST /api/clientes - Crear nuevo
app.MapPost("/api/clientes", async (Cliente cliente, SPCDbContext db) =>
{
    cliente.FechaAlta = DateTime.Now;
    cliente.Activo = true;
    
    db.Clientes.Add(cliente);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/clientes/{cliente.Id}", cliente);
});

// PUT /api/clientes/{id} - Actualizar
app.MapPut("/api/clientes/{id}", async (int id, Cliente clienteActualizado, SPCDbContext db) =>
{
    var cliente = await db.Clientes.FindAsync(id);
    
    if (cliente == null)
        return Results.NotFound(new { error = "Cliente no encontrado" });
    
    // Actualizar propiedades
    cliente.RazonSocial = clienteActualizado.RazonSocial;
    cliente.NombreFantasia = clienteActualizado.NombreFantasia;
    cliente.CUIT = clienteActualizado.CUIT;
    cliente.Direccion = clienteActualizado.Direccion;
    cliente.Localidad = clienteActualizado.Localidad;
    cliente.Provincia = clienteActualizado.Provincia;
    cliente.CodigoPostal = clienteActualizado.CodigoPostal;
    cliente.Telefono = clienteActualizado.Telefono;
    cliente.Celular = clienteActualizado.Celular;
    cliente.Email = clienteActualizado.Email;
    cliente.CondicionIvaId = clienteActualizado.CondicionIvaId;
    cliente.VendedorId = clienteActualizado.VendedorId;
    cliente.ZonaVentaId = clienteActualizado.ZonaVentaId;
    cliente.PorcentajeDescuento = clienteActualizado.PorcentajeDescuento;
    cliente.LimiteCredito = clienteActualizado.LimiteCredito;
    cliente.Observaciones = clienteActualizado.Observaciones;
    
    await db.SaveChangesAsync();
    
    return Results.Ok(cliente);
});

// DELETE /api/clientes/{id} - Eliminar (soft delete)
app.MapDelete("/api/clientes/{id}", async (int id, SPCDbContext db) =>
{
    var cliente = await db.Clientes.FindAsync(id);
    
    if (cliente == null)
        return Results.NotFound(new { error = "Cliente no encontrado" });
    
    // Soft delete - no eliminamos, marcamos como inactivo
    cliente.Activo = false;
    await db.SaveChangesAsync();
    
    return Results.NoContent();
});


// =============================================
// ENDPOINTS - PRODUCTOS
// =============================================

app.MapGet("/api/productos", async (SPCDbContext db) =>
{
    var productos = await db.Productos
        .Include(p => p.Rubro)
        .Include(p => p.UnidadMedida)
        .Where(p => p.Activo)
        .OrderBy(p => p.Descripcion)
        .ToListAsync();
    
    return Results.Ok(productos);
});

app.MapGet("/api/productos/{id}", async (int id, SPCDbContext db) =>
{
    var producto = await db.Productos
        .Include(p => p.Rubro)
        .Include(p => p.UnidadMedida)
        .FirstOrDefaultAsync(p => p.Id == id);
    
    return producto != null 
        ? Results.Ok(producto) 
        : Results.NotFound(new { error = "Producto no encontrado" });
});

app.MapGet("/api/productos/buscar", async (string? descripcion, SPCDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(descripcion))
        return Results.BadRequest(new { error = "Debe proporcionar una descripción" });
    
    var productos = await db.Productos
        .Include(p => p.Rubro)
        .Where(p => p.Activo && 
               (p.Descripcion.Contains(descripcion) || p.Codigo.Contains(descripcion)))
        .OrderBy(p => p.Descripcion)
        .ToListAsync();
    
    return Results.Ok(productos);
});

app.MapPost("/api/productos", async (Producto producto, SPCDbContext db) =>
{
    producto.Activo = true;
    db.Productos.Add(producto);
    await db.SaveChangesAsync();
    return Results.Created($"/api/productos/{producto.Id}", producto);
});


// =============================================
// ENDPOINTS - TABLAS AUXILIARES
// =============================================

// Condiciones IVA
app.MapGet("/api/condicionesiva", async (SPCDbContext db) =>
{
    var condiciones = await db.CondicionesIva.ToListAsync();
    return Results.Ok(condiciones);
});

// Vendedores
app.MapGet("/api/vendedores", async (SPCDbContext db) =>
{
    var vendedores = await db.Vendedores
        .Where(v => v.Activo)
        .OrderBy(v => v.Nombre)
        .ToListAsync();
    return Results.Ok(vendedores);
});

app.MapPost("/api/vendedores", async (Vendedor vendedor, SPCDbContext db) =>
{
    vendedor.Activo = true;
    db.Vendedores.Add(vendedor);
    await db.SaveChangesAsync();
    return Results.Created($"/api/vendedores/{vendedor.Id}", vendedor);
});

// Zonas de Venta
app.MapGet("/api/zonasventas", async (SPCDbContext db) =>
{
    var zonas = await db.ZonasVenta
        .Where(z => z.Activa)
        .OrderBy(z => z.Nombre)
        .ToListAsync();
    return Results.Ok(zonas);
});

// Rubros
app.MapGet("/api/rubros", async (SPCDbContext db) =>
{
    var rubros = await db.Rubros
        .Where(r => r.Activo)
        .OrderBy(r => r.Nombre)
        .ToListAsync();
    return Results.Ok(rubros);
});

// Depósitos
app.MapGet("/api/depositos", async (SPCDbContext db) =>
{
    var depositos = await db.Depositos
        .Where(d => d.Activo)
        .OrderBy(d => d.Nombre)
        .ToListAsync();
    return Results.Ok(depositos);
});


app.Run();

// Make Program accessible to integration tests
public partial class Program { }
