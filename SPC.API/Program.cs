// =============================================
// SPC - Sistema de Gestión Comercial
// API REST con ASP.NET Core
// =============================================

using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.API.Endpoints;
using SPC.API.Services;
using SPC.Shared.Licensing;

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
// PENDING: Add [Column] attributes to all entities for ES→EN mapping OR rename DB columns to English
builder.Services.AddDbContext<SPCDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// Register business services
builder.Services.AddScoped<ICustomersService, CustomersService>();
builder.Services.AddScoped<IProductsService, ProductsService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IInvoiceQueryService, InvoiceQueryService>();
builder.Services.AddScoped<IInvoiceCommandService, InvoiceCommandService>();
builder.Services.AddScoped<IQuoteQueryService, QuoteQueryService>();
builder.Services.AddScoped<IQuoteCommandService, QuoteCommandService>();
builder.Services.AddScoped<ICreditNoteQueryService, CreditNoteQueryService>();
builder.Services.AddScoped<ICreditNoteCommandService, CreditNoteCommandService>();
builder.Services.AddScoped<IDebitNoteQueryService, DebitNoteQueryService>();
builder.Services.AddScoped<IDebitNoteCommandService, DebitNoteCommandService>();
builder.Services.AddScoped<ITaxConfigurationService, TaxConfigurationService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<ICompanySettingsService, CompanySettingsService>();
builder.Services.AddScoped<ICurrentAccountService, CurrentAccountService>();
builder.Services.AddScoped<IAuxiliaryTablesService, AuxiliaryTablesService>();

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
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SPC API", Version = "v1" });
});

var app = builder.Build();

// Enable Swagger UI in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SPC API v1");
        c.RoutePrefix = "swagger";
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
        "/api/invoices",
        "/api/quotes",
        "/api/facturas (legacy)",
        "/api/presupuestos (legacy)",
        "/api/notas-credito",
        "/api/notas-debito",
        "/api/current-accounts",
        "/api/TaxConditions",
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
app.MapCustomersEndpoints();
app.MapProductsEndpoints();
app.MapStockEndpoints();
app.MapInvoicesEndpoints();
app.MapQuotesEndpoints();
app.MapCreditNotesEndpoints();
app.MapDebitNotesEndpoints();
app.MapCurrentAccountEndpoints();
app.MapAuxiliaryTablesEndpoints();


app.Run();

// Make Program accessible to integration tests
public partial class Program { }
