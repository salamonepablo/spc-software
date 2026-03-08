// =============================================
// SPC - Sistema de Gestión Comercial
// API REST con ASP.NET Core
// =============================================

using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.API.Endpoints;
using SPC.API.Services;
using SPC.Shared.Licensing;
using SPC.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON to ignore circular references
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// Configure Licensing (feature flags)
builder.Services.Configure<LicensingOptions>(
    builder.Configuration.GetSection(LicensingOptions.SectionName));
builder.Services.AddSingleton<ILicenseService, LicenseService>();

// Configure Entity Framework with SQL Server (LocalDB in development)
builder.Services.AddDbContext<SPCDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register business services
builder.Services.AddScoped<IClientesService, ClientesService>();
builder.Services.AddScoped<IProductosService, ProductosService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IFacturasService, FacturasService>();
builder.Services.AddScoped<IPresupuestosService, PresupuestosService>();
builder.Services.AddScoped<INotasCreditoService, NotasCreditoService>();
builder.Services.AddScoped<INotasDebitoService, NotasDebitoService>();
builder.Services.AddScoped<ITaxConfigurationService, TaxConfigurationService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<ICompanySettingsService, CompanySettingsService>();

// Enable CORS for Blazor to consume the API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Swagger/OpenAPI for documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// Enable Swagger UI in development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "SPC API v1");
        options.RoutePrefix = "swagger";
    });
}

// Apply pending migrations automatically (only for relational databases)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SPCDbContext>();
    db.Database.Migrate();
}

app.UseCors("AllowBlazor");

// =============================================
// ROOT ENDPOINT
// =============================================
app.MapGet("/", (ILicenseService license) => new 
{ 
    sistema = "SPC - Sistema de Gestion Comercial",
    version = "1.0",
    license = license.GetLicenseInfo().Tier,
    endpoints = new[] 
    { 
        "/api/clientes", 
        "/api/productos", 
        "/api/stock", 
        "/api/facturas",
        "/api/presupuestos",
        "/api/notas-credito",
        "/api/notas-debito",
        "/api/condicionesiva", 
        "/api/license" 
    }
});

// =============================================
// LICENSE ENDPOINT
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
// BUSINESS ENDPOINTS (modular)
// =============================================
app.MapClientesEndpoints();
app.MapProductosEndpoints();
app.MapStockEndpoints();
app.MapFacturasEndpoints();
app.MapPresupuestosEndpoints();
app.MapNotasCreditoEndpoints();
app.MapNotasDebitoEndpoints();
app.MapSucursalesEndpoints();

// =============================================
// AUXILIARY TABLE ENDPOINTS
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
