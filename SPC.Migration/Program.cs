using System.Data;
using System.Data.OleDb;
using EFCore.BulkExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.Migration;

/// <summary>
/// Data migration tool: Access -> SQL Server
/// Migrates historical data from the VB6 Access database to the new SQL Server database
/// </summary>
class Program
{
    // Access database path
    private const string AccessDbPath = @"C:\TrabajosActivos\SPC-Core\Db_SPC_SI.mdb";
    
    // SQL Server connection string (same as SPC.API)
    private const string SqlServerConnectionString = 
        "Server=(localdb)\\MSSQLLocalDB;Database=SPC;Trusted_Connection=True;TrustServerCertificate=True;";

    // Lookup dictionaries for FK mapping (Access text PK -> SQL Server int PK)
    private static readonly Dictionary<string, int> VendedorLegajoToId = new();
    private static readonly Dictionary<string, int> ProductoCodigoToId = new();
    private static readonly Dictionary<string, int> DepositoCodigoToId = new();
    private static readonly Dictionary<string, int> CondicionIvaCodigoToId = new();
    private static readonly Dictionary<string, int> RubroCodigoToId = new();
    private static readonly Dictionary<string, int> UnidadMedidaCodigoToId = new();
    private static readonly Dictionary<string, int> PaymentMethodCodigoToId = new();
    private static readonly Dictionary<int, int> ZonaVentaAccessIdToSqlId = new();
    private static readonly Dictionary<int, int> BranchAccessIdToSqlId = new();
    private static readonly Dictionary<int, int> ClienteAccessIdToSqlId = new();
    private static readonly Dictionary<(string tipo, int numero), int> FacturaAccessToSqlId = new();
    private static readonly Dictionary<(int sucursal, int numero), int> RemitoAccessToSqlId = new();

    static async Task Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("  SPC Data Migration: Access -> SQL Server");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        // Verify Access database exists
        if (!File.Exists(AccessDbPath))
        {
            Console.WriteLine($"ERROR: Access database not found at: {AccessDbPath}");
            return;
        }

        Console.WriteLine($"Access DB: {AccessDbPath}");
        Console.WriteLine($"SQL Server: {SqlServerConnectionString}");
        Console.WriteLine();

        // Create DbContext
        var optionsBuilder = new DbContextOptionsBuilder<SPCDbContext>();
        optionsBuilder.UseSqlServer(SqlServerConnectionString);
        
        using var db = new SPCDbContext(optionsBuilder.Options);

        // Verify SQL Server connection
        try
        {
            await db.Database.CanConnectAsync();
            Console.WriteLine("SQL Server connection: OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Cannot connect to SQL Server: {ex.Message}");
            return;
        }

        // Clean database option
        if (args.Any(a => string.Equals(a, "--clean", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine("Cleaning database...");
            Console.WriteLine();

            try
            {
                await CleanDatabaseAsync(db);
                Console.WriteLine();
                Console.WriteLine("===========================================");
                Console.WriteLine("  Database cleanup completed!");
                Console.WriteLine("===========================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR during cleanup: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            return;
        }

        if (args.Any(a => string.Equals(a, "--csv", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine("Running CSV migration...");
            Console.WriteLine();

            try
            {
                await CsvImporter.RunAsync(db);
                Console.WriteLine();
                Console.WriteLine("===========================================");
                Console.WriteLine("  CSV Migration completed successfully!");
                Console.WriteLine("===========================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR during CSV migration: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            return;
        }

        // Build Access connection string (ACE for Office 2007+ or JET for older)
        string accessConnectionString = 
            $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={AccessDbPath};";

        try
        {
            using var accessConn = new OleDbConnection(accessConnectionString);
            await accessConn.OpenAsync();
            Console.WriteLine("Access connection: OK");
            Console.WriteLine();

            // Run migrations in order (respecting FK dependencies)
            Console.WriteLine("Starting migration...");
            Console.WriteLine();

            // Phase 1: Auxiliary tables (no dependencies)
            await MigrateCondicionIva(accessConn, db);
            await MigrateUnidadesMedida(accessConn, db);
            await MigrateRubros(accessConn, db);
            await MigratePaymentMethods(accessConn, db);
            await MigrateBranches(accessConn, db);
            await MigrateZonasVenta(db);
            
            // Phase 2: Tables with simple dependencies
            await MigrateVendedores(accessConn, db);
            await MigrateDepositos(accessConn, db);
            
            // Phase 3: Main entities
            await MigrateProductos(accessConn, db);
            await MigrateClientes(accessConn, db);
            await MigrateCustomerAddresses(accessConn, db);
            
            // Phase 4: Stock
            await MigrateStock(accessConn, db);
            
            // Phase 5: Documents (header + detail)
            await MigrateFacturas(accessConn, db);
            await MigrateRemitos(accessConn, db);
            await MigratePresupuestos(accessConn, db);
            await MigrateNotasCredito(accessConn, db);
            await MigrateNotasDebito(accessConn, db);
            await MigrateNotasDebitoInternas(accessConn, db);
            await MigrateConsignaciones(accessConn, db);
            
            // Phase 6: Payments
            await MigratePagos(accessConn, db);
            
            // Phase 7: Current Account
            await MigrateCtaCte(accessConn, db);
            await MigrateMovimientosCtaCte(accessConn, db);

            Console.WriteLine();
            Console.WriteLine("===========================================");
            Console.WriteLine("  Migration completed successfully!");
            Console.WriteLine("===========================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR during migration: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    #region Auxiliary Tables

    static async Task MigrateCondicionIva(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating CondicionIva... ");
        
        // Clear existing (except seed data)
        // We'll map the Access codes to our seeded IDs
        var existing = await db.CondicionesIva.ToListAsync();
        foreach (var e in existing)
        {
            CondicionIvaCodigoToId[e.Codigo] = e.Id;
        }

        using var cmd = new OleDbCommand("SELECT IDCondicionIVA, Descripcion FROM CondicionIva", access);
        using var reader = cmd.ExecuteReader();
        
        int count = 0;
        while (reader.Read())
        {
            var codigo = reader.GetString(0).Trim();
            var descripcion = reader.GetString(1).Trim();
            
            // Map Access codes to our standard codes
            var mappedCode = codigo switch
            {
                "RI" => "RI",
                "MO" => "MO",
                "CF" => "CF",
                "EX" => "EX",
                "M" => "MO",  // Alternative monotributo code
                _ => codigo
            };

            if (!CondicionIvaCodigoToId.ContainsKey(mappedCode))
            {
                // Determine TipoFactura based on code
                var tipoFactura = mappedCode == "RI" ? "A" : "B";
                
                var entity = new CondicionIva
                {
                    Codigo = mappedCode,
                    Descripcion = descripcion,
                    TipoFactura = tipoFactura
                };
                db.CondicionesIva.Add(entity);
                await db.SaveChangesAsync();
                CondicionIvaCodigoToId[mappedCode] = entity.Id;
            }
            
            // Also map the original code if different
            if (codigo != mappedCode && !CondicionIvaCodigoToId.ContainsKey(codigo))
            {
                CondicionIvaCodigoToId[codigo] = CondicionIvaCodigoToId[mappedCode];
            }
            
            count++;
        }
        
        Console.WriteLine($"OK ({count} records)");
    }

    static async Task MigrateUnidadesMedida(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating UnidadesMedida... ");
        
        var existing = await db.UnidadesMedida.ToListAsync();
        foreach (var e in existing)
        {
            UnidadMedidaCodigoToId[e.Codigo] = e.Id;
        }

        using var cmd = new OleDbCommand("SELECT IdUnidadMedida, Descripcion FROM UnidadesMedida", access);
        using var reader = cmd.ExecuteReader();
        
        int count = 0;
        while (reader.Read())
        {
            var codigo = reader.GetString(0).Trim();
            var descripcion = reader.GetString(1).Trim();
            
            if (!UnidadMedidaCodigoToId.ContainsKey(codigo))
            {
                var entity = new UnidadMedida
                {
                    Codigo = codigo,
                    Nombre = descripcion
                };
                db.UnidadesMedida.Add(entity);
                await db.SaveChangesAsync();
                UnidadMedidaCodigoToId[codigo] = entity.Id;
            }
            count++;
        }
        
        Console.WriteLine($"OK ({count} records)");
    }

    static async Task MigrateRubros(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating Rubros... ");
        
        var existing = await db.Rubros.ToListAsync();
        foreach (var e in existing)
        {
            RubroCodigoToId[e.Nombre] = e.Id;
        }

        using var cmd = new OleDbCommand("SELECT IdRubro, Descripcion FROM Rubros", access);
        using var reader = cmd.ExecuteReader();
        
        int count = 0;
        while (reader.Read())
        {
            var codigo = reader.GetString(0).Trim();
            var descripcion = reader.GetString(1).Trim();
            
            if (!RubroCodigoToId.ContainsKey(descripcion))
            {
                var entity = new Rubro
                {
                    Nombre = descripcion,
                    Activo = true
                };
                db.Rubros.Add(entity);
                await db.SaveChangesAsync();
                RubroCodigoToId[descripcion] = entity.Id;
            }
            
            // Also map by code
            if (!RubroCodigoToId.ContainsKey(codigo))
            {
                RubroCodigoToId[codigo] = RubroCodigoToId[descripcion];
            }
            
            count++;
        }
        
        Console.WriteLine($"OK ({count} records)");
    }

    static async Task MigratePaymentMethods(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating PaymentMethods (CodPago)... ");
        
        var existing = await db.PaymentMethods.ToListAsync();
        foreach (var e in existing)
        {
            PaymentMethodCodigoToId[e.Code.ToUpper()] = e.Id;
        }

        using var cmd = new OleDbCommand("SELECT IDPago, Descripcion FROM CodPago", access);
        using var reader = cmd.ExecuteReader();
        
        int count = 0;
        int added = 0;
        while (reader.Read())
        {
            var codigoRaw = GetString(reader, 0) ?? "";
            var codigo = codigoRaw.Trim().ToUpper();
            var descripcion = GetString(reader, 1) ?? codigo;
            
            if (string.IsNullOrWhiteSpace(codigo)) continue;
            
            if (!PaymentMethodCodigoToId.ContainsKey(codigo))
            {
                // Determine type based on code
                var type = codigo switch
                {
                    "EF" or "EFECT" or "EFECTIVO" => PaymentMethodType.Cash,
                    "CH" or "CHEQUE" => PaymentMethodType.Check,
                    "TR" or "TRANSF" or "TRANSFERENCIA" => PaymentMethodType.Transfer,
                    "TC" or "TARJETA" => PaymentMethodType.Card,
                    "TD" => PaymentMethodType.Card,
                    "RZ" or "REZAGO" => PaymentMethodType.Barter,
                    "ME" or "MERCADERIA" => PaymentMethodType.Barter,
                    _ => PaymentMethodType.Other
                };
                
                // Truncate code to 10 chars max (column size)
                if (codigo.Length > 10) codigo = codigo.Substring(0, 10);
                
                var entity = new PaymentMethod
                {
                    Code = codigo,
                    Description = descripcion.Length > 100 ? descripcion.Substring(0, 100) : descripcion,
                    Type = type,
                    RequiresDetail = type == PaymentMethodType.Check || type == PaymentMethodType.Barter,
                    IsActive = true
                };
                
                try
                {
                    db.PaymentMethods.Add(entity);
                    await db.SaveChangesAsync();
                    PaymentMethodCodigoToId[codigo] = entity.Id;
                    added++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n  Warning: Could not add payment method '{codigo}': {ex.Message}");
                    db.Entry(entity).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
            }
            count++;
        }
        
        Console.WriteLine($"OK ({count} records, {added} new)");
    }

    static async Task MigrateBranches(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating Branches (Sucursales)... ");
        
        // Map existing seeded branches
        var existing = await db.Branches.ToListAsync();
        foreach (var e in existing)
        {
            // Map by PointOfSale since that's the IdSucursal in Access
            BranchAccessIdToSqlId[e.PointOfSale] = e.Id;
        }

        using var cmd = new OleDbCommand("SELECT IdSucursal, NombreSucursal FROM Sucursales", access);
        using var reader = cmd.ExecuteReader();
        
        int count = 0;
        while (reader.Read())
        {
            var idSucursal = reader.GetInt32(0);
            var nombre = GetString(reader, 1) ?? $"Sucursal {idSucursal}";
            
            if (!BranchAccessIdToSqlId.ContainsKey(idSucursal))
            {
                var entity = new Branch
                {
                    Code = $"SUC{idSucursal}",
                    Name = nombre,
                    PointOfSale = idSucursal,
                    IsActive = true
                };
                db.Branches.Add(entity);
                await db.SaveChangesAsync();
                BranchAccessIdToSqlId[idSucursal] = entity.Id;
            }
            count++;
        }
        
        Console.WriteLine($"OK ({count} records)");
    }

    static async Task MigrateZonasVenta(SPCDbContext db)
    {
        Console.Write("Migrating ZonasVenta... ");
        
        // Create ZonasVenta based on known values from Access
        // Values found in qListadoClientesVendedor: 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 100
        var zonasAccess = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 100 };
        
        // Map existing
        var existing = await db.ZonasVenta.ToListAsync();
        foreach (var e in existing)
        {
            // Try to extract the zone number from the name
            var nombre = e.Nombre;
            if (nombre.StartsWith("Zona ") && int.TryParse(nombre.Substring(5).Trim(), out var num))
            {
                ZonaVentaAccessIdToSqlId[num] = e.Id;
            }
        }
        
        int count = 0;
        foreach (var zonaId in zonasAccess)
        {
            if (!ZonaVentaAccessIdToSqlId.ContainsKey(zonaId))
            {
                var entity = new ZonaVenta
                {
                    Nombre = $"Zona {zonaId:D2}",
                    Descripcion = $"Zona de venta {zonaId}",
                    Activa = true
                };
                db.ZonasVenta.Add(entity);
                await db.SaveChangesAsync();
                ZonaVentaAccessIdToSqlId[zonaId] = entity.Id;
                count++;
            }
        }
        
        Console.WriteLine($"OK ({count} new records)");
    }

    #endregion

    #region Vendedores and Depositos

    static async Task MigrateVendedores(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating Vendedores (Empleados)... ");
        
        // Load existing vendedores first
        var existingVendedores = await db.Vendedores.ToListAsync();
        foreach (var v in existingVendedores)
        {
            VendedorLegajoToId[v.Legajo] = v.Id;
        }
        
        if (existingVendedores.Count > 0)
        {
            Console.WriteLine($"SKIP ({existingVendedores.Count} already exist)");
            return;
        }
        
        using var cmd = new OleDbCommand(@"
            SELECT Legajo, Nombre, CUIL, Domicilio, Localidad, Prov, CP, 
                   DNI, Tel, Cel, emaill, FechaNacimiento, FechaIngreso, Comision, Observaciones
            FROM Empleados", access);
        using var reader = cmd.ExecuteReader();
        
        var allVendedores = new List<Vendedor>();
        var legajoList = new List<string>();
        
        while (reader.Read())
        {
            var legajo = GetString(reader, 0);
            if (string.IsNullOrWhiteSpace(legajo)) continue;
            
            var entity = new Vendedor
            {
                Legajo = legajo,
                Nombre = GetString(reader, 1) ?? "Sin Nombre",
                CUIL = GetString(reader, 2),
                Domicilio = GetString(reader, 3),
                Localidad = GetString(reader, 4),
                Provincia = GetString(reader, 5),
                CodigoPostal = GetString(reader, 6),
                DNI = GetString(reader, 7),
                Telefono = GetString(reader, 8),
                Celular = GetString(reader, 9),
                Email = GetString(reader, 10),
                FechaNacimiento = GetDateTime(reader, 11),
                FechaIngreso = GetDateTime(reader, 12),
                PorcentajeComision = GetDecimal(reader, 13),
                Observaciones = GetString(reader, 14),
                Activo = true
            };
            
            allVendedores.Add(entity);
            legajoList.Add(legajo);
        }
        
        if (allVendedores.Count > 0)
        {
            await db.BulkInsertAsync(allVendedores, new BulkConfig { SetOutputIdentity = true });
            
            for (int i = 0; i < allVendedores.Count; i++)
            {
                VendedorLegajoToId[legajoList[i]] = allVendedores[i].Id;
            }
        }
        
        Console.WriteLine($"OK ({allVendedores.Count} records)");
    }

    static async Task MigrateDepositos(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating Depositos... ");
        
        // Map existing (seeded) deposito
        var existing = await db.Depositos.ToListAsync();
        foreach (var e in existing)
        {
            DepositoCodigoToId[e.Nombre] = e.Id;
        }

        using var cmd = new OleDbCommand("SELECT IdDeposito, Descripcion, VendedorAsociado FROM Depositos", access);
        using var reader = cmd.ExecuteReader();
        
        int count = 0;
        while (reader.Read())
        {
            var codigo = GetString(reader, 0) ?? "";
            var descripcion = GetString(reader, 1) ?? codigo;
            var vendedorLegajo = GetString(reader, 2);
            
            if (!DepositoCodigoToId.ContainsKey(codigo) && !DepositoCodigoToId.ContainsKey(descripcion))
            {
                var entity = new Deposito
                {
                    Nombre = descripcion,
                    VendedorAsociadoId = !string.IsNullOrEmpty(vendedorLegajo) && VendedorLegajoToId.ContainsKey(vendedorLegajo) 
                        ? VendedorLegajoToId[vendedorLegajo] 
                        : null,
                    Activo = true
                };
                db.Depositos.Add(entity);
                await db.SaveChangesAsync();
                DepositoCodigoToId[codigo] = entity.Id;
                DepositoCodigoToId[descripcion] = entity.Id;
            }
            count++;
        }
        
        Console.WriteLine($"OK ({count} records)");
    }

    #endregion

    #region Productos and Clientes

    static async Task MigrateProductos(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating Productos... ");
        
        // Load existing productos first
        var existingProductos = await db.Productos.ToListAsync();
        foreach (var p in existingProductos)
        {
            ProductoCodigoToId[p.Codigo] = p.Id;
        }
        
        if (existingProductos.Count > 0)
        {
            Console.WriteLine($"SKIP ({existingProductos.Count} already exist)");
            return;
        }
        
        using var cmd = new OleDbCommand(@"
            SELECT CodProd, Descripcion, PrecioUnitarioFactura, PrecioUnitarioPresupuesto, 
                   Rubro, UnidadMedida, PuntoPedido, Observaciones
            FROM Productos", access);
        using var reader = cmd.ExecuteReader();
        
        var allProductos = new List<Producto>();
        var codigoList = new List<string>(); // Track codes in same order
        
        while (reader.Read())
        {
            var codigo = GetString(reader, 0);
            if (string.IsNullOrWhiteSpace(codigo)) continue;
            
            var rubro = GetString(reader, 4);
            var unidadMedida = GetString(reader, 5);
            
            var entity = new Producto
            {
                Codigo = codigo,
                Descripcion = GetString(reader, 1) ?? codigo,
                PrecioVenta = GetDecimal(reader, 2),
                PrecioCosto = GetDecimal(reader, 3),
                RubroId = !string.IsNullOrEmpty(rubro) && RubroCodigoToId.ContainsKey(rubro) 
                    ? RubroCodigoToId[rubro] 
                    : null,
                UnidadMedidaId = !string.IsNullOrEmpty(unidadMedida) && UnidadMedidaCodigoToId.ContainsKey(unidadMedida) 
                    ? UnidadMedidaCodigoToId[unidadMedida] 
                    : null,
                StockMinimo = int.TryParse(GetString(reader, 6), out var pp) ? pp : 0,
                Observaciones = GetString(reader, 7),
                PorcentajeIVA = 21m, // Default IVA
                Activo = true
            };
            
            allProductos.Add(entity);
            codigoList.Add(codigo);
        }
        
        Console.Write($"({allProductos.Count} records to insert)... ");
        
        // Bulk insert all at once
        await db.BulkInsertAsync(allProductos, new BulkConfig { SetOutputIdentity = true });
        
        // Map codes to new SQL Server IDs
        for (int i = 0; i < allProductos.Count; i++)
        {
            ProductoCodigoToId[codigoList[i]] = allProductos[i].Id;
        }
        
        Console.WriteLine($"OK ({allProductos.Count} records)");
    }

    static async Task MigrateClientes(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating Clientes... ");
        
        // Load existing clientes first
        var existingCount = await db.Clientes.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            
            // Load existing for FK mapping - IDs should match Access IDs now
            var existingClientes = await db.Clientes.ToListAsync();
            foreach (var c in existingClientes)
            {
                ClienteAccessIdToSqlId[c.Id] = c.Id;
            }
            return;
        }
        
        using var cmd = new OleDbCommand(@"
            SELECT IDCliente, RazonSocial, NombreFantasia, CUIT, Domicilio, Localidad, 
                   Prov, CP, Tel, Cel, email, CondicionIva, Vendedor, ZonaVenta,
                   PorcentajeDescuento, LimiteCredito, Observaciones
            FROM Clientes
            ORDER BY IDCliente", access);
        using var reader = cmd.ExecuteReader();
        
        var allClientes = new List<Cliente>();
        
        while (reader.Read())
        {
            var accessId = reader.GetInt32(0);
            var condicionIva = GetString(reader, 11);
            var vendedorLegajo = GetString(reader, 12);
            var zonaVentaAccess = GetInt(reader, 13);
            
            // Clamp percentages to valid range
            var porcentajeDescuento = GetDecimal(reader, 14);
            if (porcentajeDescuento > 100) porcentajeDescuento = 100;
            if (porcentajeDescuento < 0) porcentajeDescuento = 0;
            
            // Clamp limite credito to reasonable range
            var limiteCredito = GetDecimal(reader, 15);
            if (limiteCredito > 999999999999m) limiteCredito = 999999999999m;
            if (limiteCredito < 0) limiteCredito = 0;
            
            var entity = new Cliente
            {
                Id = accessId,  // PRESERVE ORIGINAL ID!
                RazonSocial = TruncateString(GetString(reader, 1) ?? "Sin Nombre", 200),
                NombreFantasia = TruncateString(GetString(reader, 2), 200),
                CUIT = TruncateString(GetString(reader, 3), 13),
                Direccion = TruncateString(GetString(reader, 4), 300),
                Localidad = TruncateString(GetString(reader, 5), 100),
                Provincia = TruncateString(GetString(reader, 6), 100),
                CodigoPostal = TruncateString(GetString(reader, 7), 10),
                Telefono = TruncateString(GetString(reader, 8), 50),
                Celular = TruncateString(GetString(reader, 9), 50),
                Email = TruncateString(GetString(reader, 10), 200),
                CondicionIvaId = !string.IsNullOrEmpty(condicionIva) && CondicionIvaCodigoToId.ContainsKey(condicionIva) 
                    ? CondicionIvaCodigoToId[condicionIva] 
                    : null,
                VendedorId = !string.IsNullOrEmpty(vendedorLegajo) && VendedorLegajoToId.ContainsKey(vendedorLegajo) 
                    ? VendedorLegajoToId[vendedorLegajo] 
                    : null,
                ZonaVentaId = ZonaVentaAccessIdToSqlId.ContainsKey(zonaVentaAccess) 
                    ? ZonaVentaAccessIdToSqlId[zonaVentaAccess] 
                    : null,
                PorcentajeDescuento = porcentajeDescuento,
                LimiteCredito = limiteCredito,
                Observaciones = TruncateString(GetString(reader, 16), 500),
                Activo = true,
                FechaAlta = DateTime.Now
            };
            
            allClientes.Add(entity);
            ClienteAccessIdToSqlId[accessId] = accessId;  // ID = ID (preserved)
        }
        
        Console.Write($"({allClientes.Count} records to insert)... ");
        
        // Bulk insert with IDENTITY_INSERT ON to preserve original IDs
        await BulkInsertWithIdentityAsync(db, allClientes, "Clientes", 2000);
        
        Console.WriteLine($"OK ({allClientes.Count} records)");
    }

    /// <summary>
    /// Bulk insert with IDENTITY_INSERT ON to preserve original IDs
    /// Uses raw SQL since EFCore.BulkExtensions 10.x doesn't have KeepIdentity
    /// </summary>
    static async Task BulkInsertWithIdentityAsync<T>(SPCDbContext db, List<T> items, string tableName, int batchSize = 2000) where T : class
    {
        if (items.Count == 0) return;

        var connectionString = db.Database.GetConnectionString();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var transaction = connection.BeginTransaction();
        try
        {
            // Enable IDENTITY_INSERT
            await using (var cmd = new SqlCommand($"SET IDENTITY_INSERT [{tableName}] ON", connection, transaction))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Use SqlBulkCopy for fast inserts
            using var bulkCopy = new SqlBulkCopy(connection, Microsoft.Data.SqlClient.SqlBulkCopyOptions.KeepIdentity, transaction)
            {
                DestinationTableName = tableName,
                BatchSize = batchSize
            };

            // Create DataTable from items
            var dataTable = ToDataTable(items);
            
            // Map columns
            foreach (DataColumn column in dataTable.Columns)
            {
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }

            await bulkCopy.WriteToServerAsync(dataTable);

            // Disable IDENTITY_INSERT
            await using (var cmd = new SqlCommand($"SET IDENTITY_INSERT [{tableName}] OFF", connection, transaction))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Convert list of entities to DataTable for SqlBulkCopy
    /// </summary>
    static DataTable ToDataTable<T>(List<T> items)
    {
        var table = new DataTable();
        var properties = typeof(T).GetProperties()
            .Where(p => p.CanRead && !IsNavigationProperty(p))
            .ToArray();

        // Add columns
        foreach (var prop in properties)
        {
            var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            
            // Handle enums
            if (propType.IsEnum)
            {
                propType = typeof(int);
            }
            
            table.Columns.Add(prop.Name, propType);
        }

        // Add rows
        foreach (var item in items)
        {
            var row = table.NewRow();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(item);
                
                // Handle enums
                if (value != null && prop.PropertyType.IsEnum)
                {
                    value = (int)value;
                }
                else if (value != null && Nullable.GetUnderlyingType(prop.PropertyType)?.IsEnum == true)
                {
                    value = (int)value;
                }
                
                row[prop.Name] = value ?? DBNull.Value;
            }
            table.Rows.Add(row);
        }

        return table;
    }

    /// <summary>
    /// Check if property is a navigation property (should be excluded from bulk insert)
    /// </summary>
    static bool IsNavigationProperty(System.Reflection.PropertyInfo prop)
    {
        // Skip collections
        if (prop.PropertyType != typeof(string) && 
            typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.PropertyType))
            return true;

        // Skip navigation properties (reference types except string and common value types)
        if (prop.PropertyType.IsClass && 
            prop.PropertyType != typeof(string) &&
            !prop.PropertyType.IsPrimitive &&
            prop.PropertyType != typeof(DateTime) &&
            prop.PropertyType != typeof(decimal))
            return true;

        return false;
    }

    /// <summary>
    /// Clean all data from database in correct order (respecting FK constraints)
    /// </summary>
    static async Task CleanDatabaseAsync(SPCDbContext db)
    {
        var tables = new[]
        {
            // Detail tables first (FK dependencies)
            "CurrentAccountMovements",
            "MovimientosCtaCte",
            "CurrentAccounts",
            "PaymentDetails",
            "Payments",
            "ConsignmentDetails",
            "Consignments",
            "InternalDebitNoteDetails",
            "InternalDebitNotes",
            "DebitNoteDetails",
            "DebitNotes",
            "CreditNoteDetails",
            "CreditNotes",
            "QuoteDetails",
            "Quotes",
            "RemitoDetalles",
            "Remitos",
            "FacturaDetalles",
            "Facturas",
            "StockMovementDetails",
            "StockMovements",
            "Stocks",
            "CustomerAddresses",
            "Clientes",
            "Productos",
            "Vendedores",
            "Depositos",
            "Branches",
            "ZonasVenta",
            "PaymentMethods",
            "Rubros",
            "UnidadesMedida",
            "CondicionesIva"
        };

        // Disable FK checks
        Console.WriteLine("Disabling foreign key constraints...");
        await db.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");

        // Delete from each table
        foreach (var table in tables)
        {
            Console.Write($"Deleting {table}... ");
            try
            {
                var count = await db.Database.ExecuteSqlRawAsync($"DELETE FROM [{table}]");
                Console.WriteLine($"OK ({count} rows)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SKIP ({ex.Message})");
            }
        }

        // Reset identity seeds
        Console.WriteLine();
        Console.WriteLine("Resetting identity seeds...");
        foreach (var table in tables)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync($"DBCC CHECKIDENT ('{table}', RESEED, 0)");
            }
            catch
            {
                // Some tables may not have identity columns - ignore
            }
        }

        // Re-enable FK checks
        Console.WriteLine("Re-enabling foreign key constraints...");
        await db.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'");
    }
    
    static string? TruncateString(string? value, int maxLength)
    {
        if (value == null) return null;
        return value.Length > maxLength ? value.Substring(0, maxLength) : value;
    }

    static async Task MigrateCustomerAddresses(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating CustomerAddresses (DomiciliosClientes)... ");
        
        using var cmd = new OleDbCommand(@"
            SELECT IDCliente, Item, Domicilio, Localidad, Prov, CP, Tel, Cel, email, Observaciones
            FROM DomiciliosClientes", access);
        using var reader = cmd.ExecuteReader();
        
        int count = 0;
        while (reader.Read())
        {
            var accessClienteId = reader.GetInt32(0);
            if (!ClienteAccessIdToSqlId.ContainsKey(accessClienteId)) continue;
            
            var entity = new CustomerAddress
            {
                CustomerId = ClienteAccessIdToSqlId[accessClienteId],
                ItemNumber = reader.GetInt32(1),
                AddressType = AddressType.Delivery,
                Address = GetString(reader, 2) ?? "",
                City = GetString(reader, 3),
                Province = GetString(reader, 4),
                PostalCode = GetString(reader, 5),
                Phone = GetString(reader, 6),
                Mobile = GetString(reader, 7),
                Email = GetString(reader, 8),
                Notes = GetString(reader, 9),
                IsDefault = reader.GetInt32(1) == 1
            };
            
            db.CustomerAddresses.Add(entity);
            count++;
        }
        
        await db.SaveChangesAsync();
        Console.WriteLine($"OK ({count} records)");
    }

    #endregion

    #region Stock

    static async Task MigrateStock(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating Stock... ");
        
        using var cmd = new OleDbCommand("SELECT CodProd, IdDeposito, Cantidad FROM Stock", access);
        using var reader = cmd.ExecuteReader();
        
        int count = 0;
        while (reader.Read())
        {
            var codProd = GetString(reader, 0);
            var idDeposito = GetString(reader, 1);
            
            if (string.IsNullOrEmpty(codProd) || !ProductoCodigoToId.ContainsKey(codProd)) continue;
            if (string.IsNullOrEmpty(idDeposito) || !DepositoCodigoToId.ContainsKey(idDeposito)) continue;
            
            var entity = new Stock
            {
                ProductoId = ProductoCodigoToId[codProd],
                DepositoId = DepositoCodigoToId[idDeposito],
                Cantidad = GetDecimal(reader, 2)
            };
            
            db.Stocks.Add(entity);
            count++;
        }
        
        await db.SaveChangesAsync();
        Console.WriteLine($"OK ({count} records)");
    }

    #endregion

    #region Documents

    static async Task MigrateFacturas(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating Facturas... ");
        
        var existingCount = await db.Facturas.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            // Load for FK mapping
            var existing = await db.Facturas.ToListAsync();
            foreach (var f in existing)
            {
                FacturaAccessToSqlId[(f.TipoFactura, (int)f.NumeroFactura)] = f.Id;
            }
            return;
        }
        
        // Headers - collect all first
        using var cmdH = new OleDbCommand(@"
            SELECT TipoFactura, NroFactura, FechaFactura, CodCliente, CodVendedor,
                   SubTotalFactura, PorcentajeIVA, TotalIVA, AlicuotaIIBB, ImportePercepIIBB,
                   PorcentajeDesc, ImporteDesc, TotalFactura, Cae, FechaVC, 
                   CondicionVenta, FormaPago, UnidadNegocio, Cancelada, IdSucursal, NroRemito, AclaracionFactura
            FROM FacturaC
            ORDER BY TipoFactura, NroFactura", access);
        using var readerH = cmdH.ExecuteReader();
        
        var allFacturas = new List<Factura>();
        var facturaKeys = new List<(string tipo, int numero)>();
        
        while (readerH.Read())
        {
            var tipoFactura = GetString(readerH, 0) ?? "B";
            var nroFactura = readerH.GetInt32(1);
            var codCliente = GetInt(readerH, 3);
            
            if (!ClienteAccessIdToSqlId.ContainsKey(codCliente)) continue;
            
            var vendedorLegajo = GetString(readerH, 4);
            var idSucursal = GetInt(readerH, 19);
            
            var branchId = BranchAccessIdToSqlId.ContainsKey(idSucursal) 
                ? BranchAccessIdToSqlId[idSucursal] 
                : 1;
            var puntoVenta = idSucursal > 0 ? idSucursal : 2;
            
            var entity = new Factura
            {
                BranchId = branchId,
                TipoFactura = tipoFactura,
                PuntoVenta = puntoVenta,
                NumeroFactura = nroFactura,
                FechaFactura = GetDateTime(readerH, 2) ?? DateTime.Now,
                ClienteId = ClienteAccessIdToSqlId[codCliente],
                VendedorId = !string.IsNullOrEmpty(vendedorLegajo) && VendedorLegajoToId.ContainsKey(vendedorLegajo) 
                    ? VendedorLegajoToId[vendedorLegajo] 
                    : null,
                Subtotal = GetDecimal(readerH, 5),
                PorcentajeIVA = GetDecimal(readerH, 6),
                ImporteIVA = GetDecimal(readerH, 7),
                AlicuotaIIBB = GetDecimal(readerH, 8),
                ImportePercepcionIIBB = GetDecimal(readerH, 9),
                PorcentajeDescuento = GetDecimal(readerH, 10),
                ImporteDescuento = GetDecimal(readerH, 11),
                Total = GetDecimal(readerH, 12),
                CAE = GetString(readerH, 13),
                FechaVencimientoCAE = GetDateTime(readerH, 14),
                CondicionVenta = GetString(readerH, 15),
                FormaPago = GetInt(readerH, 16),
                UnidadNegocio = GetString(readerH, 17),
                Anulada = GetString(readerH, 18)?.ToUpper() == "S" || GetBool(readerH, 18),
                Aclaracion = GetString(readerH, 21)
            };
            
            allFacturas.Add(entity);
            facturaKeys.Add((tipoFactura, nroFactura));
        }
        
        Console.Write($"({allFacturas.Count} headers)... ");
        
        // Insert in batches to avoid timeout
        const int batchSize = 2000;
        for (int batch = 0; batch < allFacturas.Count; batch += batchSize)
        {
            var batchItems = allFacturas.Skip(batch).Take(batchSize).ToList();
            await db.BulkInsertAsync(batchItems, new BulkConfig { SetOutputIdentity = true, BatchSize = batchSize });
            
            // Map the IDs for this batch
            for (int i = 0; i < batchItems.Count; i++)
            {
                FacturaAccessToSqlId[facturaKeys[batch + i]] = batchItems[i].Id;
            }
            Console.Write(".");
        }
        
        Console.WriteLine(" OK");
        
        // Details
        Console.Write("Migrating FacturaDetalles... ");
        using var cmdD = new OleDbCommand(@"
            SELECT TipoFactura, NroFactura, ItemFactura, IDCodProd, Cantidad, 
                   PrecioUnitario, PorcentajeDescuento, TotalLinea
            FROM FacturaD
            ORDER BY TipoFactura, NroFactura, ItemFactura", access);
        using var readerD = cmdD.ExecuteReader();
        
        var allDetalles = new List<FacturaDetalle>();
        
        while (readerD.Read())
        {
            var tipoFactura = GetString(readerD, 0) ?? "B";
            var nroFactura = readerD.GetInt32(1);
            var codProd = GetString(readerD, 3);
            
            if (!FacturaAccessToSqlId.ContainsKey((tipoFactura, nroFactura))) continue;
            if (string.IsNullOrEmpty(codProd) || !ProductoCodigoToId.ContainsKey(codProd)) continue;
            
            var entity = new FacturaDetalle
            {
                FacturaId = FacturaAccessToSqlId[(tipoFactura, nroFactura)],
                ItemNumero = readerD.GetInt32(2),
                ProductoId = ProductoCodigoToId[codProd],
                Cantidad = GetDecimal(readerD, 4),
                PrecioUnitario = GetDecimal(readerD, 5),
                PorcentajeDescuento = GetDecimal(readerD, 6),
                PorcentajeIVA = 21m,
                Subtotal = GetDecimal(readerD, 7)
            };
            
            allDetalles.Add(entity);
        }
        
        Console.Write($"({allDetalles.Count} details)... ");
        
        // Insert in batches
        for (int batch = 0; batch < allDetalles.Count; batch += batchSize)
        {
            var batchItems = allDetalles.Skip(batch).Take(batchSize).ToList();
            await db.BulkInsertAsync(batchItems, new BulkConfig { BatchSize = batchSize });
            Console.Write(".");
        }
        
        Console.WriteLine(" OK");
    }

    static async Task MigrateRemitos(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating Remitos... ");
        
        using var cmdH = new OleDbCommand(@"
            SELECT IdSucursal, NroRemito, FechaRemito, CodCliente, CodVendedor,
                   UnidadNegocio, NroFactura, TipoFactura, AclaracionRemito
            FROM RemitoC
            ORDER BY IdSucursal, NroRemito", access);
        using var readerH = cmdH.ExecuteReader();
        
        var allRemitos = new List<Remito>();
        var remitoKeys = new List<(int sucursal, int numero)>();
        
        while (readerH.Read())
        {
            var idSucursal = readerH.GetInt32(0);
            var nroRemito = readerH.GetInt32(1);
            var codCliente = GetInt(readerH, 3);
            
            if (!ClienteAccessIdToSqlId.ContainsKey(codCliente)) continue;
            
            var vendedorLegajo = GetString(readerH, 4);
            var nroFactura = GetInt(readerH, 6);
            var tipoFactura = GetString(readerH, 7);
            
            var branchId = BranchAccessIdToSqlId.ContainsKey(idSucursal) 
                ? BranchAccessIdToSqlId[idSucursal] 
                : 1;
            var puntoVenta = idSucursal > 0 ? idSucursal : 2;
            
            int? facturaId = null;
            if (nroFactura > 0 && !string.IsNullOrEmpty(tipoFactura) && FacturaAccessToSqlId.ContainsKey((tipoFactura, nroFactura)))
            {
                facturaId = FacturaAccessToSqlId[(tipoFactura, nroFactura)];
            }
            
            var entity = new Remito
            {
                BranchId = branchId,
                PuntoVenta = puntoVenta,
                NumeroRemito = nroRemito,
                FechaRemito = GetDateTime(readerH, 2) ?? DateTime.Now,
                ClienteId = ClienteAccessIdToSqlId[codCliente],
                VendedorId = !string.IsNullOrEmpty(vendedorLegajo) && VendedorLegajoToId.ContainsKey(vendedorLegajo) 
                    ? VendedorLegajoToId[vendedorLegajo] 
                    : null,
                UnidadNegocio = GetString(readerH, 5),
                FacturaId = facturaId,
                TipoFactura = tipoFactura,
                Facturado = facturaId.HasValue,
                Aclaracion = GetString(readerH, 8),
                Anulado = false
            };
            
            allRemitos.Add(entity);
            remitoKeys.Add((idSucursal, nroRemito));
        }
        
        Console.Write($"({allRemitos.Count} headers)... ");
        
        if (allRemitos.Count > 0)
        {
            await db.BulkInsertAsync(allRemitos, new BulkConfig { SetOutputIdentity = true });
            for (int i = 0; i < allRemitos.Count; i++)
            {
                RemitoAccessToSqlId[remitoKeys[i]] = allRemitos[i].Id;
            }
        }
        
        Console.WriteLine("OK");
        
        // Details
        Console.Write("Migrating RemitoDetalles... ");
        using var cmdD = new OleDbCommand(@"
            SELECT IdSucursal, NroRemito, ItemRemito, IDCodProd, Cantidad
            FROM RemitoD
            ORDER BY IdSucursal, NroRemito, ItemRemito", access);
        using var readerD = cmdD.ExecuteReader();
        
        var allDetalles = new List<RemitoDetalle>();
        
        while (readerD.Read())
        {
            var idSucursal = readerD.GetInt32(0);
            var nroRemito = readerD.GetInt32(1);
            var codProd = GetString(readerD, 3);
            
            if (!RemitoAccessToSqlId.ContainsKey((idSucursal, nroRemito))) continue;
            if (string.IsNullOrEmpty(codProd) || !ProductoCodigoToId.ContainsKey(codProd)) continue;
            
            var entity = new RemitoDetalle
            {
                RemitoId = RemitoAccessToSqlId[(idSucursal, nroRemito)],
                ItemNumero = readerD.GetInt32(2),
                ProductoId = ProductoCodigoToId[codProd],
                Cantidad = GetDecimal(readerD, 4)
            };
            
            allDetalles.Add(entity);
        }
        
        Console.Write($"({allDetalles.Count} details)... ");
        
        if (allDetalles.Count > 0)
        {
            await db.BulkInsertAsync(allDetalles);
        }
        
        Console.WriteLine("OK");
    }

    static async Task MigratePresupuestos(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating Presupuestos (Quotes)... ");
        
        var quoteMap = new Dictionary<int, int>(); // Access NroPresu -> SQL Id
        
        using var cmdH = new OleDbCommand(@"
            SELECT NroPresu, FechaPresu, CodCliente, CodVendedor,
                   SubTotalPresu, PorcentajeDesc, ImporteDesc, TotalPresu,
                   UnidadNegocio, Anulado
            FROM PresupuestoC
            ORDER BY NroPresu", access);
        using var readerH = cmdH.ExecuteReader();
        
        var allQuotes = new List<Quote>();
        var quoteKeys = new List<int>();
        
        while (readerH.Read())
        {
            var nroPresu = readerH.GetInt32(0);
            var codCliente = GetInt(readerH, 2);
            
            if (!ClienteAccessIdToSqlId.ContainsKey(codCliente)) continue;
            
            var vendedorLegajo = GetString(readerH, 3);
            
            var entity = new Quote
            {
                BranchId = 1,
                QuoteNumber = nroPresu,
                QuoteDate = GetDateTime(readerH, 1) ?? DateTime.Now,
                CustomerId = ClienteAccessIdToSqlId[codCliente],
                SalesRepId = !string.IsNullOrEmpty(vendedorLegajo) && VendedorLegajoToId.ContainsKey(vendedorLegajo) 
                    ? VendedorLegajoToId[vendedorLegajo] 
                    : null,
                Subtotal = GetDecimal(readerH, 4),
                DiscountPercent = GetDecimal(readerH, 5),
                DiscountAmount = GetDecimal(readerH, 6),
                Total = GetDecimal(readerH, 7),
                BusinessUnit = GetString(readerH, 8),
                IsVoided = GetString(readerH, 9)?.ToUpper() == "S"
            };
            
            allQuotes.Add(entity);
            quoteKeys.Add(nroPresu);
        }
        
        Console.Write($"({allQuotes.Count} headers)... ");
        
        // Insert in batches to avoid timeout
        const int batchSize = 2000;
        for (int batch = 0; batch < allQuotes.Count; batch += batchSize)
        {
            var batchItems = allQuotes.Skip(batch).Take(batchSize).ToList();
            await db.BulkInsertAsync(batchItems, new BulkConfig { SetOutputIdentity = true, BatchSize = batchSize });
            
            for (int i = 0; i < batchItems.Count; i++)
            {
                quoteMap[quoteKeys[batch + i]] = batchItems[i].Id;
            }
            Console.Write(".");
        }
        
        Console.WriteLine(" OK");
        
        // Details
        Console.Write("Migrating PresupuestoDetalles (QuoteDetails)... ");
        using var cmdD = new OleDbCommand(@"
            SELECT NroPresu, ItemPresu, CodProd, Cantidad, 
                   PrecioUnitario, PorcentajeDescuento, ImporteDescuento, TotalLinea
            FROM PresupuestoD
            ORDER BY NroPresu, ItemPresu", access);
        using var readerD = cmdD.ExecuteReader();
        
        var allDetails = new List<QuoteDetail>();
        
        while (readerD.Read())
        {
            var nroPresu = readerD.GetInt32(0);
            var codProd = GetString(readerD, 2);
            
            if (!quoteMap.ContainsKey(nroPresu)) continue;
            if (string.IsNullOrEmpty(codProd) || !ProductoCodigoToId.ContainsKey(codProd)) continue;
            
            var entity = new QuoteDetail
            {
                QuoteId = quoteMap[nroPresu],
                ItemNumber = readerD.GetInt32(1),
                ProductId = ProductoCodigoToId[codProd],
                Quantity = GetDecimal(readerD, 3),
                UnitPrice = GetDecimal(readerD, 4),
                DiscountPercent = GetDecimal(readerD, 5),
                DiscountAmount = GetDecimal(readerD, 6),
                Subtotal = GetDecimal(readerD, 7)
            };
            
            allDetails.Add(entity);
        }
        
        Console.Write($"({allDetails.Count} details)... ");
        
        // Insert in batches
        for (int batch = 0; batch < allDetails.Count; batch += batchSize)
        {
            var batchItems = allDetails.Skip(batch).Take(batchSize).ToList();
            await db.BulkInsertAsync(batchItems, new BulkConfig { BatchSize = batchSize });
            Console.Write(".");
        }
        
        Console.WriteLine(" OK");
    }

    static async Task MigrateNotasCredito(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating NotasCredito (CreditNotes)... ");
        
        var ncMap = new Dictionary<(string, int), int>();
        
        using var cmdH = new OleDbCommand(@"
            SELECT TipoNotaCredito, NroNotaCredito, FechaNotaCredito, CodCliente, CodVendedor,
                   SubTotalNotaCredito, PorcentajeIVA, TotalIVA, AlicuotaIIBB, ImportePercepIIBB,
                   PorcentajeDesc, ImporteDesc, TotalNotaCredito, CAE, FechaVC, CondicionVenta, Cancelada
            FROM NotaCreditoC
            ORDER BY TipoNotaCredito, NroNotaCredito", access);
        using var readerH = cmdH.ExecuteReader();
        
        var allNotes = new List<CreditNote>();
        var noteKeys = new List<(string tipo, int numero)>();
        
        while (readerH.Read())
        {
            var tipoNC = GetString(readerH, 0) ?? "B";
            var nroNC = readerH.GetInt32(1);
            var codCliente = GetInt(readerH, 3);
            
            if (!ClienteAccessIdToSqlId.ContainsKey(codCliente)) continue;
            
            var vendedorLegajo = GetString(readerH, 4);
            var voucherType = tipoNC == "A" ? VoucherType.CreditNoteA : VoucherType.CreditNoteB;
            
            var entity = new CreditNote
            {
                VoucherType = voucherType,
                BranchId = 1,
                PointOfSale = 2,
                CreditNoteNumber = nroNC,
                CreditNoteDate = GetDateTime(readerH, 2) ?? DateTime.Now,
                CustomerId = ClienteAccessIdToSqlId[codCliente],
                SalesRepId = !string.IsNullOrEmpty(vendedorLegajo) && VendedorLegajoToId.ContainsKey(vendedorLegajo) 
                    ? VendedorLegajoToId[vendedorLegajo] 
                    : null,
                Subtotal = GetDecimal(readerH, 5),
                VATPercent = GetDecimal(readerH, 6),
                VATAmount = GetDecimal(readerH, 7),
                IIBBPercent = GetDecimal(readerH, 8),
                IIBBAmount = GetDecimal(readerH, 9),
                DiscountPercent = GetDecimal(readerH, 10),
                DiscountAmount = GetDecimal(readerH, 11),
                Total = GetDecimal(readerH, 12),
                CAE = GetString(readerH, 13),
                CAEExpirationDate = GetDateTime(readerH, 14),
                SalesCondition = GetString(readerH, 15),
                IsVoided = GetString(readerH, 16)?.ToUpper() == "S" || GetBool(readerH, 16)
            };
            
            allNotes.Add(entity);
            noteKeys.Add((tipoNC, nroNC));
        }
        
        Console.Write($"({allNotes.Count} headers)... ");
        
        if (allNotes.Count > 0)
        {
            await db.BulkInsertAsync(allNotes, new BulkConfig { SetOutputIdentity = true });
            for (int i = 0; i < allNotes.Count; i++)
            {
                ncMap[noteKeys[i]] = allNotes[i].Id;
            }
        }
        
        Console.WriteLine("OK");
        
        // Details
        Console.Write("Migrating NotaCreditoDetalles (CreditNoteDetails)... ");
        using var cmdD = new OleDbCommand(@"
            SELECT TipoNotaCredito, NroNotaCredito, ItemNotaCredito, IDCodProd, Cantidad, 
                   PrecioUnitario, PorcentajeDescuento, ImporteDescuento, TotalLinea
            FROM NotaCreditoD
            ORDER BY TipoNotaCredito, NroNotaCredito, ItemNotaCredito", access);
        using var readerD = cmdD.ExecuteReader();
        
        var allDetails = new List<CreditNoteDetail>();
        
        while (readerD.Read())
        {
            var tipoNC = GetString(readerD, 0) ?? "B";
            var nroNC = readerD.GetInt32(1);
            var codProd = GetString(readerD, 3);
            
            if (!ncMap.ContainsKey((tipoNC, nroNC))) continue;
            if (string.IsNullOrEmpty(codProd) || !ProductoCodigoToId.ContainsKey(codProd)) continue;
            
            var entity = new CreditNoteDetail
            {
                CreditNoteId = ncMap[(tipoNC, nroNC)],
                ItemNumber = readerD.GetInt32(2),
                ProductId = ProductoCodigoToId[codProd],
                Quantity = GetDecimal(readerD, 4),
                UnitPrice = GetDecimal(readerD, 5),
                DiscountPercent = GetDecimal(readerD, 6),
                DiscountAmount = GetDecimal(readerD, 7),
                Subtotal = GetDecimal(readerD, 8)
            };
            
            allDetails.Add(entity);
        }
        
        Console.Write($"({allDetails.Count} details)... ");
        
        if (allDetails.Count > 0)
        {
            await db.BulkInsertAsync(allDetails);
        }
        
        Console.WriteLine("OK");
    }

    static async Task MigrateNotasDebito(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating NotasDebito (DebitNotes)... ");
        
        var ndMap = new Dictionary<(string, int), int>();
        
        using var cmdH = new OleDbCommand(@"
            SELECT TipoDebito, NroDebito, FechaDebito, CodCliente, CodVendedor,
                   SubTotalDebito, PorcentajeIVA, TotalIVA, AlicuotaIIBB, ImportePercepIIBB,
                   PorcentajeDesc, ImporteDesc, TotalDebito, Cae, FechaVC, CondicionVenta, Cancelada
            FROM NotaDebitoC
            ORDER BY TipoDebito, NroDebito", access);
        using var readerH = cmdH.ExecuteReader();
        
        var allNotes = new List<DebitNote>();
        var noteKeys = new List<(string tipo, int numero)>();
        
        while (readerH.Read())
        {
            var tipoND = GetString(readerH, 0) ?? "B";
            var nroND = readerH.GetInt32(1);
            var codCliente = GetInt(readerH, 3);
            
            if (!ClienteAccessIdToSqlId.ContainsKey(codCliente)) continue;
            
            var vendedorLegajo = GetString(readerH, 4);
            var voucherType = tipoND == "A" ? VoucherType.DebitNoteA : VoucherType.DebitNoteB;
            
            var entity = new DebitNote
            {
                VoucherType = voucherType,
                BranchId = 1,
                PointOfSale = 2,
                DebitNoteNumber = nroND,
                DebitNoteDate = GetDateTime(readerH, 2) ?? DateTime.Now,
                CustomerId = ClienteAccessIdToSqlId[codCliente],
                SalesRepId = !string.IsNullOrEmpty(vendedorLegajo) && VendedorLegajoToId.ContainsKey(vendedorLegajo) 
                    ? VendedorLegajoToId[vendedorLegajo] 
                    : null,
                Subtotal = GetDecimal(readerH, 5),
                VATPercent = GetDecimal(readerH, 6),
                VATAmount = GetDecimal(readerH, 7),
                IIBBPercent = GetDecimal(readerH, 8),
                IIBBAmount = GetDecimal(readerH, 9),
                DiscountPercent = GetDecimal(readerH, 10),
                DiscountAmount = GetDecimal(readerH, 11),
                Total = GetDecimal(readerH, 12),
                CAE = GetString(readerH, 13),
                CAEExpirationDate = GetDateTime(readerH, 14),
                SalesCondition = GetString(readerH, 15),
                IsVoided = GetString(readerH, 16)?.ToUpper() == "S" || GetBool(readerH, 16)
            };
            
            allNotes.Add(entity);
            noteKeys.Add((tipoND, nroND));
        }
        
        Console.Write($"({allNotes.Count} headers)... ");
        
        if (allNotes.Count > 0)
        {
            await db.BulkInsertAsync(allNotes, new BulkConfig { SetOutputIdentity = true });
            for (int i = 0; i < allNotes.Count; i++)
            {
                ndMap[noteKeys[i]] = allNotes[i].Id;
            }
        }
        
        Console.WriteLine("OK");
        
        // Details
        Console.Write("Migrating NotaDebitoDetalles (DebitNoteDetails)... ");
        using var cmdD = new OleDbCommand(@"
            SELECT TipoDebito, NroDebito, ItemDebito, IDCodProd, Cantidad, 
                   PrecioUnitario, PorcentajeDescuento, ImporteDescuento, TotalLinea
            FROM NotaDebitoD
            ORDER BY TipoDebito, NroDebito, ItemDebito", access);
        using var readerD = cmdD.ExecuteReader();
        
        var allDetails = new List<DebitNoteDetail>();
        
        while (readerD.Read())
        {
            var tipoND = GetString(readerD, 0) ?? "B";
            var nroND = readerD.GetInt32(1);
            var codProd = GetString(readerD, 3);
            
            if (!ndMap.ContainsKey((tipoND, nroND))) continue;
            if (string.IsNullOrEmpty(codProd) || !ProductoCodigoToId.ContainsKey(codProd)) continue;
            
            var entity = new DebitNoteDetail
            {
                DebitNoteId = ndMap[(tipoND, nroND)],
                ItemNumber = readerD.GetInt32(2),
                ProductId = ProductoCodigoToId[codProd],
                Quantity = GetDecimal(readerD, 4),
                UnitPrice = GetDecimal(readerD, 5),
                DiscountPercent = GetDecimal(readerD, 6),
                DiscountAmount = GetDecimal(readerD, 7),
                Subtotal = GetDecimal(readerD, 8)
            };
            
            allDetails.Add(entity);
        }
        
        Console.Write($"({allDetails.Count} details)... ");
        
        if (allDetails.Count > 0)
        {
            await db.BulkInsertAsync(allDetails);
        }
        Console.WriteLine("OK");
    }

    static async Task MigrateNotasDebitoInternas(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating NotasDebitoInternas (InternalDebitNotes)... ");
        
        var ndiMap = new Dictionary<(string, int), int>();
        
        using var cmdH = new OleDbCommand(@"
            SELECT TipoDebitoI, NroDebitoI, FechaDebitoI, CodCliente, CodVendedor,
                   SubTotalDebitoI, PorcentajeDesc, ImporteDesc, TotalDebitoI,
                   UnidadNegocio, CondicionVenta, Cancelada
            FROM NotaDebitoIC
            ORDER BY TipoDebitoI, NroDebitoI", access);
        using var readerH = cmdH.ExecuteReader();
        
        var allNotes = new List<InternalDebitNote>();
        var noteKeys = new List<(string tipo, int numero)>();
        
        while (readerH.Read())
        {
            var tipoNDI = GetString(readerH, 0) ?? "I";
            var nroNDI = readerH.GetInt32(1);
            var codCliente = GetInt(readerH, 3);
            
            if (!ClienteAccessIdToSqlId.ContainsKey(codCliente)) continue;
            
            var vendedorLegajo = GetString(readerH, 4);
            
            var entity = new InternalDebitNote
            {
                VoucherType = VoucherType.InternalDebitNote,
                BranchId = 1,
                InternalDebitNumber = nroNDI,
                DebitDate = GetDateTime(readerH, 2) ?? DateTime.Now,
                CustomerId = ClienteAccessIdToSqlId[codCliente],
                SalesRepId = !string.IsNullOrEmpty(vendedorLegajo) && VendedorLegajoToId.ContainsKey(vendedorLegajo) 
                    ? VendedorLegajoToId[vendedorLegajo] 
                    : null,
                Subtotal = GetDecimal(readerH, 5),
                DiscountPercent = GetDecimal(readerH, 6),
                DiscountAmount = GetDecimal(readerH, 7),
                Total = GetDecimal(readerH, 8),
                BusinessUnit = GetString(readerH, 9),
                SalesCondition = GetString(readerH, 10),
                IsVoided = GetString(readerH, 11)?.ToUpper() == "S" || GetBool(readerH, 11)
            };
            
            allNotes.Add(entity);
            noteKeys.Add((tipoNDI, nroNDI));
        }
        
        Console.Write($"({allNotes.Count} headers)... ");
        
        if (allNotes.Count > 0)
        {
            await db.BulkInsertAsync(allNotes, new BulkConfig { SetOutputIdentity = true });
            for (int i = 0; i < allNotes.Count; i++)
            {
                ndiMap[noteKeys[i]] = allNotes[i].Id;
            }
        }
        
        Console.WriteLine("OK");
        
        // Details
        Console.Write("Migrating NotaDebitoInternaDetalles (InternalDebitNoteDetails)... ");
        using var cmdD = new OleDbCommand(@"
            SELECT TipoDebitoI, NroDebitoI, ItemDebitoI, IDCodProd, Cantidad, 
                   PrecioUnitario, PorcentajeDescuento, ImporteDescuento, TotalLinea
            FROM NotaDebitoID
            ORDER BY TipoDebitoI, NroDebitoI, ItemDebitoI", access);
        using var readerD = cmdD.ExecuteReader();
        
        var allDetails = new List<InternalDebitNoteDetail>();
        
        while (readerD.Read())
        {
            var tipoNDI = GetString(readerD, 0) ?? "I";
            var nroNDI = readerD.GetInt32(1);
            var codProd = GetString(readerD, 3);
            
            if (!ndiMap.ContainsKey((tipoNDI, nroNDI))) continue;
            if (string.IsNullOrEmpty(codProd) || !ProductoCodigoToId.ContainsKey(codProd)) continue;
            
            var entity = new InternalDebitNoteDetail
            {
                InternalDebitNoteId = ndiMap[(tipoNDI, nroNDI)],
                ItemNumber = readerD.GetInt32(2),
                ProductId = ProductoCodigoToId[codProd],
                Quantity = GetDecimal(readerD, 4),
                UnitPrice = GetDecimal(readerD, 5),
                DiscountPercent = GetDecimal(readerD, 6),
                DiscountAmount = GetDecimal(readerD, 7),
                Subtotal = GetDecimal(readerD, 8)
            };
            
            allDetails.Add(entity);
        }
        
        Console.Write($"({allDetails.Count} details)... ");
        
        if (allDetails.Count > 0)
        {
            await db.BulkInsertAsync(allDetails);
        }
        
        Console.WriteLine("OK");
    }

    static async Task MigrateConsignaciones(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating Consignaciones... ");
        
        var consigMap = new Dictionary<int, int>();
        
        using var cmdH = new OleDbCommand(@"
            SELECT NroConsignacion, FechaC, IdCliente, CodVendedor
            FROM ConsignacionesC
            ORDER BY NroConsignacion", access);
        using var readerH = cmdH.ExecuteReader();
        
        int countH = 0;
        while (readerH.Read())
        {
            var nroConsig = readerH.GetInt32(0);
            var codCliente = GetInt(readerH, 2);
            
            if (!ClienteAccessIdToSqlId.ContainsKey(codCliente)) continue;
            
            var vendedorLegajo = GetString(readerH, 3);
            
            var entity = new Consignment
            {
                ConsignmentNumber = nroConsig,
                ConsignmentDate = GetDateTime(readerH, 1) ?? DateTime.Now,
                CustomerId = ClienteAccessIdToSqlId[codCliente],
                SalesRepId = !string.IsNullOrEmpty(vendedorLegajo) && VendedorLegajoToId.ContainsKey(vendedorLegajo) 
                    ? VendedorLegajoToId[vendedorLegajo] 
                    : null,
                IsActive = true
            };
            
            db.Consignments.Add(entity);
            await db.SaveChangesAsync();
            consigMap[nroConsig] = entity.Id;
            countH++;
        }
        
        Console.WriteLine($"OK ({countH} headers)");
        
        // Details
        Console.Write("Migrating ConsignacionDetalles... ");
        using var cmdD = new OleDbCommand(@"
            SELECT NroConsignacion, ItemConsignacion, IdCodProd, Cantidad, UnidadMedida
            FROM ConsignacionesD
            ORDER BY NroConsignacion, ItemConsignacion", access);
        using var readerD = cmdD.ExecuteReader();
        
        int countD = 0;
        while (readerD.Read())
        {
            var nroConsig = readerD.GetInt32(0);
            var codProd = GetString(readerD, 2);
            
            if (!consigMap.ContainsKey(nroConsig)) continue;
            if (string.IsNullOrEmpty(codProd) || !ProductoCodigoToId.ContainsKey(codProd)) continue;
            
            var entity = new ConsignmentDetail
            {
                ConsignmentId = consigMap[nroConsig],
                ItemNumber = readerD.GetInt32(1),
                ProductId = ProductoCodigoToId[codProd],
                Quantity = GetDecimal(readerD, 3),
                UnitOfMeasure = GetString(readerD, 4)
            };
            
            db.ConsignmentDetails.Add(entity);
            countD++;
        }
        
        await db.SaveChangesAsync();
        Console.WriteLine($"OK ({countD} details)");
    }

    #endregion

    #region Payments and Current Account

    static async Task MigratePagos(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating Pagos... ");
        
        var pagoMap = new Dictionary<(int, double), int>();
        
        using var cmdH = new OleDbCommand(@"
            SELECT IdSucursal, NroPago, FechaPago, IDCliente, TotalAbonado, Anulado, Corresponde
            FROM PagoC
            ORDER BY IdSucursal, NroPago", access);
        using var readerH = cmdH.ExecuteReader();
        
        int countH = 0;
        while (readerH.Read())
        {
            var idSucursal = readerH.GetInt32(0);
            var nroPago = readerH.GetDouble(1);
            var codCliente = GetInt(readerH, 3);
            
            if (!ClienteAccessIdToSqlId.ContainsKey(codCliente)) continue;
            
            // Map sucursal to branch using lookup
            var branchId = BranchAccessIdToSqlId.ContainsKey(idSucursal) 
                ? BranchAccessIdToSqlId[idSucursal] 
                : 1;
            var corresponde = GetString(readerH, 6) ?? "";
            
            // Determine which line the payment applies to
            var appliesTo = corresponde.ToUpper().Contains("L2") || corresponde.ToUpper().Contains("PRESU")
                ? AccountLineType.Budget
                : AccountLineType.Billing;
            
            var entity = new Payment
            {
                BranchId = branchId,
                PaymentNumber = (long)nroPago,
                PaymentDate = GetDateTime(readerH, 2) ?? DateTime.Now,
                CustomerId = ClienteAccessIdToSqlId[codCliente],
                TotalAmount = GetDecimal(readerH, 4),
                AppliesTo = appliesTo,
                AppliesToDescription = corresponde,
                IsVoided = GetString(readerH, 5)?.ToUpper() == "S"
            };
            
            db.Payments.Add(entity);
            await db.SaveChangesAsync();
            pagoMap[(idSucursal, nroPago)] = entity.Id;
            countH++;
        }
        
        Console.WriteLine($"OK ({countH} headers)");
        
        // Details
        Console.Write("Migrating PagoDetalles... ");
        using var cmdD = new OleDbCommand(@"
            SELECT IdSucursal, NroPago, LineaPago, FormaPago, ImportePago, Observaciones
            FROM PagoD
            ORDER BY IdSucursal, NroPago, LineaPago", access);
        using var readerD = cmdD.ExecuteReader();
        
        int countD = 0;
        while (readerD.Read())
        {
            var idSucursal = readerD.GetInt32(0);
            var nroPago = readerD.GetDouble(1);
            var formaPago = GetString(readerD, 3);
            
            if (!pagoMap.ContainsKey((idSucursal, nroPago))) continue;
            
            // Get payment method ID
            int paymentMethodId = 1; // Default to Efectivo
            if (!string.IsNullOrEmpty(formaPago) && PaymentMethodCodigoToId.ContainsKey(formaPago))
            {
                paymentMethodId = PaymentMethodCodigoToId[formaPago];
            }
            
            var entity = new PaymentDetail
            {
                PaymentId = pagoMap[(idSucursal, nroPago)],
                LineNumber = readerD.GetInt32(2),
                PaymentMethodId = paymentMethodId,
                Amount = GetDecimal(readerD, 4),
                Notes = GetString(readerD, 5)
            };
            
            db.PaymentDetails.Add(entity);
            countD++;
        }
        
        await db.SaveChangesAsync();
        Console.WriteLine($"OK ({countD} details)");
    }

    static async Task MigrateCtaCte(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating CtaCte (CurrentAccounts)... ");
        
        using var cmd = new OleDbCommand(@"
            SELECT IDCliente, SaldoL1, SaldoL2, SaldoTotal, FechaActSaldo
            FROM CtaCte", access);
        using var reader = cmd.ExecuteReader();
        
        int count = 0;
        while (reader.Read())
        {
            var codCliente = reader.GetInt32(0);
            
            if (!ClienteAccessIdToSqlId.ContainsKey(codCliente)) continue;
            
            var entity = new CurrentAccount
            {
                CustomerId = ClienteAccessIdToSqlId[codCliente],
                BillingBalance = GetDecimal(reader, 1),
                BudgetBalance = GetDecimal(reader, 2),
                TotalBalance = GetDecimal(reader, 3),
                LastUpdated = GetDateTime(reader, 4) ?? DateTime.Now
            };
            
            db.CurrentAccounts.Add(entity);
            count++;
        }
        
        await db.SaveChangesAsync();
        Console.WriteLine($"OK ({count} records)");
    }

    static async Task MigrateMovimientosCtaCte(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating MovimientosCtaCte (CurrentAccountMovements)... ");
        
        using var cmd = new OleDbCommand(@"
            SELECT Fecha, IDCliente, TipoDoc, NroDoc, ImporteLinea1, ImporteLinea2
            FROM MovimientosCtaCte
            ORDER BY Fecha, IDCliente", access);
        using var reader = cmd.ExecuteReader();
        
        int count = 0;
        decimal runningBilling = 0;
        decimal runningBudget = 0;
        int lastCustomerId = 0;
        
        while (reader.Read())
        {
            var codCliente = (int)GetDouble(reader, 1);
            
            if (!ClienteAccessIdToSqlId.ContainsKey(codCliente)) continue;
            
            var customerId = ClienteAccessIdToSqlId[codCliente];
            
            // Reset running balances when customer changes
            if (customerId != lastCustomerId)
            {
                runningBilling = 0;
                runningBudget = 0;
                lastCustomerId = customerId;
            }
            
            var tipoDoc = GetString(reader, 2) ?? "";
            var billingAmount = GetDecimal(reader, 4);
            var budgetAmount = GetDecimal(reader, 5);
            
            runningBilling += billingAmount;
            runningBudget += budgetAmount;
            
            // Map document type
            var documentType = tipoDoc.ToUpper() switch
            {
                "FA" or "FB" => DocumentType.Invoice,
                "NC" or "NCA" or "NCB" => DocumentType.CreditNote,
                "ND" or "NDA" or "NDB" => DocumentType.DebitNote,
                "NDI" => DocumentType.InternalDebitNote,
                "PR" or "PRESU" => DocumentType.Quote,
                "PA" or "PAGO" => DocumentType.Payment,
                "RE" or "REC" or "RECIBO" => DocumentType.Receipt,
                _ => DocumentType.Other
            };
            
            var entity = new CurrentAccountMovement
            {
                MovementDate = GetDateTime(reader, 0) ?? DateTime.Now,
                CustomerId = customerId,
                DocumentType = documentType,
                DocumentNumber = (long)GetDouble(reader, 3),
                BillingAmount = billingAmount,
                BudgetAmount = budgetAmount,
                BillingRunningBalance = runningBilling,
                BudgetRunningBalance = runningBudget,
                Description = tipoDoc
            };
            
            db.CurrentAccountMovements.Add(entity);
            count++;
            
            // Save in batches to avoid memory issues
            if (count % 1000 == 0)
            {
                await db.SaveChangesAsync();
            }
        }
        
        await db.SaveChangesAsync();
        Console.WriteLine($"OK ({count} records)");
    }

    #endregion

    #region Helper Methods

    static string? GetString(IDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) return null;
        return reader.GetValue(ordinal)?.ToString()?.Trim();
    }

    static int GetInt(IDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) return 0;
        var value = reader.GetValue(ordinal);
        if (value is int i) return i;
        if (value is long l) return (int)l;
        if (value is double d) return (int)d;
        if (int.TryParse(value?.ToString(), out var result)) return result;
        return 0;
    }

    static double GetDouble(IDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) return 0;
        var value = reader.GetValue(ordinal);
        if (value is double d) return d;
        if (value is float f) return f;
        if (value is decimal dec) return (double)dec;
        if (double.TryParse(value?.ToString(), out var result)) return result;
        return 0;
    }

    static decimal GetDecimal(IDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) return 0;
        var value = reader.GetValue(ordinal);
        if (value is decimal dec) return dec;
        if (value is double d)
        {
            // Handle overflow for very large/small doubles
            if (d > (double)decimal.MaxValue) return decimal.MaxValue;
            if (d < (double)decimal.MinValue) return decimal.MinValue;
            if (double.IsNaN(d) || double.IsInfinity(d)) return 0;
            return (decimal)d;
        }
        if (value is float f)
        {
            if (float.IsNaN(f) || float.IsInfinity(f)) return 0;
            return (decimal)f;
        }
        if (value is int i) return i;
        if (decimal.TryParse(value?.ToString(), out var result)) return result;
        return 0;
    }

    static DateTime? GetDateTime(IDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) return null;
        var value = reader.GetValue(ordinal);
        if (value is DateTime dt) return dt;
        if (DateTime.TryParse(value?.ToString(), out var result)) return result;
        return null;
    }

    static bool GetBool(IDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) return false;
        var value = reader.GetValue(ordinal);
        if (value is bool b) return b;
        if (value is int i) return i != 0;
        if (value is string s) return s.ToUpper() == "S" || s.ToUpper() == "TRUE" || s == "1";
        return false;
    }

    #endregion
}
