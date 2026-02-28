using Microsoft.EntityFrameworkCore;
using SPC.Shared.Models;

namespace SPC.API.Data;

/// <summary>
/// DbContext principal de SPC
/// Equivalente a la conexión con Access en VB6
/// </summary>
public class SPCDbContext : DbContext
{
    public SPCDbContext(DbContextOptions<SPCDbContext> options) : base(options)
    {
    }
    
    // Tablas principales
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<FacturaDetalle> FacturaDetalles => Set<FacturaDetalle>();
    public DbSet<Remito> Remitos => Set<Remito>();
    public DbSet<RemitoDetalle> RemitoDetalles => Set<RemitoDetalle>();
    
    // Tablas auxiliares
    public DbSet<CondicionIva> CondicionesIva => Set<CondicionIva>();
    public DbSet<Vendedor> Vendedores => Set<Vendedor>();
    public DbSet<ZonaVenta> ZonasVenta => Set<ZonaVenta>();
    public DbSet<Rubro> Rubros => Set<Rubro>();
    public DbSet<UnidadMedida> UnidadesMedida => Set<UnidadMedida>();
    public DbSet<Deposito> Depositos => Set<Deposito>();
    public DbSet<Stock> Stocks => Set<Stock>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Índice único para Stock (Producto + Depósito)
        modelBuilder.Entity<Stock>()
            .HasIndex(s => new { s.ProductoId, s.DepositoId })
            .IsUnique();
        
        // Índice único para Factura (Tipo + Número)
        modelBuilder.Entity<Factura>()
            .HasIndex(f => new { f.TipoFactura, f.PuntoVenta, f.NumeroFactura })
            .IsUnique();
        
        // Datos iniciales - Condiciones IVA
        modelBuilder.Entity<CondicionIva>().HasData(
            new CondicionIva { Id = 1, Codigo = "RI", Descripcion = "Responsable Inscripto", TipoFactura = "A" },
            new CondicionIva { Id = 2, Codigo = "MO", Descripcion = "Monotributo", TipoFactura = "B" },
            new CondicionIva { Id = 3, Codigo = "CF", Descripcion = "Consumidor Final", TipoFactura = "B" },
            new CondicionIva { Id = 4, Codigo = "EX", Descripcion = "Exento", TipoFactura = "B" }
        );
        
        // Datos iniciales - Unidades de Medida
        modelBuilder.Entity<UnidadMedida>().HasData(
            new UnidadMedida { Id = 1, Codigo = "UN", Nombre = "Unidades" },
            new UnidadMedida { Id = 2, Codigo = "CJ", Nombre = "Cajas" }
        );
        
        // Datos iniciales - Depósito principal
        modelBuilder.Entity<Deposito>().HasData(
            new Deposito { Id = 1, Nombre = "Depósito Principal", Activo = true }
        );
        
        // Datos iniciales - Rubros
        modelBuilder.Entity<Rubro>().HasData(
            new Rubro { Id = 1, Nombre = "Baterías Auto", Activo = true },
            new Rubro { Id = 2, Nombre = "Baterías Moto", Activo = true },
            new Rubro { Id = 3, Nombre = "Baterías Camión", Activo = true },
            new Rubro { Id = 4, Nombre = "Accesorios", Activo = true }
        );
    }
}
