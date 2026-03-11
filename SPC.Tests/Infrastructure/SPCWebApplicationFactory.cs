using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// Uses InMemory database instead of SQL Server.
/// Each test class gets a fresh database instance.
/// </summary>
public class SPCWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"SPCTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove ALL DbContext-related registrations
            services.RemoveAll(typeof(DbContextOptions<SPCDbContext>));
            services.RemoveAll(typeof(DbContextOptions));
            services.RemoveAll(typeof(SPCDbContext));
            
            // Remove all DbContext registrations by scanning for the service type
            var dbContextDescriptors = services
                .Where(d => d.ServiceType == typeof(SPCDbContext) || 
                           d.ServiceType == typeof(DbContextOptions<SPCDbContext>) ||
                           d.ServiceType.FullName?.Contains("DbContextOptions") == true)
                .ToList();
            
            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            // Add InMemory database for testing with a unique name per factory instance
            services.AddDbContext<SPCDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });
        });

        builder.UseEnvironment("Testing");
    }
    
    /// <summary>
    /// Creates the host and ensures database is set up with seed data.
    /// </summary>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        
        // Ensure database is created and seed data is added
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SPCDbContext>();
        
        // EnsureCreated triggers OnModelCreating which includes HasData seed
        db.Database.EnsureCreated();
        
        // For InMemory database, we need to manually add seed data
        // because HasData only works with migrations
        SeedTestData(db);
        
        return host;
    }
    
    /// <summary>
    /// Seeds the test database with initial data.
    /// InMemory database doesn't run HasData, so we do it manually.
    /// </summary>
    private static void SeedTestData(SPCDbContext db)
    {
        // Only seed if empty (avoid duplicates)
        if (!db.CondicionesIva.Any())
        {
            db.CondicionesIva.AddRange(
                new TaxCondition { Id = 1, Codigo = "RI", Descripcion = "Responsable Inscripto", TipoInvoice = "A" },
                new TaxCondition { Id = 2, Codigo = "MO", Descripcion = "Monotributo", TipoInvoice = "B" },
                new TaxCondition { Id = 3, Codigo = "CF", Descripcion = "Consumidor Final", TipoInvoice = "B" },
                new TaxCondition { Id = 4, Codigo = "EX", Descripcion = "Exento", TipoInvoice = "B" }
            );
        }
        
        if (!db.UnidadesMedida.Any())
        {
            db.UnidadesMedida.AddRange(
                new UnitOfMeasure { Id = 1, Codigo = "UN", Nombre = "Unidades" },
                new UnitOfMeasure { Id = 2, Codigo = "CJ", Nombre = "Cajas" }
            );
        }
        
        if (!db.Warehouses.Any())
        {
            db.Warehouses.Add(new Warehouse { Id = 1, Nombre = "Warehouse Principal", Activo = true });
        }
        
        if (!db.Categorys.Any())
        {
            db.Categorys.AddRange(
                new Category { Id = 1, Nombre = "Baterias Auto", Activo = true },
                new Category { Id = 2, Nombre = "Baterias Moto", Activo = true },
                new Category { Id = 3, Nombre = "Baterias Camion", Activo = true },
                new Category { Id = 4, Nombre = "Accesorios", Activo = true }
            );
        }
        
        if (!db.Branches.Any())
        {
            db.Branches.AddRange(
                new Branch { Id = 1, Code = "CALLE", Name = "Calle (SalesRepes)", PointOfSale = 2, IsActive = true },
                new Branch { Id = 2, Code = "DISTRIB", Name = "Distribuidora (Oficina)", PointOfSale = 5, IsActive = true }
            );
        }
        
        if (!db.PaymentMethods.Any())
        {
            db.PaymentMethods.AddRange(
                new PaymentMethod { Id = 1, Code = "EF", Description = "Efectivo", Type = PaymentMethodType.Cash, IsActive = true },
                new PaymentMethod { Id = 2, Code = "CH", Description = "Cheque", Type = PaymentMethodType.Check, RequiresDetail = true, IsActive = true },
                new PaymentMethod { Id = 3, Code = "TR", Description = "Transferencia", Type = PaymentMethodType.Transfer, IsActive = true },
                new PaymentMethod { Id = 4, Code = "TC", Description = "Tarjeta de Credito", Type = PaymentMethodType.Card, IsActive = true },
                new PaymentMethod { Id = 5, Code = "TD", Description = "Tarjeta de Debito", Type = PaymentMethodType.Card, IsActive = true },
                new PaymentMethod { Id = 6, Code = "RZ", Description = "Rezago (Baterias usadas)", Type = PaymentMethodType.Barter, RequiresDetail = true, IsActive = true },
                new PaymentMethod { Id = 7, Code = "ME", Description = "Mercaderia (Canje)", Type = PaymentMethodType.Barter, RequiresDetail = true, IsActive = true }
            );
        }
        
        // Tax Settings (for tax configuration service)
        if (!db.TaxSettings.Any())
        {
            db.TaxSettings.AddRange(
                new TaxSetting { Id = 1, TaxCode = "VAT", Description = "IVA General", Rate = 21.00m, IsDefault = true, IsActive = true, EffectiveFrom = new DateTime(2000, 1, 1) },
                new TaxSetting { Id = 2, TaxCode = "VAT_REDUCED", Description = "IVA Reducido", Rate = 10.50m, IsDefault = false, IsActive = true, EffectiveFrom = new DateTime(2000, 1, 1) },
                new TaxSetting { Id = 3, TaxCode = "VAT_EXEMPT", Description = "IVA Exento", Rate = 0.00m, IsDefault = false, IsActive = true, EffectiveFrom = new DateTime(2000, 1, 1) },
                new TaxSetting { Id = 4, TaxCode = "IIBB_BA", Description = "IIBB Buenos Aires", Rate = 3.00m, IsDefault = false, IsActive = true, EffectiveFrom = new DateTime(2000, 1, 1) }
            );
        }
        
        // Company Settings (for IIBB/IVA agent status)
        if (!db.CompanySettings.Any())
        {
            db.CompanySettings.Add(new CompanySettings
            {
                Id = 1,
                CompanyName = "SPC Baterias",
                CUIT = "30-70843254-3",
                IsIIBBPerceptionAgent = true,  // Company is ARBA perception agent
                IsIVAWithholdingAgent = false,
                IIBBProvince = "Buenos Aires",
                IIBBRegistrationNumber = "30708432543",
                FiscalActivityStartDate = new DateTime(2020, 1, 1),
                IsActive = true
            });
        }
        
        // Add a test customer with discount for testing
        if (!db.Customers.Any())
        {
            db.Customers.Add(new Customer 
            { 
                Id = 1, 
                RazonSocial = "Customer Test", 
                CUIT = "20-12345678-9",
                TaxConditionId = 1, 
                PorcentajeDescuento = 10m, // 10% default discount
                LimiteCredito = 50000m,
                Activo = true,
                FechaAlta = DateTime.Now
            });
        }
        
        // Add test products with dual pricing
        if (!db.Products.Any())
        {
            db.Products.AddRange(
                new Product 
                { 
                    Id = 1, 
                    Codigo = "BAT001", 
                    Descripcion = "Bateria 12V 65AH", 
                    PrecioInvoice = 1000m,  // Invoice price (without VAT)
                    PrecioQuote = 1210m,  // Quote price (with VAT included)
                    PrecioVenta = 1000m,
                    PorcentajeIVA = 21m,
                    CategoryId = 1,
                    Activo = true 
                },
                new Product 
                { 
                    Id = 2, 
                    Codigo = "BAT002", 
                    Descripcion = "Bateria 12V 75AH", 
                    PrecioInvoice = 1500m,
                    PrecioQuote = 1815m,
                    PrecioVenta = 1500m,
                    PorcentajeIVA = 21m,
                    CategoryId = 1,
                    Activo = true 
                }
            );
        }
        
        db.SaveChanges();
    }
}
