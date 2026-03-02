using System.Data;
using System.Data.OleDb;
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
            PaymentMethodCodigoToId[e.Code] = e.Id;
        }

        using var cmd = new OleDbCommand("SELECT IDPago, Descripcion FROM CodPago", access);
        using var reader = cmd.ExecuteReader();
        
        int count = 0;
        while (reader.Read())
        {
            var codigo = reader.GetString(0).Trim();
            var descripcion = reader.GetString(1).Trim();
            
            if (!PaymentMethodCodigoToId.ContainsKey(codigo))
            {
                // Determine type based on code
                var type = codigo.ToUpper() switch
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
                
                var entity = new PaymentMethod
                {
                    Code = codigo,
                    Description = descripcion,
                    Type = type,
                    RequiresDetail = type == PaymentMethodType.Check || type == PaymentMethodType.Barter,
                    IsActive = true
                };
                db.PaymentMethods.Add(entity);
                await db.SaveChangesAsync();
                PaymentMethodCodigoToId[codigo] = entity.Id;
            }
            count++;
        }
        
        Console.WriteLine($"OK ({count} records)");
    }

    #endregion

    #region Vendedores and Depositos

    static async Task MigrateVendedores(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating Vendedores (Empleados)... ");
        
        using var cmd = new OleDbCommand(@"
            SELECT Legajo, Nombre, CUIL, Domicilio, Localidad, Prov, CP, 
                   DNI, Tel, Cel, emaill, FechaNacimiento, FechaIngreso, Comision, Observaciones
            FROM Empleados", access);
        using var reader = cmd.ExecuteReader();
        
        int count = 0;
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
            
            db.Vendedores.Add(entity);
            await db.SaveChangesAsync();
            VendedorLegajoToId[legajo] = entity.Id;
            count++;
        }
        
        Console.WriteLine($"OK ({count} records)");
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
        
        using var cmd = new OleDbCommand(@"
            SELECT CodProd, Descripcion, PrecioUnitarioFactura, PrecioUnitarioPresupuesto, 
                   Rubro, UnidadMedida, PuntoPedido, Observaciones
            FROM Productos", access);
        using var reader = cmd.ExecuteReader();
        
        int count = 0;
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
            
            db.Productos.Add(entity);
            await db.SaveChangesAsync();
            ProductoCodigoToId[codigo] = entity.Id;
            count++;
        }
        
        Console.WriteLine($"OK ({count} records)");
    }

    static async Task MigrateClientes(OleDbConnection access, SPCDbContext db)
    {
        Console.Write("Migrating Clientes... ");
        
        using var cmd = new OleDbCommand(@"
            SELECT IDCliente, RazonSocial, NombreFantasia, CUIT, Domicilio, Localidad, 
                   Prov, CP, Tel, Cel, email, CondicionIva, Vendedor, ZonaVenta,
                   PorcentajeDescuento, LimiteCredito, Observaciones
            FROM Clientes", access);
        using var reader = cmd.ExecuteReader();
        
        int count = 0;
        while (reader.Read())
        {
            var accessId = reader.GetInt32(0);
            var condicionIva = GetString(reader, 11);
            var vendedorLegajo = GetString(reader, 12);
            
            var entity = new Cliente
            {
                RazonSocial = GetString(reader, 1) ?? "Sin Nombre",
                NombreFantasia = GetString(reader, 2),
                CUIT = GetString(reader, 3),
                Direccion = GetString(reader, 4),
                Localidad = GetString(reader, 5),
                Provincia = GetString(reader, 6),
                CodigoPostal = GetString(reader, 7),
                Telefono = GetString(reader, 8),
                Celular = GetString(reader, 9),
                Email = GetString(reader, 10),
                CondicionIvaId = !string.IsNullOrEmpty(condicionIva) && CondicionIvaCodigoToId.ContainsKey(condicionIva) 
                    ? CondicionIvaCodigoToId[condicionIva] 
                    : null,
                VendedorId = !string.IsNullOrEmpty(vendedorLegajo) && VendedorLegajoToId.ContainsKey(vendedorLegajo) 
                    ? VendedorLegajoToId[vendedorLegajo] 
                    : null,
                PorcentajeDescuento = GetDecimal(reader, 14),
                LimiteCredito = GetDecimal(reader, 15),
                Observaciones = GetString(reader, 16),
                Activo = true,
                FechaAlta = DateTime.Now
            };
            
            db.Clientes.Add(entity);
            await db.SaveChangesAsync();
            ClienteAccessIdToSqlId[accessId] = entity.Id;
            count++;
        }
        
        Console.WriteLine($"OK ({count} records)");
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
        
        // Headers
        using var cmdH = new OleDbCommand(@"
            SELECT TipoFactura, NroFactura, FechaFactura, CodCliente, CodVendedor,
                   SubTotalFactura, PorcentajeIVA, TotalIVA, AlicuotaIIBB, ImportePercepIIBB,
                   PorcentajeDesc, ImporteDesc, TotalFactura, Cae, FechaVC, 
                   CondicionVenta, FormaPago, UnidadNegocio, Cancelada, IdSucursal, NroRemito, AclaracionFactura
            FROM FacturaC
            ORDER BY TipoFactura, NroFactura", access);
        using var readerH = cmdH.ExecuteReader();
        
        int countH = 0;
        while (readerH.Read())
        {
            var tipoFactura = GetString(readerH, 0) ?? "B";
            var nroFactura = readerH.GetInt32(1);
            var codCliente = GetInt(readerH, 3);
            
            if (!ClienteAccessIdToSqlId.ContainsKey(codCliente)) continue;
            
            var vendedorLegajo = GetString(readerH, 4);
            var idSucursal = GetInt(readerH, 19);
            
            // Map sucursal to branch (2=Calle, 5=Distribuidora)
            var branchId = idSucursal == 5 ? 2 : 1;
            var puntoVenta = idSucursal == 5 ? 5 : 2;
            
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
            
            db.Facturas.Add(entity);
            await db.SaveChangesAsync();
            FacturaAccessToSqlId[(tipoFactura, nroFactura)] = entity.Id;
            countH++;
        }
        
        Console.WriteLine($"OK ({countH} headers)");
        
        // Details
        Console.Write("Migrating FacturaDetalles... ");
        using var cmdD = new OleDbCommand(@"
            SELECT TipoFactura, NroFactura, ItemFactura, IDCodProd, Cantidad, 
                   PrecioUnitario, PorcentajeDescuento, TotalLinea
            FROM FacturaD
            ORDER BY TipoFactura, NroFactura, ItemFactura", access);
        using var readerD = cmdD.ExecuteReader();
        
        int countD = 0;
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
            
            db.FacturaDetalles.Add(entity);
            countD++;
        }
        
        await db.SaveChangesAsync();
        Console.WriteLine($"OK ({countD} details)");
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
        
        int countH = 0;
        while (readerH.Read())
        {
            var idSucursal = readerH.GetInt32(0);
            var nroRemito = readerH.GetInt32(1);
            var codCliente = GetInt(readerH, 3);
            
            if (!ClienteAccessIdToSqlId.ContainsKey(codCliente)) continue;
            
            var vendedorLegajo = GetString(readerH, 4);
            var nroFactura = GetInt(readerH, 6);
            var tipoFactura = GetString(readerH, 7);
            
            var branchId = idSucursal == 5 ? 2 : 1;
            var puntoVenta = idSucursal == 5 ? 5 : 2;
            
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
            
            db.Remitos.Add(entity);
            await db.SaveChangesAsync();
            RemitoAccessToSqlId[(idSucursal, nroRemito)] = entity.Id;
            countH++;
        }
        
        Console.WriteLine($"OK ({countH} headers)");
        
        // Details
        Console.Write("Migrating RemitoDetalles... ");
        using var cmdD = new OleDbCommand(@"
            SELECT IdSucursal, NroRemito, ItemRemito, IDCodProd, Cantidad
            FROM RemitoD
            ORDER BY IdSucursal, NroRemito, ItemRemito", access);
        using var readerD = cmdD.ExecuteReader();
        
        int countD = 0;
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
            
            db.RemitoDetalles.Add(entity);
            countD++;
        }
        
        await db.SaveChangesAsync();
        Console.WriteLine($"OK ({countD} details)");
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
        
        int countH = 0;
        while (readerH.Read())
        {
            var nroPresu = readerH.GetInt32(0);
            var codCliente = GetInt(readerH, 2);
            
            if (!ClienteAccessIdToSqlId.ContainsKey(codCliente)) continue;
            
            var vendedorLegajo = GetString(readerH, 3);
            
            var entity = new Quote
            {
                BranchId = 1, // Default to Calle
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
            
            db.Quotes.Add(entity);
            await db.SaveChangesAsync();
            quoteMap[nroPresu] = entity.Id;
            countH++;
        }
        
        Console.WriteLine($"OK ({countH} headers)");
        
        // Details
        Console.Write("Migrating PresupuestoDetalles (QuoteDetails)... ");
        using var cmdD = new OleDbCommand(@"
            SELECT NroPresu, ItemPresu, CodProd, Cantidad, 
                   PrecioUnitario, PorcentajeDescuento, ImporteDescuento, TotalLinea
            FROM PresupuestoD
            ORDER BY NroPresu, ItemPresu", access);
        using var readerD = cmdD.ExecuteReader();
        
        int countD = 0;
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
            
            db.QuoteDetails.Add(entity);
            countD++;
        }
        
        await db.SaveChangesAsync();
        Console.WriteLine($"OK ({countD} details)");
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
        
        int countH = 0;
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
            
            db.CreditNotes.Add(entity);
            await db.SaveChangesAsync();
            ncMap[(tipoNC, nroNC)] = entity.Id;
            countH++;
        }
        
        Console.WriteLine($"OK ({countH} headers)");
        
        // Details
        Console.Write("Migrating NotaCreditoDetalles (CreditNoteDetails)... ");
        using var cmdD = new OleDbCommand(@"
            SELECT TipoNotaCredito, NroNotaCredito, ItemNotaCredito, IDCodProd, Cantidad, 
                   PrecioUnitario, PorcentajeDescuento, ImporteDescuento, TotalLinea
            FROM NotaCreditoD
            ORDER BY TipoNotaCredito, NroNotaCredito, ItemNotaCredito", access);
        using var readerD = cmdD.ExecuteReader();
        
        int countD = 0;
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
            
            db.CreditNoteDetails.Add(entity);
            countD++;
        }
        
        await db.SaveChangesAsync();
        Console.WriteLine($"OK ({countD} details)");
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
        
        int countH = 0;
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
            
            db.DebitNotes.Add(entity);
            await db.SaveChangesAsync();
            ndMap[(tipoND, nroND)] = entity.Id;
            countH++;
        }
        
        Console.WriteLine($"OK ({countH} headers)");
        
        // Details
        Console.Write("Migrating NotaDebitoDetalles (DebitNoteDetails)... ");
        using var cmdD = new OleDbCommand(@"
            SELECT TipoDebito, NroDebito, ItemDebito, IDCodProd, Cantidad, 
                   PrecioUnitario, PorcentajeDescuento, ImporteDescuento, TotalLinea
            FROM NotaDebitoD
            ORDER BY TipoDebito, NroDebito, ItemDebito", access);
        using var readerD = cmdD.ExecuteReader();
        
        int countD = 0;
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
            
            db.DebitNoteDetails.Add(entity);
            countD++;
        }
        
        await db.SaveChangesAsync();
        Console.WriteLine($"OK ({countD} details)");
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
        
        int countH = 0;
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
            
            db.InternalDebitNotes.Add(entity);
            await db.SaveChangesAsync();
            ndiMap[(tipoNDI, nroNDI)] = entity.Id;
            countH++;
        }
        
        Console.WriteLine($"OK ({countH} headers)");
        
        // Details
        Console.Write("Migrating NotaDebitoInternaDetalles (InternalDebitNoteDetails)... ");
        using var cmdD = new OleDbCommand(@"
            SELECT TipoDebitoI, NroDebitoI, ItemDebitoI, IDCodProd, Cantidad, 
                   PrecioUnitario, PorcentajeDescuento, ImporteDescuento, TotalLinea
            FROM NotaDebitoID
            ORDER BY TipoDebitoI, NroDebitoI, ItemDebitoI", access);
        using var readerD = cmdD.ExecuteReader();
        
        int countD = 0;
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
            
            db.InternalDebitNoteDetails.Add(entity);
            countD++;
        }
        
        await db.SaveChangesAsync();
        Console.WriteLine($"OK ({countD} details)");
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
            
            var branchId = idSucursal == 5 ? 2 : 1;
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
        if (value is double d) return (decimal)d;
        if (value is float f) return (decimal)f;
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
