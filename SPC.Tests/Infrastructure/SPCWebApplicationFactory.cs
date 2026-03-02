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
                new CondicionIva { Id = 1, Codigo = "RI", Descripcion = "Responsable Inscripto", TipoFactura = "A" },
                new CondicionIva { Id = 2, Codigo = "MO", Descripcion = "Monotributo", TipoFactura = "B" },
                new CondicionIva { Id = 3, Codigo = "CF", Descripcion = "Consumidor Final", TipoFactura = "B" },
                new CondicionIva { Id = 4, Codigo = "EX", Descripcion = "Exento", TipoFactura = "B" }
            );
        }
        
        if (!db.UnidadesMedida.Any())
        {
            db.UnidadesMedida.AddRange(
                new UnidadMedida { Id = 1, Codigo = "UN", Nombre = "Unidades" },
                new UnidadMedida { Id = 2, Codigo = "CJ", Nombre = "Cajas" }
            );
        }
        
        if (!db.Depositos.Any())
        {
            db.Depositos.Add(new Deposito { Id = 1, Nombre = "Deposito Principal", Activo = true });
        }
        
        if (!db.Rubros.Any())
        {
            db.Rubros.AddRange(
                new Rubro { Id = 1, Nombre = "Baterias Auto", Activo = true },
                new Rubro { Id = 2, Nombre = "Baterias Moto", Activo = true },
                new Rubro { Id = 3, Nombre = "Baterias Camion", Activo = true },
                new Rubro { Id = 4, Nombre = "Accesorios", Activo = true }
            );
        }
        
        if (!db.Branches.Any())
        {
            db.Branches.AddRange(
                new Branch { Id = 1, Code = "CALLE", Name = "Calle (Vendedores)", PointOfSale = 2, IsActive = true },
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
        
        db.SaveChanges();
    }
}
