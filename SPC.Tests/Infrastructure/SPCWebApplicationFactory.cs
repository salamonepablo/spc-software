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
        if (!db.TaxConditions.Any())
        {
            db.TaxConditions.AddRange(
                new TaxCondition { Id = 1, Code = "RI", Description = "Responsable Inscripto", InvoiceType = "A" },
                new TaxCondition { Id = 2, Code = "MO", Description = "Monotributo", InvoiceType = "B" },
                new TaxCondition { Id = 3, Code = "CF", Description = "Consumidor Final", InvoiceType = "B" },
                new TaxCondition { Id = 4, Code = "EX", Description = "Exento", InvoiceType = "B" }
            );
        }
        
        if (!db.UnitsOfMeasure.Any())
        {
            db.UnitsOfMeasure.AddRange(
                new UnitOfMeasure { Id = 1, Code = "UN", Name = "Unidades" },
                new UnitOfMeasure { Id = 2, Code = "CJ", Name = "Cajas" }
            );
        }
        
        if (!db.Warehouses.Any())
        {
            db.Warehouses.Add(new Warehouse { Id = 1, Name = "Warehouse Principal", IsActive = true });
        }
        
        if (!db.Categories.Any())
        {
            db.Categories.AddRange(
                new Category { Id = 1, Name = "Baterias Auto", IsActive = true },
                new Category { Id = 2, Name = "Baterias Moto", IsActive = true },
                new Category { Id = 3, Name = "Baterias Camion", IsActive = true },
                new Category { Id = 4, Name = "Accesorios", IsActive = true }
            );
        }
        
        if (!db.Branches.Any())
        {
            db.Branches.AddRange(
                new Branch { Id = 1, Code = "CALLE", Name = "Calle (SalesReps)", PointOfSale = 2, IsActive = true },
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
        
        // Add test customers for testing
        if (!db.Customers.Any())
        {
            db.Customers.AddRange(
                new Customer 
                { 
                    Id = 1, 
                    CompanyName = "Customer Test", 
                    CUIT = "20-12345678-9",
                    TaxConditionId = 1, 
                    DiscountPercent = 10m, // 10% default discount
                    CreditLimit = 50000m,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new Customer 
                { 
                    Id = 2, 
                    CompanyName = "Customer Two", 
                    CUIT = "20-22222222-2",
                    TaxConditionId = 1, 
                    DiscountPercent = 0m,
                    CreditLimit = 30000m,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new Customer 
                { 
                    Id = 3, 
                    CompanyName = "Customer Three", 
                    CUIT = "20-33333333-3",
                    TaxConditionId = 2, // Monotributo
                    DiscountPercent = 5m,
                    CreditLimit = 20000m,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new Customer 
                { 
                    Id = 4, 
                    CompanyName = "Customer Four", 
                    CUIT = "20-44444444-4",
                    TaxConditionId = 3, // Consumidor Final
                    DiscountPercent = 0m,
                    CreditLimit = 10000m,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                }
            );
        }
        
        // Add test products with dual pricing
        if (!db.Products.Any())
        {
            db.Products.AddRange(
                new Product 
                { 
                    Id = 1, 
                    Code = "BAT001", 
                    Description = "Bateria 12V 65AH", 
                    InvoicePrice = 1000m,  // Invoice price (without VAT)
                    QuotePrice = 1210m,  // Quote price (with VAT included)
                    SalePrice = 1000m,
                    VATPercent = 21m,
                    CategoryId = 1,
                    IsActive = true 
                },
                new Product 
                { 
                    Id = 2, 
                    Code = "BAT002", 
                    Description = "Bateria 12V 75AH", 
                    InvoicePrice = 1500m,
                    QuotePrice = 1815m,
                    SalePrice = 1500m,
                    VATPercent = 21m,
                    CategoryId = 1,
                    IsActive = true 
                }
            );
        }
        
        db.SaveChanges();
    }
}
