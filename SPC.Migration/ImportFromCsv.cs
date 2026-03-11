using System.Data.OleDb;
using System.Globalization;
using System.Text;
using EFCore.BulkExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.Migration;

/// <summary>
/// Import documents from CSV files exported from Access
/// </summary>
public static class CsvImporter
{
    private const string AccessDbPath = @"C:\TrabajosActivos\SPC-Core\Db_SPC_SI.mdb";
    private const string DataDir = @"C:\Programmes\spc-software\SPC.Migration\data";
    private const int BatchSize = 2000;
    private static readonly string SkipLogPath = Path.Combine(DataDir, "migration_skipped_rows.log");

    /// <summary>
    /// Bulk insert with IDENTITY_INSERT ON to preserve original IDs
    /// Uses raw SQL since EFCore.BulkExtensions 10.x doesn't have KeepIdentity
    /// </summary>
    private static async Task BulkInsertWithIdentityAsync<T>(SPCDbContext db, List<T> items, string tableName) where T : class
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
                BatchSize = BatchSize
            };

            // Create DataTable from items
            var dataTable = ToDataTable(items);
            
            // Map columns
            foreach (System.Data.DataColumn column in dataTable.Columns)
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
    private static System.Data.DataTable ToDataTable<T>(List<T> items)
    {
        var table = new System.Data.DataTable();
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
    private static bool IsNavigationProperty(System.Reflection.PropertyInfo prop)
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

    // Mapping dictionaries
    private static readonly Dictionary<int, int> CustomerMap = new();
    private static readonly Dictionary<string, int> SalesRepMap = new();
    private static readonly Dictionary<string, int> ProductMap = new();
    private static readonly Dictionary<int, int> BranchMap = new();
    private static readonly Dictionary<string, int> PaymentMethodMap = new();
    private static readonly Dictionary<(string, int), int> InvoiceMap = new();
    private static readonly Dictionary<(int, int), int> DeliveryNoteMap = new();
    private static readonly Dictionary<int, int> QuoteMap = new();
    private static readonly Dictionary<(string, int), int> CreditNoteMap = new();
    private static readonly Dictionary<(string, int), int> DebitNoteMap = new();
    private static readonly Dictionary<(string, int), int> InternalDebitMap = new();
    private static readonly Dictionary<int, int> ConsignmentMap = new();
    private static readonly Dictionary<(int, double), int> PaymentMap = new();

    public static async Task RunAsync(SPCDbContext db)
    {
        Console.WriteLine("=== CSV Import - Full Database ===");
        Console.WriteLine();

        // Phase 1: Auxiliary tables
        await ImportCondicionesIvaAsync(db);
        await ImportUnidadesMedidaAsync(db);
        await ImportCategorysAsync(db);
        await ImportSucursalesAsync(db);
        await ImportSalesRepesAsync(db);
        await ImportWarehousesAsync(db);
        
        // Phase 2: Master data (with preserved IDs)
        await ImportCustomersAsync(db);
        await ImportProductsAsync(db);
        await ImportStockAsync(db);
        
        Console.WriteLine();
        Console.WriteLine("Loading mappings for documents...");
        await LoadMappingsAsync(db);
        Console.WriteLine();

        // Phase 3: Documents
        await ImportInvoicesAsync(db);
        await ImportInvoiceDetailsAsync(db);
        await ImportDeliveryNotesAsync(db);
        await ImportDeliveryNoteDetailsAsync(db);
        await ImportPresupuestosAsync(db);
        await ImportPresupuestoDetallesAsync(db);
        await ImportNotasCreditoAsync(db);
        await ImportNotaCreditoDetallesAsync(db);
        await ImportNotasDebitoAsync(db);
        await ImportNotaDebitoDetallesAsync(db);
        await ImportNotasDebitoInternasAsync(db);
        await ImportNotaDebitoInternaDetallesAsync(db);
        await ImportConsignacionesAsync(db);
        await ImportConsignacionDetallesAsync(db);
        await ImportPagosAsync(db);
        await ImportPagoDetallesAsync(db);
        await ImportCtaCteAsync(db);
        await ImportMovimientosCtaCteAsync(db);
    }

    #region Auxiliary Tables

    private static async Task ImportCondicionesIvaAsync(SPCDbContext db)
    {
        Console.Write("Importing CondicionesIva... ");
        var existingCount = await db.CondicionesIva.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "condicion_iva.csv");
        if (!File.Exists(csvPath))
        {
            // Use seed data
            var seeds = new List<TaxCondition>
            {
                new() { Id = 1, Codigo = "RI", Descripcion = "Responsable Inscripto", TipoInvoice = "A" },
                new() { Id = 2, Codigo = "MO", Descripcion = "Monotributo", TipoInvoice = "B" },
                new() { Id = 3, Codigo = "CF", Descripcion = "Consumidor Final", TipoInvoice = "B" },
                new() { Id = 4, Codigo = "EX", Descripcion = "Exento", TipoInvoice = "B" }
            };
            await BulkInsertWithIdentityAsync(db, seeds, "CondicionesIva");
            Console.WriteLine($"OK ({seeds.Count} seeded)");
            return;
        }

        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');
        var items = new List<TaxCondition>();
        int id = 1;

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            var codigo = SafeStr(row.GetValueOrDefault("IDCondicionIVA"), 10) ?? $"C{id}";
            var descripcion = SafeStr(row.GetValueOrDefault("Descripcion"), 100) ?? codigo;
            var tipoInvoice = codigo == "RI" ? "A" : "B";

            items.Add(new TaxCondition
            {
                Id = id++,
                Codigo = codigo,
                Descripcion = descripcion,
                TipoInvoice = tipoInvoice
            });
        }

        await BulkInsertWithIdentityAsync(db, items, "CondicionesIva");
        Console.WriteLine($"OK ({items.Count} records)");
    }

    private static async Task ImportUnidadesMedidaAsync(SPCDbContext db)
    {
        Console.Write("Importing UnidadesMedida... ");
        var existingCount = await db.UnidadesMedida.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "unidades_medida.csv");
        if (!File.Exists(csvPath))
        {
            var seeds = new List<UnitOfMeasure>
            {
                new() { Id = 1, Codigo = "UN", Nombre = "Unidades" },
                new() { Id = 2, Codigo = "CJ", Nombre = "Cajas" }
            };
            await BulkInsertWithIdentityAsync(db, seeds, "UnidadesMedida");
            Console.WriteLine($"OK ({seeds.Count} seeded)");
            return;
        }

        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');
        var items = new List<UnitOfMeasure>();
        int id = 1;

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            items.Add(new UnitOfMeasure
            {
                Id = id++,
                Codigo = SafeStr(row.GetValueOrDefault("IdUnitOfMeasure"), 10) ?? $"U{id}",
                Nombre = SafeStr(row.GetValueOrDefault("Descripcion"), 50) ?? "Sin nombre"
            });
        }

        await BulkInsertWithIdentityAsync(db, items, "UnidadesMedida");
        Console.WriteLine($"OK ({items.Count} records)");
    }

    private static async Task ImportCategorysAsync(SPCDbContext db)
    {
        Console.Write("Importing Categorys... ");
        var existingCount = await db.Categorys.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "rubros.csv");
        if (!File.Exists(csvPath))
        {
            var seeds = new List<Category>
            {
                new() { Id = 1, Nombre = "Nacionales", Activo = true }
            };
            await BulkInsertWithIdentityAsync(db, seeds, "Categorys");
            Console.WriteLine($"OK ({seeds.Count} seeded)");
            return;
        }

        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');
        var items = new List<Category>();
        int id = 1;

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            items.Add(new Category
            {
                Id = id++,
                Nombre = SafeStr(row.GetValueOrDefault("Descripcion"), 100) ?? "Sin nombre",
                Activo = true
            });
        }

        await BulkInsertWithIdentityAsync(db, items, "Categorys");
        Console.WriteLine($"OK ({items.Count} records)");
    }

    private static async Task ImportSucursalesAsync(SPCDbContext db)
    {
        Console.Write("Importing Sucursales (Branches)... ");
        var existingCount = await db.Branches.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "sucursales.csv");
        var items = new List<Branch>();
        
        // Always add a default branch with ID=1 for records without sucursal
        items.Add(new Branch
        {
            Id = 1,
            Code = "DEFAULT",
            Name = "Sin Sucursal",
            PointOfSale = 1,
            IsActive = true
        });
        
        if (!File.Exists(csvPath))
        {
            // Add default branches if no CSV
            items.Add(new Branch { Id = 2, Code = "CALLE", Name = "Calle (SalesRepes)", PointOfSale = 2, IsActive = true });
            items.Add(new Branch { Id = 5, Code = "OFICINA", Name = "Distribuidora (Oficina)", PointOfSale = 5, IsActive = true });
            await BulkInsertWithIdentityAsync(db, items, "Branches");
            Console.WriteLine($"OK ({items.Count} seeded)");
            return;
        }

        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            var idSucursal = SafeInt(row.GetValueOrDefault("IdSucursal"));
            if (idSucursal == 0 || idSucursal == 1) continue; // Skip 0 and 1 (we already added default)

            items.Add(new Branch
            {
                Id = idSucursal,
                Code = $"SUC{idSucursal}",
                Name = SafeStr(row.GetValueOrDefault("NombreSucursal"), 100) ?? $"Sucursal {idSucursal}",
                PointOfSale = idSucursal,
                IsActive = true
            });
        }

        if (items.Count > 0)
            await BulkInsertWithIdentityAsync(db, items, "Branches");
        Console.WriteLine($"OK ({items.Count} records)");
    }

    private static async Task ImportSalesRepesAsync(SPCDbContext db)
    {
        Console.Write("Importing SalesRepes (Empleados)... ");
        var existingCount = await db.SalesRepes.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "empleados.csv");
        if (!File.Exists(csvPath))
        {
            Console.WriteLine("SKIP (no CSV file)");
            return;
        }

        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');
        var items = new List<SalesRep>();
        int id = 1;

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            var legajo = SafeStr(row.GetValueOrDefault("Legajo"), 20);
            if (string.IsNullOrEmpty(legajo)) continue;

            items.Add(new SalesRep
            {
                Id = id++,
                Legajo = legajo,
                Nombre = SafeStr(row.GetValueOrDefault("Nombre"), 100) ?? "Sin Nombre",
                CUIL = SafeStr(row.GetValueOrDefault("CUIL"), 13),
                Domicilio = SafeStr(row.GetValueOrDefault("Domicilio"), 200),
                Localidad = SafeStr(row.GetValueOrDefault("Localidad"), 100),
                Provincia = SafeStr(row.GetValueOrDefault("Prov"), 100),
                CodigoPostal = SafeStr(row.GetValueOrDefault("CP"), 10),
                DNI = SafeStr(row.GetValueOrDefault("DNI"), 10),
                Telefono = SafeStr(row.GetValueOrDefault("Tel"), 50),
                Celular = SafeStr(row.GetValueOrDefault("Cel"), 50),
                Email = SafeStr(row.GetValueOrDefault("emaill"), 200),
                FechaNacimiento = SafeDate(row.GetValueOrDefault("FechaNacimiento")),
                FechaIngreso = SafeDate(row.GetValueOrDefault("FechaIngreso")),
                PorcentajeComision = SafeDecimal(row.GetValueOrDefault("Comision")),
                Observaciones = SafeStr(row.GetValueOrDefault("Observaciones"), 500),
                Activo = true
            });
        }

        if (items.Count > 0)
            await BulkInsertWithIdentityAsync(db, items, "SalesRepes");
        Console.WriteLine($"OK ({items.Count} records)");
    }

    private static async Task ImportWarehousesAsync(SPCDbContext db)
    {
        Console.Write("Importing Warehouses... ");
        var existingCount = await db.Warehouses.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "depositos.csv");
        if (!File.Exists(csvPath))
        {
            var seeds = new List<Warehouse>
            {
                new() { Id = 1, Nombre = "Warehouse Principal", Activo = true }
            };
            await BulkInsertWithIdentityAsync(db, seeds, "Warehouses");
            Console.WriteLine($"OK ({seeds.Count} seeded)");
            return;
        }

        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');
        var items = new List<Warehouse>();
        int id = 1;

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            items.Add(new Warehouse
            {
                Id = id++,
                Nombre = SafeStr(row.GetValueOrDefault("Descripcion"), 100) ?? "Sin nombre",
                Activo = true
            });
        }

        if (items.Count > 0)
            await BulkInsertWithIdentityAsync(db, items, "Warehouses");
        Console.WriteLine($"OK ({items.Count} records)");
    }

    #endregion

    #region Master Data (with preserved IDs)

    private static async Task ImportCustomersAsync(SPCDbContext db)
    {
        Console.Write("Importing Customers... ");
        var existingCount = await db.Customers.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "clientes.csv");
        if (!File.Exists(csvPath))
        {
            Console.WriteLine("ERROR: clientes.csv not found!");
            return;
        }

        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');
        var items = new List<Customer>();

        // Load lookups
        var condicionesIva = await db.CondicionesIva.ToDictionaryAsync(c => c.Codigo, c => c.Id);
        var vendedores = await db.SalesRepes.ToDictionaryAsync(v => v.Legajo, v => v.Id);

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            var idCustomer = SafeInt(row.GetValueOrDefault("IDCliente"));
            if (idCustomer == 0) continue;

            var condIva = SafeStr(row.GetValueOrDefault("TaxCondition"), 10);
            var vendedor = SafeStr(row.GetValueOrDefault("Vendedor"), 20);

            var porcentajeDesc = SafeDecimal(row.GetValueOrDefault("PorcentajeDescuento"));
            if (porcentajeDesc > 100) porcentajeDesc = 100;
            if (porcentajeDesc < 0) porcentajeDesc = 0;

            var limiteCredito = SafeDecimal(row.GetValueOrDefault("LimiteCredito"));
            if (limiteCredito > 999999999999m) limiteCredito = 999999999999m;
            if (limiteCredito < 0) limiteCredito = 0;

            items.Add(new Customer
            {
                Id = idCustomer,  // PRESERVE ORIGINAL ID!
                RazonSocial = SafeStr(row.GetValueOrDefault("RazonSocial"), 200) ?? "Sin Nombre",
                NombreFantasia = SafeStr(row.GetValueOrDefault("NombreFantasia"), 200),
                CUIT = SafeStr(row.GetValueOrDefault("CUIT"), 13),
                Direccion = SafeStr(row.GetValueOrDefault("Domicilio"), 300),
                Localidad = SafeStr(row.GetValueOrDefault("Localidad"), 100),
                Provincia = SafeStr(row.GetValueOrDefault("Prov"), 100),
                CodigoPostal = SafeStr(row.GetValueOrDefault("CP"), 10),
                Telefono = SafeStr(row.GetValueOrDefault("Tel"), 50),
                Celular = SafeStr(row.GetValueOrDefault("Cel"), 50),
                Email = SafeStr(row.GetValueOrDefault("email"), 200),
                TaxConditionId = !string.IsNullOrEmpty(condIva) && condicionesIva.ContainsKey(condIva) 
                    ? condicionesIva[condIva] : null,
                SalesRepId = !string.IsNullOrEmpty(vendedor) && vendedores.ContainsKey(vendedor) 
                    ? vendedores[vendedor] : null,
                PorcentajeDescuento = porcentajeDesc,
                LimiteCredito = limiteCredito,
                Observaciones = SafeStr(row.GetValueOrDefault("Observaciones"), 500),
                Activo = true,
                FechaAlta = DateTime.Now
            });
        }

        Console.Write($"({items.Count} records)... ");
        
        // Bulk insert with IDENTITY_INSERT ON to preserve original IDs
        for (int i = 0; i < items.Count; i += BatchSize)
        {
            var batch = items.Skip(i).Take(BatchSize).ToList();
            await BulkInsertWithIdentityAsync(db, batch, "Customers");
            Console.Write(".");
        }

        Console.WriteLine(" OK");
    }

    private static async Task ImportProductsAsync(SPCDbContext db)
    {
        Console.Write("Importing Products... ");
        var existingCount = await db.Products.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "productos.csv");
        if (!File.Exists(csvPath))
        {
            Console.WriteLine("ERROR: productos.csv not found!");
            return;
        }

        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');
        var items = new List<Product>();

        // Load lookups
        var rubros = await db.Categorys.ToDictionaryAsync(r => r.Nombre, r => r.Id);
        var unidades = await db.UnidadesMedida.ToDictionaryAsync(u => u.Codigo, u => u.Id);

        int id = 1;
        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            var codigo = SafeStr(row.GetValueOrDefault("CodProd"), 50);
            if (string.IsNullOrEmpty(codigo)) continue;

            var rubro = SafeStr(row.GetValueOrDefault("Category"), 100);
            var unidad = SafeStr(row.GetValueOrDefault("UnitOfMeasure"), 10);

            items.Add(new Product
            {
                Id = id++,
                Codigo = codigo,
                Descripcion = SafeStr(row.GetValueOrDefault("Descripcion"), 300) ?? codigo,
                PrecioVenta = SafeDecimal(row.GetValueOrDefault("PrecioUnitarioFactura")),
                PrecioCosto = SafeDecimal(row.GetValueOrDefault("PrecioUnitarioPresupuesto")),
                CategoryId = !string.IsNullOrEmpty(rubro) && rubros.ContainsKey(rubro) ? rubros[rubro] : null,
                UnitOfMeasureId = !string.IsNullOrEmpty(unidad) && unidades.ContainsKey(unidad) ? unidades[unidad] : null,
                StockMinimo = SafeInt(row.GetValueOrDefault("PuntoPedido")),
                Observaciones = SafeStr(row.GetValueOrDefault("Observaciones"), 500),
                PorcentajeIVA = 21m,
                Activo = true
            });
        }

        Console.Write($"({items.Count} records)... ");
        await BulkInsertWithIdentityAsync(db, items, "Products");
        Console.WriteLine("OK");
    }

    private static async Task ImportStockAsync(SPCDbContext db)
    {
        Console.Write("Importing Stock... ");
        var existingCount = await db.Stocks.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "stock.csv");
        if (!File.Exists(csvPath))
        {
            Console.WriteLine("SKIP (no CSV file)");
            return;
        }

        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');
        var items = new List<Stock>();

        // Load lookups
        var productos = await db.Products.ToDictionaryAsync(p => p.Codigo, p => p.Id);
        var depositos = await db.Warehouses.ToDictionaryAsync(d => d.Nombre, d => d.Id);

        int id = 1;
        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            var codProd = SafeStr(row.GetValueOrDefault("CodProd"), 50);
            var idWarehouse = SafeStr(row.GetValueOrDefault("IdDeposito"), 100);

            if (string.IsNullOrEmpty(codProd) || !productos.ContainsKey(codProd)) continue;
            
            // Try to find deposito by name or use default
            var depositoId = 1;
            if (!string.IsNullOrEmpty(idWarehouse) && depositos.ContainsKey(idWarehouse))
            {
                depositoId = depositos[idWarehouse];
            }

            items.Add(new Stock
            {
                Id = id++,
                ProductId = productos[codProd],
                WarehouseId = depositoId,
                Cantidad = SafeDecimal(row.GetValueOrDefault("Cantidad"))
            });
        }

        Console.Write($"({items.Count} records)... ");
        if (items.Count > 0)
            await BulkInsertWithIdentityAsync(db, items, "Stocks");
        Console.WriteLine("OK");
    }

    #endregion

    private static async Task LoadMappingsAsync(SPCDbContext db)
    {
        // Load Customers - IDs are now preserved, so map is 1:1
        var clientes = await db.Customers.Select(c => c.Id).ToListAsync();
        foreach (var id in clientes)
        {
            CustomerMap[id] = id;  // ID = ID (preserved from Access)
        }
        Console.WriteLine($"  Customers: {CustomerMap.Count}");

        // SalesRepes
        var vendedores = await db.SalesRepes.ToListAsync();
        foreach (var v in vendedores)
        {
            SalesRepMap[v.Legajo] = v.Id;
        }
        Console.WriteLine($"  SalesRepes: {SalesRepMap.Count}");

        // Products
        var productos = await db.Products.ToListAsync();
        foreach (var p in productos)
        {
            ProductMap[p.Codigo] = p.Id;
        }
        Console.WriteLine($"  Products: {ProductMap.Count}");

        // Branches
        var branches = await db.Branches.ToListAsync();
        foreach (var b in branches)
        {
            BranchMap[b.PointOfSale] = b.Id;
        }
        Console.WriteLine($"  Branches: {BranchMap.Count}");

        // Payment Methods
        var paymentMethods = await db.PaymentMethods.ToListAsync();
        foreach (var pm in paymentMethods)
        {
            PaymentMethodMap[pm.Code.ToUpper()] = pm.Id;
        }
        Console.WriteLine($"  PaymentMethods: {PaymentMethodMap.Count}");
    }

    private static async Task ImportInvoicesAsync(SPCDbContext db)
    {
        Console.Write("Importing Invoices... ");
        
        var existingCount = await db.Invoices.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            var existing = await db.Invoices.ToListAsync();
            foreach (var f in existing)
            {
                InvoiceMap[(f.TipoInvoice, (int)f.NumeroInvoice)] = f.Id;
            }
            return;
        }

        var csvPath = Path.Combine(DataDir, "facturas_c.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');
        
        var facturas = new List<Invoice>();
        var keys = new List<(string tipo, int nro)>();

        for (int i = 1; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var cols = ParseCsvLine(lines[i]);
            var row = CreateDictionary(header, cols);
            
            var codCustomer = SafeInt(row.GetValueOrDefault("CodCliente"));
            if (!CustomerMap.ContainsKey(codCustomer)) continue;

            var tipoRaw = row.GetValueOrDefault("TipoFactura");
            var nroRaw = row.GetValueOrDefault("NroFactura");
            if (string.IsNullOrWhiteSpace(tipoRaw) || string.IsNullOrWhiteSpace(nroRaw))
            {
                LogSkip("facturas_c.csv", lineNumber, "Missing TipoInvoice or NroInvoice", row);
                continue;
            }

            var tipo = SafeStr(tipoRaw, 1)!;
            var nro = SafeInt(nroRaw);
            var idSucursal = SafeInt(row.GetValueOrDefault("IdSucursal"));
            var branchId = BranchMap.GetValueOrDefault(idSucursal, 1);
            var puntoVenta = idSucursal > 0 ? idSucursal : 2;
            var vendedorLegajo = SafeStr(row.GetValueOrDefault("CodVendedor"), 20);

            var factura = new Invoice
            {
                BranchId = branchId,
                TipoInvoice = tipo,
                PuntoVenta = puntoVenta,
                NumeroInvoice = nro,
                FechaInvoice = SafeDate(row.GetValueOrDefault("FechaFactura")) ?? DateTime.Now,
                CustomerId = CustomerMap[codCustomer],
                SalesRepId = !string.IsNullOrEmpty(vendedorLegajo) && SalesRepMap.ContainsKey(vendedorLegajo) 
                    ? SalesRepMap[vendedorLegajo] : null,
                Subtotal = SafeDecimal(row.GetValueOrDefault("SubTotalFactura")),
                PorcentajeIVA = SafeDecimal(row.GetValueOrDefault("PorcentajeIVA")),
                ImporteIVA = SafeDecimal(row.GetValueOrDefault("TotalIVA")),
                AlicuotaIIBB = SafeDecimal(row.GetValueOrDefault("AlicuotaIIBB")),
                ImportePercepcionIIBB = SafeDecimal(row.GetValueOrDefault("ImportePercepIIBB")),
                PorcentajeDescuento = SafeDecimal(row.GetValueOrDefault("PorcentajeDesc")),
                ImporteDescuento = SafeDecimal(row.GetValueOrDefault("ImporteDesc")),
                Total = SafeDecimal(row.GetValueOrDefault("TotalFactura")),
                CAE = SafeStr(row.GetValueOrDefault("Cae"), 20),
                FechaVencimientoCAE = SafeDate(row.GetValueOrDefault("FechaVC")),
                CondicionVenta = SafeStr(row.GetValueOrDefault("CondicionVenta"), 50),
                FormaPago = SafeInt(row.GetValueOrDefault("FormaPago")),
                UnidadNegocio = SafeStr(row.GetValueOrDefault("UnidadNegocio"), 50),
                Anulada = row.GetValueOrDefault("Cancelada")?.ToUpper() == "S",
                Aclaracion = SafeStr(row.GetValueOrDefault("AclaracionFactura"), 500)
            };

            facturas.Add(factura);
            keys.Add((tipo, nro));
        }

        Console.Write($"({facturas.Count} records)... ");

        // Bulk insert in batches
        for (int i = 0; i < facturas.Count; i += BatchSize)
        {
            var batch = facturas.Skip(i).Take(BatchSize).ToList();
            await db.BulkInsertAsync(batch, new BulkConfig { SetOutputIdentity = true, BatchSize = BatchSize });
            
            for (int j = 0; j < batch.Count; j++)
            {
                InvoiceMap[keys[i + j]] = batch[j].Id;
            }
            Console.Write(".");
        }

        Console.WriteLine(" OK");
    }

    private static async Task ImportInvoiceDetailsAsync(SPCDbContext db)
    {
        Console.Write("Importing InvoiceDetails... ");

        var existingCount = await db.InvoiceDetails.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "facturas_d.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        var detalles = new List<InvoiceDetail>();

        for (int i = 1; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var cols = ParseCsvLine(lines[i]);
            var row = CreateDictionary(header, cols);

            var tipoRaw = row.GetValueOrDefault("TipoFactura");
            var nroRaw = row.GetValueOrDefault("NroFactura");
            if (string.IsNullOrWhiteSpace(tipoRaw) || string.IsNullOrWhiteSpace(nroRaw))
            {
                LogSkip("facturas_d.csv", lineNumber, "Missing TipoInvoice or NroInvoice", row);
                continue;
            }

            var tipo = SafeStr(tipoRaw, 1)!;
            var nro = SafeInt(nroRaw);
            var key = (tipo, nro);

            if (!InvoiceMap.ContainsKey(key)) continue;

            var codProd = SafeStr(row.GetValueOrDefault("IDCodProd"), 50);
            if (string.IsNullOrEmpty(codProd) || !ProductMap.ContainsKey(codProd)) continue;

            detalles.Add(new InvoiceDetail
            {
                InvoiceId = InvoiceMap[key],
                ItemNumero = SafeInt(row.GetValueOrDefault("ItemFactura")),
                ProductId = ProductMap[codProd],
                Cantidad = SafeDecimal(row.GetValueOrDefault("Cantidad")),
                PrecioUnitario = SafeDecimal(row.GetValueOrDefault("PrecioUnitario")),
                PorcentajeDescuento = SafeDecimal(row.GetValueOrDefault("PorcentajeDescuento")),
                PorcentajeIVA = 21m,
                Subtotal = SafeDecimal(row.GetValueOrDefault("TotalLinea"))
            });
        }

        Console.Write($"({detalles.Count} records)... ");

        for (int i = 0; i < detalles.Count; i += BatchSize)
        {
            var batch = detalles.Skip(i).Take(BatchSize).ToList();
            await db.BulkInsertAsync(batch, new BulkConfig { BatchSize = BatchSize });
            Console.Write(".");
        }

        Console.WriteLine(" OK");
    }

    // Stub methods for remaining imports - similar pattern
    private static async Task ImportDeliveryNotesAsync(SPCDbContext db) 
    {
        Console.Write("Importing DeliveryNotes... ");
        var existingCount = await db.DeliveryNotes.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            var existing = await db.DeliveryNotes.ToListAsync();
            foreach (var r in existing)
            {
                DeliveryNoteMap[(r.PuntoVenta, (int)r.NumeroDeliveryNote)] = r.Id;
            }
            return;
        }
        var csvPath = Path.Combine(DataDir, "remitos_c.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        var remitos = new List<DeliveryNote>();
        var keys = new List<(int sucursal, int nro)>();

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            var idSucursal = SafeInt(row.GetValueOrDefault("IdSucursal"));
            var nroDeliveryNote = SafeInt(row.GetValueOrDefault("NroRemito"));
            var codCustomer = SafeInt(row.GetValueOrDefault("CodCliente"));
            if (!CustomerMap.ContainsKey(codCustomer)) continue;

            var vendedorLegajo = SafeStr(row.GetValueOrDefault("CodVendedor"), 20);
            var nroInvoice = SafeInt(row.GetValueOrDefault("NroFactura"));
            var tipoInvoice = SafeStr(row.GetValueOrDefault("TipoFactura"), 2);

            var branchId = BranchMap.GetValueOrDefault(idSucursal, 1);
            var puntoVenta = idSucursal > 0 ? idSucursal : 2;

            int? facturaId = null;
            if (nroInvoice > 0 && !string.IsNullOrEmpty(tipoInvoice) && InvoiceMap.ContainsKey((tipoInvoice, nroInvoice)))
            {
                facturaId = InvoiceMap[(tipoInvoice, nroInvoice)];
            }

            var remito = new DeliveryNote
            {
                BranchId = branchId,
                PuntoVenta = puntoVenta,
                NumeroDeliveryNote = nroDeliveryNote,
                FechaDeliveryNote = SafeDate(row.GetValueOrDefault("FechaRemito")) ?? DateTime.Now,
                CustomerId = CustomerMap[codCustomer],
                SalesRepId = !string.IsNullOrEmpty(vendedorLegajo) && SalesRepMap.ContainsKey(vendedorLegajo)
                    ? SalesRepMap[vendedorLegajo]
                    : null,
                UnidadNegocio = SafeStr(row.GetValueOrDefault("UnidadNegocio"), 50),
                InvoiceId = facturaId,
                TipoFactura = tipoInvoice,
                Facturado = facturaId.HasValue,
                Aclaracion = SafeStr(row.GetValueOrDefault("AclaracionRemito"), 500),
                Anulado = false
            };

            remitos.Add(remito);
            keys.Add((idSucursal, nroDeliveryNote));
        }

        Console.Write($"({remitos.Count} records)... ");

        for (int i = 0; i < remitos.Count; i += BatchSize)
        {
            var batch = remitos.Skip(i).Take(BatchSize).ToList();
            await db.BulkInsertAsync(batch, new BulkConfig { SetOutputIdentity = true, BatchSize = BatchSize });

            for (int j = 0; j < batch.Count; j++)
            {
                DeliveryNoteMap[keys[i + j]] = batch[j].Id;
            }
            Console.Write(".");
        }

        Console.WriteLine(" OK");
    }

    private static async Task ImportDeliveryNoteDetailsAsync(SPCDbContext db) 
    {
        Console.Write("Importing DeliveryNoteDetails... ");
        var existingCount = await db.DeliveryNoteDetails.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }
        var csvPath = Path.Combine(DataDir, "remitos_d.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        var detalles = new List<DeliveryNoteDetail>();

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            var idSucursal = SafeInt(row.GetValueOrDefault("IdSucursal"));
            var nroDeliveryNote = SafeInt(row.GetValueOrDefault("NroRemito"));
            var key = (idSucursal, nroDeliveryNote);

            if (!DeliveryNoteMap.ContainsKey(key)) continue;

            var codProd = SafeStr(row.GetValueOrDefault("IDCodProd"), 50);
            if (string.IsNullOrEmpty(codProd) || !ProductMap.ContainsKey(codProd)) continue;

            detalles.Add(new DeliveryNoteDetail
            {
                DeliveryNoteId = DeliveryNoteMap[key],
                ItemNumero = SafeInt(row.GetValueOrDefault("ItemRemito")),
                ProductId = ProductMap[codProd],
                Cantidad = SafeDecimal(row.GetValueOrDefault("Cantidad"))
            });
        }

        Console.Write($"({detalles.Count} records)... ");

        for (int i = 0; i < detalles.Count; i += BatchSize)
        {
            var batch = detalles.Skip(i).Take(BatchSize).ToList();
            await db.BulkInsertAsync(batch, new BulkConfig { BatchSize = BatchSize });
            Console.Write(".");
        }

        Console.WriteLine(" OK");
    }

    private static async Task ImportPresupuestosAsync(SPCDbContext db)
    {
        Console.Write("Importing Presupuestos... ");
        var existingCount = await db.Quotes.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            var existing = await db.Quotes.ToListAsync();
            foreach (var q in existing)
            {
                QuoteMap[(int)q.QuoteNumber] = q.Id;
            }
            return;
        }
        var csvPath = Path.Combine(DataDir, "presupuestos_c.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        var quotes = new List<Quote>();
        var keys = new List<int>();

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            var nroPresu = SafeInt(row.GetValueOrDefault("NroPresu"));
            var codCustomer = SafeInt(row.GetValueOrDefault("CodCliente"));
            if (!CustomerMap.ContainsKey(codCustomer)) continue;

            var vendedorLegajo = SafeStr(row.GetValueOrDefault("CodVendedor"), 20);

            quotes.Add(new Quote
            {
                BranchId = 1,
                QuoteNumber = nroPresu,
                QuoteDate = SafeDate(row.GetValueOrDefault("FechaPresu")) ?? DateTime.Now,
                CustomerId = CustomerMap[codCustomer],
                SalesRepId = !string.IsNullOrEmpty(vendedorLegajo) && SalesRepMap.ContainsKey(vendedorLegajo)
                    ? SalesRepMap[vendedorLegajo]
                    : null,
                Subtotal = SafeDecimal(row.GetValueOrDefault("SubTotalPresu")),
                DiscountPercent = SafeDecimal(row.GetValueOrDefault("PorcentajeDesc")),
                DiscountAmount = SafeDecimal(row.GetValueOrDefault("ImporteDesc")),
                Total = SafeDecimal(row.GetValueOrDefault("TotalPresu")),
                BusinessUnit = SafeStr(row.GetValueOrDefault("UnidadNegocio"), 50),
                IsVoided = row.GetValueOrDefault("Anulado")?.ToUpper() == "S"
            });

            keys.Add(nroPresu);
        }

        Console.Write($"({quotes.Count} records)... ");

        for (int i = 0; i < quotes.Count; i += BatchSize)
        {
            var batch = quotes.Skip(i).Take(BatchSize).ToList();
            await db.BulkInsertAsync(batch, new BulkConfig { SetOutputIdentity = true, BatchSize = BatchSize });

            for (int j = 0; j < batch.Count; j++)
            {
                QuoteMap[keys[i + j]] = batch[j].Id;
            }
            Console.Write(".");
        }

        Console.WriteLine(" OK");
    }

    private static async Task ImportPresupuestoDetallesAsync(SPCDbContext db)
    {
        Console.Write("Importing PresupuestoDetalles... ");
        var existingCount = await db.QuoteDetails.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }
        var csvPath = Path.Combine(DataDir, "presupuestos_d.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        var detalles = new List<QuoteDetail>();

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            var nroPresu = SafeInt(row.GetValueOrDefault("NroPresu"));
            if (!QuoteMap.ContainsKey(nroPresu)) continue;

            var codProd = SafeStr(row.GetValueOrDefault("CodProd"), 50);
            if (string.IsNullOrEmpty(codProd) || !ProductMap.ContainsKey(codProd)) continue;

            detalles.Add(new QuoteDetail
            {
                QuoteId = QuoteMap[nroPresu],
                ItemNumber = SafeInt(row.GetValueOrDefault("ItemPresu")),
                ProductId = ProductMap[codProd],
                Quantity = SafeDecimal(row.GetValueOrDefault("Cantidad")),
                UnitPrice = SafeDecimal(row.GetValueOrDefault("PrecioUnitario")),
                DiscountPercent = SafeDecimal(row.GetValueOrDefault("PorcentajeDescuento")),
                DiscountAmount = SafeDecimal(row.GetValueOrDefault("ImporteDescuento")),
                Subtotal = SafeDecimal(row.GetValueOrDefault("TotalLinea"))
            });
        }

        Console.Write($"({detalles.Count} records)... ");

        for (int i = 0; i < detalles.Count; i += BatchSize)
        {
            var batch = detalles.Skip(i).Take(BatchSize).ToList();
            await db.BulkInsertAsync(batch, new BulkConfig { BatchSize = BatchSize });
            Console.Write(".");
        }

        Console.WriteLine(" OK");
    }

    private static async Task ImportNotasCreditoAsync(SPCDbContext db)
    {
        Console.Write("Importing NotasCredito... ");
        var existingCount = await db.CreditNotes.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            var existing = await db.CreditNotes.ToListAsync();
            foreach (var n in existing)
            {
                var tipo = n.VoucherType == VoucherType.CreditNoteA ? "A" : "B";
                CreditNoteMap[(tipo, (int)n.CreditNoteNumber)] = n.Id;
            }
            return;
        }

        var csvPath = Path.Combine(DataDir, "notas_credito_c.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        var notes = new List<CreditNote>();
        var keys = new List<(string tipo, int nro)>();
        var seen = new HashSet<(string tipo, int nro)>();
        var skippedEmptyType = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var cols = ParseCsvLine(lines[i]);
            var row = CreateDictionary(header, cols);

            var codCustomer = SafeInt(row.GetValueOrDefault("CodCliente"));
            if (!CustomerMap.ContainsKey(codCustomer)) continue;

            var tipoRaw = row.GetValueOrDefault("TipoNotaCredito");
            var nroRaw = row.GetValueOrDefault("NroNotaCredito");
            if (string.IsNullOrWhiteSpace(tipoRaw) || string.IsNullOrWhiteSpace(nroRaw))
            {
                skippedEmptyType++;
                LogSkip("notas_credito_c.csv", lineNumber, "Missing TipoNotaCredito or NroNotaCredito", row);
                continue;
            }

            var tipo = SafeStr(tipoRaw, 1)!;
            var nro = SafeInt(nroRaw);
            var key = (tipo, nro);
            if (!seen.Add(key)) continue;
            var vendedorLegajo = SafeStr(row.GetValueOrDefault("CodVendedor"), 20);

            var cancelada = row.GetValueOrDefault("Cancelada");
            var isVoided = cancelada?.ToUpper() == "S" || cancelada?.ToUpper() == "TRUE" || cancelada == "1";

            var voucherType = tipo == "A" ? VoucherType.CreditNoteA : VoucherType.CreditNoteB;

            var note = new CreditNote
            {
                VoucherType = voucherType,
                BranchId = 1,
                PointOfSale = 2,
                CreditNoteNumber = nro,
                CreditNoteDate = SafeDate(row.GetValueOrDefault("FechaNotaCredito")) ?? DateTime.Now,
                CustomerId = CustomerMap[codCustomer],
                SalesRepId = !string.IsNullOrEmpty(vendedorLegajo) && SalesRepMap.ContainsKey(vendedorLegajo)
                    ? SalesRepMap[vendedorLegajo]
                    : null,
                Subtotal = SafeDecimal(row.GetValueOrDefault("SubTotalNotaCredito")),
                VATPercent = SafeDecimal(row.GetValueOrDefault("PorcentajeIVA")),
                VATAmount = SafeDecimal(row.GetValueOrDefault("TotalIVA")),
                IIBBPercent = SafeDecimal(row.GetValueOrDefault("AlicuotaIIBB")),
                IIBBAmount = SafeDecimal(row.GetValueOrDefault("ImportePercepIIBB")),
                DiscountPercent = SafeDecimal(row.GetValueOrDefault("PorcentajeDesc")),
                DiscountAmount = SafeDecimal(row.GetValueOrDefault("ImporteDesc")),
                Total = SafeDecimal(row.GetValueOrDefault("TotalNotaCredito")),
                CAE = SafeStr(row.GetValueOrDefault("CAE"), 20),
                CAEExpirationDate = SafeDate(row.GetValueOrDefault("FechaVC")),
                SalesCondition = SafeStr(row.GetValueOrDefault("CondicionVenta"), 50),
                IsVoided = isVoided
            };

            notes.Add(note);
            keys.Add(key);
        }

        Console.Write($"({notes.Count} records)... ");

        for (int i = 0; i < notes.Count; i += BatchSize)
        {
            var batch = notes.Skip(i).Take(BatchSize).ToList();
            await db.BulkInsertAsync(batch, new BulkConfig { SetOutputIdentity = true, BatchSize = BatchSize });

            for (int j = 0; j < batch.Count; j++)
            {
                CreditNoteMap[keys[i + j]] = batch[j].Id;
            }
            Console.Write(".");
        }

        Console.WriteLine(skippedEmptyType > 0
            ? $" OK (skipped {skippedEmptyType} empty tipo/nro)"
            : " OK");
    }

    private static async Task ImportNotaCreditoDetallesAsync(SPCDbContext db)
    {
        Console.Write("Importing NotaCreditoDetalles... ");
        var existingCount = await db.CreditNoteDetails.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "notas_credito_d.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        var detalles = new List<CreditNoteDetail>();
        var skippedEmptyType = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var cols = ParseCsvLine(lines[i]);
            var row = CreateDictionary(header, cols);

            var tipoRaw = row.GetValueOrDefault("TipoNotaCredito");
            var nroRaw = row.GetValueOrDefault("NroNotaCredito");
            if (string.IsNullOrWhiteSpace(tipoRaw) || string.IsNullOrWhiteSpace(nroRaw))
            {
                skippedEmptyType++;
                LogSkip("notas_credito_d.csv", lineNumber, "Missing TipoNotaCredito or NroNotaCredito", row);
                continue;
            }

            var tipo = SafeStr(tipoRaw, 1)!;
            var nro = SafeInt(nroRaw);
            var key = (tipo, nro);

            if (!CreditNoteMap.ContainsKey(key)) continue;

            var codProd = SafeStr(row.GetValueOrDefault("IDCodProd"), 50);
            if (string.IsNullOrEmpty(codProd) || !ProductMap.ContainsKey(codProd)) continue;

            detalles.Add(new CreditNoteDetail
            {
                CreditNoteId = CreditNoteMap[key],
                ItemNumber = SafeInt(row.GetValueOrDefault("ItemNotaCredito")),
                ProductId = ProductMap[codProd],
                Quantity = SafeDecimal(row.GetValueOrDefault("Cantidad")),
                UnitPrice = SafeDecimal(row.GetValueOrDefault("PrecioUnitario")),
                DiscountPercent = SafeDecimal(row.GetValueOrDefault("PorcentajeDescuento")),
                DiscountAmount = SafeDecimal(row.GetValueOrDefault("ImporteDescuento")),
                Subtotal = SafeDecimal(row.GetValueOrDefault("TotalLinea")),
                UnitOfMeasure = SafeStr(row.GetValueOrDefault("UnitOfMeasure"), 20)
            });
        }

        Console.Write($"({detalles.Count} records)... ");

        for (int i = 0; i < detalles.Count; i += BatchSize)
        {
            var batch = detalles.Skip(i).Take(BatchSize).ToList();
            await db.BulkInsertAsync(batch, new BulkConfig { BatchSize = BatchSize });
            Console.Write(".");
        }

        Console.WriteLine(skippedEmptyType > 0
            ? $" OK (skipped {skippedEmptyType} empty tipo/nro)"
            : " OK");
    }

    private static async Task ImportNotasDebitoAsync(SPCDbContext db)
    {
        Console.Write("Importing NotasDebito... ");
        var existingCount = await db.DebitNotes.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            var existing = await db.DebitNotes.ToListAsync();
            foreach (var n in existing)
            {
                var tipo = n.VoucherType == VoucherType.DebitNoteA ? "A" : "B";
                DebitNoteMap[(tipo, (int)n.DebitNoteNumber)] = n.Id;
            }
            return;
        }

        var csvPath = Path.Combine(DataDir, "notas_debito_c.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        var notes = new List<DebitNote>();
        var keys = new List<(string tipo, int nro)>();

        for (int i = 1; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var cols = ParseCsvLine(lines[i]);
            var row = CreateDictionary(header, cols);

            var codCustomer = SafeInt(row.GetValueOrDefault("CodCliente"));
            if (!CustomerMap.ContainsKey(codCustomer)) continue;

            var tipoRaw = row.GetValueOrDefault("TipoDebito");
            var nroRaw = row.GetValueOrDefault("NroDebito");
            if (string.IsNullOrWhiteSpace(tipoRaw) || string.IsNullOrWhiteSpace(nroRaw))
            {
                LogSkip("notas_debito_c.csv", lineNumber, "Missing TipoDebito or NroDebito", row);
                continue;
            }

            var tipo = SafeStr(tipoRaw, 1)!;
            var nro = SafeInt(nroRaw);
            var vendedorLegajo = SafeStr(row.GetValueOrDefault("CodVendedor"), 20);

            var cancelada = row.GetValueOrDefault("Cancelada");
            var isVoided = cancelada?.ToUpper() == "S" || cancelada?.ToUpper() == "TRUE" || cancelada == "1";

            var voucherType = tipo == "A" ? VoucherType.DebitNoteA : VoucherType.DebitNoteB;

            var note = new DebitNote
            {
                VoucherType = voucherType,
                BranchId = 1,
                PointOfSale = 2,
                DebitNoteNumber = nro,
                DebitNoteDate = SafeDate(row.GetValueOrDefault("FechaDebito")) ?? DateTime.Now,
                CustomerId = CustomerMap[codCustomer],
                SalesRepId = !string.IsNullOrEmpty(vendedorLegajo) && SalesRepMap.ContainsKey(vendedorLegajo)
                    ? SalesRepMap[vendedorLegajo]
                    : null,
                Subtotal = SafeDecimal(row.GetValueOrDefault("SubTotalDebito")),
                VATPercent = SafeDecimal(row.GetValueOrDefault("PorcentajeIVA")),
                VATAmount = SafeDecimal(row.GetValueOrDefault("TotalIVA")),
                IIBBPercent = SafeDecimal(row.GetValueOrDefault("AlicuotaIIBB")),
                IIBBAmount = SafeDecimal(row.GetValueOrDefault("ImportePercepIIBB")),
                DiscountPercent = SafeDecimal(row.GetValueOrDefault("PorcentajeDesc")),
                DiscountAmount = SafeDecimal(row.GetValueOrDefault("ImporteDesc")),
                Total = SafeDecimal(row.GetValueOrDefault("TotalDebito")),
                CAE = SafeStr(row.GetValueOrDefault("Cae"), 20),
                CAEExpirationDate = SafeDate(row.GetValueOrDefault("FechaVC")),
                SalesCondition = SafeStr(row.GetValueOrDefault("CondicionVenta"), 50),
                IsVoided = isVoided
            };

            notes.Add(note);
            keys.Add((tipo, nro));
        }

        Console.Write($"({notes.Count} records)... ");

        for (int i = 0; i < notes.Count; i += BatchSize)
        {
            var batch = notes.Skip(i).Take(BatchSize).ToList();
            await db.BulkInsertAsync(batch, new BulkConfig { SetOutputIdentity = true, BatchSize = BatchSize });

            for (int j = 0; j < batch.Count; j++)
            {
                DebitNoteMap[keys[i + j]] = batch[j].Id;
            }
            Console.Write(".");
        }

        Console.WriteLine(" OK");
    }

    private static async Task ImportNotaDebitoDetallesAsync(SPCDbContext db)
    {
        Console.Write("Importing NotaDebitoDetalles... ");
        var existingCount = await db.DebitNoteDetails.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "notas_debito_d.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        var detalles = new List<DebitNoteDetail>();

        for (int i = 1; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var cols = ParseCsvLine(lines[i]);
            var row = CreateDictionary(header, cols);

            var tipoRaw = row.GetValueOrDefault("TipoDebito");
            var nroRaw = row.GetValueOrDefault("NroDebito");
            if (string.IsNullOrWhiteSpace(tipoRaw) || string.IsNullOrWhiteSpace(nroRaw))
            {
                LogSkip("notas_debito_d.csv", lineNumber, "Missing TipoDebito or NroDebito", row);
                continue;
            }

            var tipo = SafeStr(tipoRaw, 1)!;
            var nro = SafeInt(nroRaw);
            var key = (tipo, nro);

            if (!DebitNoteMap.ContainsKey(key)) continue;

            var codProd = SafeStr(row.GetValueOrDefault("IDCodProd"), 50);
            if (string.IsNullOrEmpty(codProd) || !ProductMap.ContainsKey(codProd)) continue;

            detalles.Add(new DebitNoteDetail
            {
                DebitNoteId = DebitNoteMap[key],
                ItemNumber = SafeInt(row.GetValueOrDefault("ItemDebito")),
                ProductId = ProductMap[codProd],
                Quantity = SafeDecimal(row.GetValueOrDefault("Cantidad")),
                UnitPrice = SafeDecimal(row.GetValueOrDefault("PrecioUnitario")),
                DiscountPercent = SafeDecimal(row.GetValueOrDefault("PorcentajeDescuento")),
                DiscountAmount = SafeDecimal(row.GetValueOrDefault("ImporteDescuento")),
                Subtotal = SafeDecimal(row.GetValueOrDefault("TotalLinea")),
                UnitOfMeasure = SafeStr(row.GetValueOrDefault("UnitOfMeasure"), 20)
            });
        }

        Console.Write($"({detalles.Count} records)... ");

        for (int i = 0; i < detalles.Count; i += BatchSize)
        {
            var batch = detalles.Skip(i).Take(BatchSize).ToList();
            await db.BulkInsertAsync(batch, new BulkConfig { BatchSize = BatchSize });
            Console.Write(".");
        }

        Console.WriteLine(" OK");
    }

    private static async Task ImportNotasDebitoInternasAsync(SPCDbContext db)
    {
        Console.Write("Importing NotasDebitoInternas... ");
        var existingCount = await db.InternalDebitNotes.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "notas_debito_i_c.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        var notes = new List<InternalDebitNote>();
        var keys = new List<(string tipo, int nro)>();

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            var codCustomer = SafeInt(row.GetValueOrDefault("CodCliente"));
            if (!CustomerMap.ContainsKey(codCustomer)) continue;

            var tipo = SafeStr(row.GetValueOrDefault("TipoDebitoI"), 1) ?? "I";
            var nro = SafeInt(row.GetValueOrDefault("NroDebitoI"));
            var vendedorLegajo = SafeStr(row.GetValueOrDefault("CodVendedor"), 20);

            var cancelada = row.GetValueOrDefault("Cancelada");
            var isVoided = cancelada?.ToUpper() == "S" || cancelada?.ToUpper() == "TRUE" || cancelada == "1";

            var note = new InternalDebitNote
            {
                VoucherType = VoucherType.InternalDebitNote,
                BranchId = 1,
                InternalDebitNumber = nro,
                DebitDate = SafeDate(row.GetValueOrDefault("FechaDebitoI")) ?? DateTime.Now,
                CustomerId = CustomerMap[codCustomer],
                SalesRepId = !string.IsNullOrEmpty(vendedorLegajo) && SalesRepMap.ContainsKey(vendedorLegajo)
                    ? SalesRepMap[vendedorLegajo]
                    : null,
                Subtotal = SafeDecimal(row.GetValueOrDefault("SubTotalDebitoI")),
                DiscountPercent = SafeDecimal(row.GetValueOrDefault("PorcentajeDesc")),
                DiscountAmount = SafeDecimal(row.GetValueOrDefault("ImporteDesc")),
                Total = SafeDecimal(row.GetValueOrDefault("TotalDebitoI")),
                BusinessUnit = SafeStr(row.GetValueOrDefault("UnidadNegocio"), 50),
                SalesCondition = SafeStr(row.GetValueOrDefault("CondicionVenta"), 50),
                IsVoided = isVoided
            };

            notes.Add(note);
            keys.Add((tipo, nro));
        }

        Console.Write($"({notes.Count} records)... ");

        for (int i = 0; i < notes.Count; i += BatchSize)
        {
            var batch = notes.Skip(i).Take(BatchSize).ToList();
            await db.BulkInsertAsync(batch, new BulkConfig { SetOutputIdentity = true, BatchSize = BatchSize });

            for (int j = 0; j < batch.Count; j++)
            {
                InternalDebitMap[keys[i + j]] = batch[j].Id;
            }
            Console.Write(".");
        }

        Console.WriteLine(" OK");
    }

    private static async Task ImportNotaDebitoInternaDetallesAsync(SPCDbContext db)
    {
        Console.Write("Importing NotaDebitoInternaDetalles... ");
        var existingCount = await db.InternalDebitNoteDetails.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "notas_debito_i_d.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        var detalles = new List<InternalDebitNoteDetail>();

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            var tipo = SafeStr(row.GetValueOrDefault("TipoDebitoI"), 1) ?? "I";
            var nro = SafeInt(row.GetValueOrDefault("NroDebitoI"));
            var key = (tipo, nro);

            if (!InternalDebitMap.ContainsKey(key)) continue;

            var codProd = SafeStr(row.GetValueOrDefault("IDCodProd"), 50);
            if (string.IsNullOrEmpty(codProd) || !ProductMap.ContainsKey(codProd)) continue;

            detalles.Add(new InternalDebitNoteDetail
            {
                InternalDebitNoteId = InternalDebitMap[key],
                ItemNumber = SafeInt(row.GetValueOrDefault("ItemDebitoI")),
                ProductId = ProductMap[codProd],
                Quantity = SafeDecimal(row.GetValueOrDefault("Cantidad")),
                UnitPrice = SafeDecimal(row.GetValueOrDefault("PrecioUnitario")),
                DiscountPercent = SafeDecimal(row.GetValueOrDefault("PorcentajeDescuento")),
                DiscountAmount = SafeDecimal(row.GetValueOrDefault("ImporteDescuento")),
                Subtotal = SafeDecimal(row.GetValueOrDefault("TotalLinea")),
                UnitOfMeasure = SafeStr(row.GetValueOrDefault("UnitOfMeasure"), 20)
            });
        }

        Console.Write($"({detalles.Count} records)... ");

        for (int i = 0; i < detalles.Count; i += BatchSize)
        {
            var batch = detalles.Skip(i).Take(BatchSize).ToList();
            await db.BulkInsertAsync(batch, new BulkConfig { BatchSize = BatchSize });
            Console.Write(".");
        }

        Console.WriteLine(" OK");
    }

    private static async Task ImportConsignacionesAsync(SPCDbContext db)
    {
        Console.Write("Importing Consignaciones... ");
        var existingCount = await db.Consignments.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "consignaciones_c.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        var consignations = new List<Consignment>();
        var keys = new List<int>();

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            var nro = SafeInt(row.GetValueOrDefault("NroConsignacion"));
            var codCustomer = SafeInt(row.GetValueOrDefault("IdCliente"));
            if (!CustomerMap.ContainsKey(codCustomer)) continue;

            var vendedorLegajo = SafeStr(row.GetValueOrDefault("CodVendedor"), 20);

            var consignment = new Consignment
            {
                ConsignmentNumber = nro,
                ConsignmentDate = SafeDate(row.GetValueOrDefault("FechaC")) ?? DateTime.Now,
                CustomerId = CustomerMap[codCustomer],
                SalesRepId = !string.IsNullOrEmpty(vendedorLegajo) && SalesRepMap.ContainsKey(vendedorLegajo)
                    ? SalesRepMap[vendedorLegajo]
                    : null,
                IsActive = true
            };

            consignations.Add(consignment);
            keys.Add(nro);
        }

        Console.Write($"({consignations.Count} records)... ");

        for (int i = 0; i < consignations.Count; i += BatchSize)
        {
            var batch = consignations.Skip(i).Take(BatchSize).ToList();
            await db.BulkInsertAsync(batch, new BulkConfig { SetOutputIdentity = true, BatchSize = BatchSize });

            for (int j = 0; j < batch.Count; j++)
            {
                ConsignmentMap[keys[i + j]] = batch[j].Id;
            }
            Console.Write(".");
        }

        Console.WriteLine(" OK");
    }

    private static async Task ImportConsignacionDetallesAsync(SPCDbContext db)
    {
        Console.Write("Importing ConsignacionDetalles... ");
        var existingCount = await db.ConsignmentDetails.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "consignaciones_d.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        var detalles = new List<ConsignmentDetail>();

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            var nro = SafeInt(row.GetValueOrDefault("NroConsignacion"));
            if (!ConsignmentMap.ContainsKey(nro)) continue;

            var codProd = SafeStr(row.GetValueOrDefault("IdCodProd"), 50);
            if (string.IsNullOrEmpty(codProd) || !ProductMap.ContainsKey(codProd)) continue;

            detalles.Add(new ConsignmentDetail
            {
                ConsignmentId = ConsignmentMap[nro],
                ItemNumber = SafeInt(row.GetValueOrDefault("ItemConsignacion")),
                ProductId = ProductMap[codProd],
                Quantity = SafeDecimal(row.GetValueOrDefault("Cantidad")),
                UnitOfMeasure = SafeStr(row.GetValueOrDefault("UnitOfMeasure"), 20)
            });
        }

        Console.Write($"({detalles.Count} records)... ");

        for (int i = 0; i < detalles.Count; i += BatchSize)
        {
            var batch = detalles.Skip(i).Take(BatchSize).ToList();
            await db.BulkInsertAsync(batch, new BulkConfig { BatchSize = BatchSize });
            Console.Write(".");
        }

        Console.WriteLine(" OK");
    }

    private static async Task ImportPagosAsync(SPCDbContext db)
    {
        Console.Write("Importing Pagos... ");
        var existingCount = await db.Payments.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "pagos_c.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        var payments = new List<Payment>();
        var keys = new List<(int sucursal, double nro)>();

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            var idSucursal = SafeInt(row.GetValueOrDefault("IdSucursal"));
            var nroPago = SafeDouble(row.GetValueOrDefault("NroPago"));
            var codCustomer = SafeInt(row.GetValueOrDefault("IDCliente"));
            if (!CustomerMap.ContainsKey(codCustomer)) continue;

            var branchId = BranchMap.GetValueOrDefault(idSucursal, 1);
            var corresponde = SafeStr(row.GetValueOrDefault("Corresponde"), 200) ?? "";

            var appliesTo = corresponde.ToUpper().Contains("L2") || corresponde.ToUpper().Contains("PRESU")
                ? AccountLineType.Budget
                : AccountLineType.Billing;

            var anulada = row.GetValueOrDefault("Anulado");
            var isVoided = anulada?.ToUpper() == "S" || anulada?.ToUpper() == "TRUE" || anulada == "1";

            var payment = new Payment
            {
                BranchId = branchId,
                PaymentNumber = (long)nroPago,
                PaymentDate = SafeDate(row.GetValueOrDefault("FechaPago")) ?? DateTime.Now,
                CustomerId = CustomerMap[codCustomer],
                TotalAmount = SafeDecimal(row.GetValueOrDefault("TotalAbonado")),
                AppliesTo = appliesTo,
                AppliesToDescription = corresponde,
                IsVoided = isVoided
            };

            payments.Add(payment);
            keys.Add((idSucursal, nroPago));
        }

        Console.Write($"({payments.Count} records)... ");

        for (int i = 0; i < payments.Count; i += BatchSize)
        {
            var batch = payments.Skip(i).Take(BatchSize).ToList();
            await db.BulkInsertAsync(batch, new BulkConfig { SetOutputIdentity = true, BatchSize = BatchSize });

            for (int j = 0; j < batch.Count; j++)
            {
                PaymentMap[keys[i + j]] = batch[j].Id;
            }
            Console.Write(".");
        }

        Console.WriteLine(" OK");
    }

    private static async Task ImportPagoDetallesAsync(SPCDbContext db)
    {
        Console.Write("Importing PagoDetalles... ");
        var existingCount = await db.PaymentDetails.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "pagos_d.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        var detalles = new List<PaymentDetail>();

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            var idSucursal = SafeInt(row.GetValueOrDefault("IdSucursal"));
            var nroPago = SafeDouble(row.GetValueOrDefault("NroPago"));
            var key = (idSucursal, nroPago);

            if (!PaymentMap.ContainsKey(key)) continue;

            var formaPago = SafeStr(row.GetValueOrDefault("FormaPago"), 50);
            var lookupKey = formaPago?.ToUpper() ?? "";
            var paymentMethodId = PaymentMethodMap.ContainsKey(lookupKey)
                ? PaymentMethodMap[lookupKey]
                : (PaymentMethodMap.Values.FirstOrDefault() == 0 ? 1 : PaymentMethodMap.Values.FirstOrDefault());

            detalles.Add(new PaymentDetail
            {
                PaymentId = PaymentMap[key],
                LineNumber = SafeInt(row.GetValueOrDefault("LineaPago")),
                PaymentMethodId = paymentMethodId,
                Amount = SafeDecimal(row.GetValueOrDefault("ImportePago")),
                Notes = SafeStr(row.GetValueOrDefault("Observaciones"), 500)
            });
        }

        Console.Write($"({detalles.Count} records)... ");

        for (int i = 0; i < detalles.Count; i += BatchSize)
        {
            var batch = detalles.Skip(i).Take(BatchSize).ToList();
            await db.BulkInsertAsync(batch, new BulkConfig { BatchSize = BatchSize });
            Console.Write(".");
        }

        Console.WriteLine(" OK");
    }

    private static async Task ImportCtaCteAsync(SPCDbContext db)
    {
        Console.Write("Importing CtaCte... ");
        var existingCount = await db.CurrentAccounts.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "cta_cte.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        var accounts = new List<CurrentAccount>();

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            var codCustomer = SafeInt(row.GetValueOrDefault("IDCliente"));
            if (!CustomerMap.ContainsKey(codCustomer)) continue;

            accounts.Add(new CurrentAccount
            {
                CustomerId = CustomerMap[codCustomer],
                BillingBalance = SafeDecimal(row.GetValueOrDefault("SaldoL1")),
                BudgetBalance = SafeDecimal(row.GetValueOrDefault("SaldoL2")),
                TotalBalance = SafeDecimal(row.GetValueOrDefault("SaldoTotal")),
                LastUpdated = SafeDate(row.GetValueOrDefault("FechaActSaldo")) ?? DateTime.Now
            });
        }

        Console.Write($"({accounts.Count} records)... ");

        for (int i = 0; i < accounts.Count; i += BatchSize)
        {
            var batch = accounts.Skip(i).Take(BatchSize).ToList();
            await db.BulkInsertAsync(batch, new BulkConfig { BatchSize = BatchSize });
            Console.Write(".");
        }

        Console.WriteLine(" OK");
    }

    private static async Task ImportMovimientosCtaCteAsync(SPCDbContext db)
    {
        Console.Write("Importing MovimientosCtaCte... ");
        var existingCount = await db.CurrentAccountMovements.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "movimientos_cta_cte.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        var movimientos = new List<CurrentAccountMovement>();
        decimal runningBilling = 0;
        decimal runningBudget = 0;
        int lastCustomerId = 0;

        int count = 0;
        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            var codCustomer = SafeInt(row.GetValueOrDefault("IDCliente"));
            if (!CustomerMap.ContainsKey(codCustomer)) continue;

            var customerId = CustomerMap[codCustomer];
            if (customerId != lastCustomerId)
            {
                runningBilling = 0;
                runningBudget = 0;
                lastCustomerId = customerId;
            }

            var tipoDoc = SafeStr(row.GetValueOrDefault("TipoDoc"), 50) ?? "";
            var billingAmount = SafeDecimal(row.GetValueOrDefault("ImporteLinea1"));
            var budgetAmount = SafeDecimal(row.GetValueOrDefault("ImporteLinea2"));

            runningBilling += billingAmount;
            runningBudget += budgetAmount;

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

            movimientos.Add(new CurrentAccountMovement
            {
                MovementDate = SafeDate(row.GetValueOrDefault("Fecha")) ?? DateTime.Now,
                CustomerId = customerId,
                DocumentType = documentType,
                DocumentNumber = (long)SafeDouble(row.GetValueOrDefault("NroDoc")),
                BillingAmount = billingAmount,
                BudgetAmount = budgetAmount,
                BillingRunningBalance = runningBilling,
                BudgetRunningBalance = runningBudget,
                Description = tipoDoc
            });

            count++;
            if (movimientos.Count >= BatchSize)
            {
                await db.BulkInsertAsync(movimientos, new BulkConfig { BatchSize = BatchSize });
                movimientos.Clear();
                Console.Write(".");
            }
        }

        if (movimientos.Count > 0)
        {
            await db.BulkInsertAsync(movimientos, new BulkConfig { BatchSize = BatchSize });
            Console.Write(".");
        }

        Console.WriteLine($" OK ({count} records)");
    }

    #region Helper Methods

    private static string[] ParseCsvLine(string line)
    {
        // Simple CSV parsing (handles semicolon delimiter)
        var result = new List<string>();
        var current = "";
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ';' && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }
        result.Add(current);
        return result.ToArray();
    }

    private static Dictionary<string, string> CreateDictionary(string[] header, string[] values)
    {
        var dict = new Dictionary<string, string>();
        for (int i = 0; i < header.Length && i < values.Length; i++)
        {
            dict[header[i]] = values[i];
        }
        return dict;
    }

    private static string? SafeStr(string? val, int maxLen = 500)
    {
        if (string.IsNullOrWhiteSpace(val)) return null;
        var s = val.Trim();
        return s.Length > maxLen ? s[..maxLen] : s;
    }

    private static int SafeInt(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return 0;
        if (double.TryParse(val.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            return (int)d;
        return 0;
    }

    private static double SafeDouble(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return 0;
        if (double.TryParse(val.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            return d;
        return 0;
    }

    private static decimal SafeDecimal(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return 0;
        if (decimal.TryParse(val.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
        {
            if (d > 999999999999.9999m) return 999999999999.9999m;
            if (d < -999999999999.9999m) return -999999999999.9999m;
            return d;
        }
        return 0;
    }

    private static DateTime? SafeDate(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return null;
        
        // Try various formats
        string[] formats = { 
            "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd", 
            "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy",
            "M/d/yyyy h:mm:ss tt", "M/d/yyyy"
        };
        
        foreach (var fmt in formats)
        {
            if (DateTime.TryParseExact(val.Split('.')[0], fmt, CultureInfo.InvariantCulture, 
                DateTimeStyles.None, out var dt))
                return dt;
        }
        
        if (DateTime.TryParse(val, out var result))
            return result;
            
        return null;
    }

    private static void LogSkip(string fileName, int lineNumber, string reason, Dictionary<string, string> row)
    {
        if (!File.Exists(SkipLogPath))
        {
            File.AppendAllLines(SkipLogPath, new[] { "Timestamp\tFile\tLine\tReason\tDetails" });
        }

        var details = new List<string>();
        AddIfPresent(details, "TipoInvoice", row);
        AddIfPresent(details, "NroInvoice", row);
        AddIfPresent(details, "TipoNotaCredito", row);
        AddIfPresent(details, "NroNotaCredito", row);
        AddIfPresent(details, "TipoDebito", row);
        AddIfPresent(details, "NroDebito", row);
        AddIfPresent(details, "CodCustomer", row);
        AddIfPresent(details, "IDCustomer", row);

        var detailText = details.Count > 0 ? string.Join(" ", details) : "(no key fields)";
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{fileName}\tL{lineNumber}\t{reason}\t{detailText}";
        File.AppendAllLines(SkipLogPath, new[] { line });
    }

    private static void AddIfPresent(List<string> details, string key, Dictionary<string, string> row)
    {
        if (row.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            details.Add($"{key}={value.Trim()}");
        }
    }

    #endregion
}
