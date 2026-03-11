using Microsoft.EntityFrameworkCore;
using SPC.Shared.Models;

namespace SPC.API.Data;

/// <summary>
/// DbContext principal de SPC.
/// Equivalente a la conexion con Access en VB6.
/// </summary>
public class SPCDbContext : DbContext
{
    public SPCDbContext(DbContextOptions<SPCDbContext> options) : base(options)
    {
    }
    
    // ===========================================
    // ENTIDADES PRINCIPALES
    // ===========================================
    
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<Product> Products => Set<Product>();
    
    // Documentos Billing (Linea 1 - Fiscales)
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceDetail> InvoiceDetails => Set<InvoiceDetail>();
    public DbSet<CreditNote> CreditNotes => Set<CreditNote>();
    public DbSet<CreditNoteDetail> CreditNoteDetails => Set<CreditNoteDetail>();
    public DbSet<DebitNote> DebitNotes => Set<DebitNote>();
    public DbSet<DebitNoteDetail> DebitNoteDetails => Set<DebitNoteDetail>();
    
    // Documentos Budget (Linea 2 - Internos)
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteDetail> QuoteDetails => Set<QuoteDetail>();
    public DbSet<InternalDebitNote> InternalDebitNotes => Set<InternalDebitNote>();
    public DbSet<InternalDebitNoteDetail> InternalDebitNoteDetails => Set<InternalDebitNoteDetail>();
    
    // DeliveryNotes
    public DbSet<DeliveryNote> DeliveryNotes => Set<DeliveryNote>();
    public DbSet<DeliveryNoteDetail> DeliveryNoteDetails => Set<DeliveryNoteDetail>();
    public DbSet<CasualDeliveryNote> CasualDeliveryNotes => Set<CasualDeliveryNote>();
    public DbSet<CasualDeliveryNoteDetail> CasualDeliveryNoteDetails => Set<CasualDeliveryNoteDetail>();
    
    // Pagos
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentDetail> PaymentDetails => Set<PaymentDetail>();
    
    // Cuenta Corriente
    public DbSet<CurrentAccount> CurrentAccounts => Set<CurrentAccount>();
    public DbSet<CurrentAccountMovement> CurrentAccountMovements => Set<CurrentAccountMovement>();
    
    // Consignaciones
    public DbSet<Consignment> Consignments => Set<Consignment>();
    public DbSet<ConsignmentDetail> ConsignmentDetails => Set<ConsignmentDetail>();
    
    // Stock
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockMovementDetail> StockMovementDetails => Set<StockMovementDetail>();
    
    // ===========================================
    // TABLAS AUXILIARES
    // ===========================================
    
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<TaxCondition> CondicionesIva => Set<TaxCondition>();
    public DbSet<SalesRep> SalesRepes => Set<SalesRep>();
    public DbSet<SalesZone> ZonasVenta => Set<SalesZone>();
    public DbSet<Category> Categorys => Set<Category>();
    public DbSet<UnitOfMeasure> UnidadesMedida => Set<UnitOfMeasure>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<TaxSetting> TaxSettings => Set<TaxSetting>();
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ===========================================
        // CONFIGURACION DE PRECISION DECIMALES
        // ===========================================
        
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(c => c.LimiteCredito).HasPrecision(18, 2);
            entity.Property(c => c.PorcentajeDescuento).HasPrecision(5, 2);
            entity.Property(c => c.AlicuotaIIBB).HasPrecision(5, 2);
        });

        modelBuilder.Entity<SalesRep>(entity =>
        {
            entity.Property(v => v.PorcentajeComision).HasPrecision(5, 2);
            entity.HasIndex(v => v.Legajo).IsUnique();
        });

        // Invoices
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.Property(f => f.PorcentajeIVA).HasPrecision(5, 2);
            entity.Property(f => f.AlicuotaIIBB).HasPrecision(5, 2);
            entity.Property(f => f.PorcentajeDescuento).HasPrecision(5, 2);
            entity.Property(f => f.ImporteDescuento).HasPrecision(18, 2);
        });

        // Quotes
        modelBuilder.Entity<Quote>(entity =>
        {
            entity.Property(q => q.Subtotal).HasPrecision(18, 2);
            entity.Property(q => q.DiscountPercent).HasPrecision(5, 2);
            entity.Property(q => q.DiscountAmount).HasPrecision(18, 2);
            entity.Property(q => q.Total).HasPrecision(18, 2);
        });
        
        modelBuilder.Entity<QuoteDetail>(entity =>
        {
            entity.Property(d => d.Quantity).HasPrecision(18, 2);
            entity.Property(d => d.UnitPrice).HasPrecision(18, 2);
            entity.Property(d => d.DiscountPercent).HasPrecision(5, 2);
            entity.Property(d => d.DiscountAmount).HasPrecision(18, 2);
            entity.Property(d => d.Subtotal).HasPrecision(18, 2);
        });

        // Credit Notes
        modelBuilder.Entity<CreditNote>(entity =>
        {
            entity.Property(c => c.Subtotal).HasPrecision(18, 2);
            entity.Property(c => c.VATPercent).HasPrecision(5, 2);
            entity.Property(c => c.VATAmount).HasPrecision(18, 2);
            entity.Property(c => c.IIBBPercent).HasPrecision(5, 2);
            entity.Property(c => c.IIBBAmount).HasPrecision(18, 2);
            entity.Property(c => c.DiscountPercent).HasPrecision(5, 2);
            entity.Property(c => c.DiscountAmount).HasPrecision(18, 2);
            entity.Property(c => c.Total).HasPrecision(18, 2);
        });

        modelBuilder.Entity<CreditNoteDetail>(entity =>
        {
            entity.Property(d => d.Quantity).HasPrecision(18, 2);
            entity.Property(d => d.UnitPrice).HasPrecision(18, 2);
            entity.Property(d => d.DiscountPercent).HasPrecision(5, 2);
            entity.Property(d => d.DiscountAmount).HasPrecision(18, 2);
            entity.Property(d => d.Subtotal).HasPrecision(18, 2);
        });

        // Debit Notes
        modelBuilder.Entity<DebitNote>(entity =>
        {
            entity.Property(d => d.Subtotal).HasPrecision(18, 2);
            entity.Property(d => d.VATPercent).HasPrecision(5, 2);
            entity.Property(d => d.VATAmount).HasPrecision(18, 2);
            entity.Property(d => d.IIBBPercent).HasPrecision(5, 2);
            entity.Property(d => d.IIBBAmount).HasPrecision(18, 2);
            entity.Property(d => d.DiscountPercent).HasPrecision(5, 2);
            entity.Property(d => d.DiscountAmount).HasPrecision(18, 2);
            entity.Property(d => d.Total).HasPrecision(18, 2);
        });

        modelBuilder.Entity<DebitNoteDetail>(entity =>
        {
            entity.Property(d => d.Quantity).HasPrecision(18, 2);
            entity.Property(d => d.UnitPrice).HasPrecision(18, 2);
            entity.Property(d => d.DiscountPercent).HasPrecision(5, 2);
            entity.Property(d => d.DiscountAmount).HasPrecision(18, 2);
            entity.Property(d => d.Subtotal).HasPrecision(18, 2);
        });

        // Internal Debit Notes
        modelBuilder.Entity<InternalDebitNote>(entity =>
        {
            entity.Property(d => d.Subtotal).HasPrecision(18, 2);
            entity.Property(d => d.DiscountPercent).HasPrecision(5, 2);
            entity.Property(d => d.DiscountAmount).HasPrecision(18, 2);
            entity.Property(d => d.Total).HasPrecision(18, 2);
        });

        modelBuilder.Entity<InternalDebitNoteDetail>(entity =>
        {
            entity.Property(d => d.Quantity).HasPrecision(18, 2);
            entity.Property(d => d.UnitPrice).HasPrecision(18, 2);
            entity.Property(d => d.DiscountPercent).HasPrecision(5, 2);
            entity.Property(d => d.DiscountAmount).HasPrecision(18, 2);
            entity.Property(d => d.Subtotal).HasPrecision(18, 2);
        });

        // Payments
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(p => p.TotalAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<PaymentDetail>(entity =>
        {
            entity.Property(d => d.Amount).HasPrecision(18, 2);
        });

        // Current Account
        modelBuilder.Entity<CurrentAccount>(entity =>
        {
            entity.Property(c => c.BillingBalance).HasPrecision(18, 2);
            entity.Property(c => c.BudgetBalance).HasPrecision(18, 2);
            entity.Property(c => c.TotalBalance).HasPrecision(18, 2);
            entity.HasIndex(c => c.CustomerId).IsUnique();
        });

        modelBuilder.Entity<CurrentAccountMovement>(entity =>
        {
            entity.Property(m => m.BillingAmount).HasPrecision(18, 2);
            entity.Property(m => m.BudgetAmount).HasPrecision(18, 2);
            entity.Property(m => m.BillingRunningBalance).HasPrecision(18, 2);
            entity.Property(m => m.BudgetRunningBalance).HasPrecision(18, 2);
        });

        // Consignments
        modelBuilder.Entity<ConsignmentDetail>(entity =>
        {
            entity.Property(d => d.Quantity).HasPrecision(18, 2);
        });

        // Stock
        modelBuilder.Entity<StockMovementDetail>(entity =>
        {
            entity.Property(d => d.Quantity).HasPrecision(18, 2);
        });

        // DeliveryNote details
        modelBuilder.Entity<DeliveryNoteDetail>(entity =>
        {
            entity.Property(d => d.Cantidad).HasPrecision(18, 2);
        });

        modelBuilder.Entity<CasualDeliveryNoteDetail>(entity =>
        {
            entity.Property(d => d.Quantity).HasPrecision(18, 2);
        });

        // ===========================================
        // INDICES UNICOS
        // ===========================================
        
        modelBuilder.Entity<Stock>()
            .HasIndex(s => new { s.ProductId, s.WarehouseId })
            .IsUnique();
        
        modelBuilder.Entity<Invoice>()
            .HasIndex(f => new { f.TipoInvoice, f.PuntoVenta, f.NumeroInvoice })
            .IsUnique();

        modelBuilder.Entity<Quote>()
            .HasIndex(q => new { q.BranchId, q.QuoteNumber })
            .IsUnique();

        modelBuilder.Entity<CreditNote>()
            .HasIndex(c => new { c.VoucherType, c.PointOfSale, c.CreditNoteNumber })
            .IsUnique();

        modelBuilder.Entity<DebitNote>()
            .HasIndex(d => new { d.VoucherType, d.PointOfSale, d.DebitNoteNumber })
            .IsUnique();

        modelBuilder.Entity<InternalDebitNote>()
            .HasIndex(d => new { d.VoucherType, d.BranchId, d.InternalDebitNumber })
            .IsUnique();

        modelBuilder.Entity<Payment>()
            .HasIndex(p => new { p.BranchId, p.PaymentNumber })
            .IsUnique();

        modelBuilder.Entity<DeliveryNote>()
            .HasIndex(r => new { r.BranchId, r.NumeroDeliveryNote })
            .IsUnique();

        modelBuilder.Entity<Branch>()
            .HasIndex(b => b.Code)
            .IsUnique();

        modelBuilder.Entity<PaymentMethod>()
            .HasIndex(p => p.Code)
            .IsUnique();

        modelBuilder.Entity<TaxSetting>(entity =>
        {
            entity.Property(t => t.Rate).HasPrecision(5, 2);
            entity.HasIndex(t => new { t.TaxCode, t.EffectiveFrom }).IsUnique();
        });

        // ===========================================
        // RELACIONES
        // ===========================================
        
        // DeliveryNote -> Invoice (un remito pertenece a una factura)
        // Restrict to avoid cascade path conflict: Customer -> Invoice -> DeliveryNote vs Customer -> DeliveryNote
        modelBuilder.Entity<Invoice>()
            .HasOne(f => f.Customer)
            .WithMany(c => c.Invoices)
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Invoice>()
            .HasOne(f => f.SalesRep)
            .WithMany()
            .HasForeignKey(f => f.SalesRepId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InvoiceDetail>()
            .HasOne(d => d.Invoice)
            .WithMany(f => f.Detalles)
            .HasForeignKey(d => d.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InvoiceDetail>()
            .HasOne(d => d.Product)
            .WithMany()
            .HasForeignKey(d => d.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DeliveryNote>()
            .HasOne(r => r.Invoice)
            .WithMany(f => f.DeliveryNotes)
            .HasForeignKey(r => r.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // CreditNote -> Invoice (NC se emite contra una factura)
        modelBuilder.Entity<CreditNote>()
            .HasOne(c => c.Invoice)
            .WithMany(f => f.CreditNotes)
            .HasForeignKey(c => c.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // StockMovement -> Warehouses (evitar cascade multiple)
        modelBuilder.Entity<StockMovement>()
            .HasOne(s => s.SourceWarehouse)
            .WithMany()
            .HasForeignKey(s => s.SourceWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockMovement>()
            .HasOne(s => s.DestinationWarehouse)
            .WithMany()
            .HasForeignKey(s => s.DestinationWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // ===========================================
        // DATOS INICIALES (SEED)
        // ===========================================
        
        // Condiciones IVA
        modelBuilder.Entity<TaxCondition>().HasData(
            new TaxCondition { Id = 1, Codigo = "RI", Descripcion = "Responsable Inscripto", TipoInvoice = "A" },
            new TaxCondition { Id = 2, Codigo = "MO", Descripcion = "Monotributo", TipoInvoice = "B" },
            new TaxCondition { Id = 3, Codigo = "CF", Descripcion = "Consumidor Final", TipoInvoice = "B" },
            new TaxCondition { Id = 4, Codigo = "EX", Descripcion = "Exento", TipoInvoice = "B" }
        );
        
        // Unidades de Medida
        modelBuilder.Entity<UnitOfMeasure>().HasData(
            new UnitOfMeasure { Id = 1, Codigo = "UN", Nombre = "Unidades" },
            new UnitOfMeasure { Id = 2, Codigo = "CJ", Nombre = "Cajas" }
        );
        
        // Warehouse principal
        modelBuilder.Entity<Warehouse>().HasData(
            new Warehouse { Id = 1, Nombre = "Warehouse Principal", Activo = true }
        );
        
        // Categorys
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Nombre = "Baterias Auto", Activo = true },
            new Category { Id = 2, Nombre = "Baterias Moto", Activo = true },
            new Category { Id = 3, Nombre = "Baterias Camion", Activo = true },
            new Category { Id = 4, Nombre = "Accesorios", Activo = true }
        );

        // Branches
        modelBuilder.Entity<Branch>().HasData(
            new Branch { Id = 1, Code = "CALLE", Name = "Calle (SalesRepes)", PointOfSale = 2, IsActive = true },
            new Branch { Id = 2, Code = "DISTRIB", Name = "Distribuidora (Oficina)", PointOfSale = 5, IsActive = true }
        );

        // Formas de Pago
        modelBuilder.Entity<PaymentMethod>().HasData(
            new PaymentMethod { Id = 1, Code = "EF", Description = "Efectivo", Type = PaymentMethodType.Cash, IsActive = true },
            new PaymentMethod { Id = 2, Code = "CH", Description = "Cheque", Type = PaymentMethodType.Check, RequiresDetail = true, IsActive = true },
            new PaymentMethod { Id = 3, Code = "TR", Description = "Transferencia", Type = PaymentMethodType.Transfer, IsActive = true },
            new PaymentMethod { Id = 4, Code = "TC", Description = "Tarjeta de Credito", Type = PaymentMethodType.Card, IsActive = true },
            new PaymentMethod { Id = 5, Code = "TD", Description = "Tarjeta de Debito", Type = PaymentMethodType.Card, IsActive = true },
            new PaymentMethod { Id = 6, Code = "RZ", Description = "Rezago (Baterias usadas)", Type = PaymentMethodType.Barter, RequiresDetail = true, IsActive = true },
            new PaymentMethod { Id = 7, Code = "ME", Description = "Mercaderia (Canje)", Type = PaymentMethodType.Barter, RequiresDetail = true, IsActive = true }
        );

        // Tax Settings (IVA y percepciones)
        modelBuilder.Entity<TaxSetting>().HasData(
            new TaxSetting { Id = 1, TaxCode = "VAT", Description = "IVA General", Rate = 21.00m, IsDefault = true, IsActive = true, EffectiveFrom = new DateTime(2000, 1, 1) },
            new TaxSetting { Id = 2, TaxCode = "VAT_REDUCED", Description = "IVA Reducido", Rate = 10.50m, IsDefault = false, IsActive = true, EffectiveFrom = new DateTime(2000, 1, 1) },
            new TaxSetting { Id = 3, TaxCode = "VAT_EXEMPT", Description = "IVA Exento", Rate = 0.00m, IsDefault = false, IsActive = true, EffectiveFrom = new DateTime(2000, 1, 1) },
            new TaxSetting { Id = 4, TaxCode = "IIBB_BA", Description = "IIBB Buenos Aires", Rate = 3.00m, IsDefault = false, IsActive = true, EffectiveFrom = new DateTime(2000, 1, 1) },
            new TaxSetting { Id = 5, TaxCode = "IIBB_CABA", Description = "IIBB CABA", Rate = 3.00m, IsDefault = false, IsActive = true, EffectiveFrom = new DateTime(2000, 1, 1) }
        );
    }
}
