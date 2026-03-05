using System.Data.OleDb;
using System.Globalization;
using EFCore.BulkExtensions;
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

    // Mapping dictionaries
    private static readonly Dictionary<int, int> ClienteMap = new();
    private static readonly Dictionary<string, int> VendedorMap = new();
    private static readonly Dictionary<string, int> ProductoMap = new();
    private static readonly Dictionary<int, int> BranchMap = new();
    private static readonly Dictionary<string, int> PaymentMethodMap = new();
    private static readonly Dictionary<(string, int), int> FacturaMap = new();
    private static readonly Dictionary<(int, int), int> RemitoMap = new();
    private static readonly Dictionary<int, int> QuoteMap = new();
    private static readonly Dictionary<(string, int), int> CreditNoteMap = new();
    private static readonly Dictionary<(string, int), int> DebitNoteMap = new();
    private static readonly Dictionary<(string, int), int> InternalDebitMap = new();
    private static readonly Dictionary<int, int> ConsignmentMap = new();
    private static readonly Dictionary<(int, double), int> PaymentMap = new();

    public static async Task RunAsync(SPCDbContext db)
    {
        Console.WriteLine("Loading mappings from existing data...");
        await LoadMappingsAsync(db);
        Console.WriteLine();

        // Import documents
        await ImportFacturasAsync(db);
        await ImportFacturaDetallesAsync(db);
        await ImportRemitosAsync(db);
        await ImportRemitoDetallesAsync(db);
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

    private static async Task LoadMappingsAsync(SPCDbContext db)
    {
        // Load Clientes - map Access IDCliente -> SQL Id by order
        var sqlClientes = await db.Clientes.OrderBy(c => c.Id).Select(c => c.Id).ToListAsync();
        var accessClienteIds = new List<int>();

        using (var accessConn = new OleDbConnection($"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={AccessDbPath};"))
        {
            await accessConn.OpenAsync();
            using var cmd = new OleDbCommand("SELECT IDCliente FROM Clientes ORDER BY IDCliente", accessConn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (reader.Read())
            {
                accessClienteIds.Add(reader.GetInt32(0));
            }
        }

        var mapCount = Math.Min(sqlClientes.Count, accessClienteIds.Count);
        for (int i = 0; i < mapCount; i++)
        {
            ClienteMap[accessClienteIds[i]] = sqlClientes[i];
        }
        Console.WriteLine($"  Clientes: {ClienteMap.Count}");

        // Vendedores
        var vendedores = await db.Vendedores.ToListAsync();
        foreach (var v in vendedores)
        {
            VendedorMap[v.Legajo] = v.Id;
        }
        Console.WriteLine($"  Vendedores: {VendedorMap.Count}");

        // Productos
        var productos = await db.Productos.ToListAsync();
        foreach (var p in productos)
        {
            ProductoMap[p.Codigo] = p.Id;
        }
        Console.WriteLine($"  Productos: {ProductoMap.Count}");

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

    private static async Task ImportFacturasAsync(SPCDbContext db)
    {
        Console.Write("Importing Facturas... ");
        
        var existingCount = await db.Facturas.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            var existing = await db.Facturas.ToListAsync();
            foreach (var f in existing)
            {
                FacturaMap[(f.TipoFactura, (int)f.NumeroFactura)] = f.Id;
            }
            return;
        }

        var csvPath = Path.Combine(DataDir, "facturas_c.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');
        
        var facturas = new List<Factura>();
        var keys = new List<(string tipo, int nro)>();

        for (int i = 1; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var cols = ParseCsvLine(lines[i]);
            var row = CreateDictionary(header, cols);
            
            var codCliente = SafeInt(row.GetValueOrDefault("CodCliente"));
            if (!ClienteMap.ContainsKey(codCliente)) continue;

            var tipoRaw = row.GetValueOrDefault("TipoFactura");
            var nroRaw = row.GetValueOrDefault("NroFactura");
            if (string.IsNullOrWhiteSpace(tipoRaw) || string.IsNullOrWhiteSpace(nroRaw))
            {
                LogSkip("facturas_c.csv", lineNumber, "Missing TipoFactura or NroFactura", row);
                continue;
            }

            var tipo = SafeStr(tipoRaw, 1)!;
            var nro = SafeInt(nroRaw);
            var idSucursal = SafeInt(row.GetValueOrDefault("IdSucursal"));
            var branchId = BranchMap.GetValueOrDefault(idSucursal, 1);
            var puntoVenta = idSucursal > 0 ? idSucursal : 2;
            var vendedorLegajo = SafeStr(row.GetValueOrDefault("CodVendedor"), 20);

            var factura = new Factura
            {
                BranchId = branchId,
                TipoFactura = tipo,
                PuntoVenta = puntoVenta,
                NumeroFactura = nro,
                FechaFactura = SafeDate(row.GetValueOrDefault("FechaFactura")) ?? DateTime.Now,
                ClienteId = ClienteMap[codCliente],
                VendedorId = !string.IsNullOrEmpty(vendedorLegajo) && VendedorMap.ContainsKey(vendedorLegajo) 
                    ? VendedorMap[vendedorLegajo] : null,
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
                FacturaMap[keys[i + j]] = batch[j].Id;
            }
            Console.Write(".");
        }

        Console.WriteLine(" OK");
    }

    private static async Task ImportFacturaDetallesAsync(SPCDbContext db)
    {
        Console.Write("Importing FacturaDetalles... ");

        var existingCount = await db.FacturaDetalles.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }

        var csvPath = Path.Combine(DataDir, "facturas_d.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        var detalles = new List<FacturaDetalle>();

        for (int i = 1; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var cols = ParseCsvLine(lines[i]);
            var row = CreateDictionary(header, cols);

            var tipoRaw = row.GetValueOrDefault("TipoFactura");
            var nroRaw = row.GetValueOrDefault("NroFactura");
            if (string.IsNullOrWhiteSpace(tipoRaw) || string.IsNullOrWhiteSpace(nroRaw))
            {
                LogSkip("facturas_d.csv", lineNumber, "Missing TipoFactura or NroFactura", row);
                continue;
            }

            var tipo = SafeStr(tipoRaw, 1)!;
            var nro = SafeInt(nroRaw);
            var key = (tipo, nro);

            if (!FacturaMap.ContainsKey(key)) continue;

            var codProd = SafeStr(row.GetValueOrDefault("IDCodProd"), 50);
            if (string.IsNullOrEmpty(codProd) || !ProductoMap.ContainsKey(codProd)) continue;

            detalles.Add(new FacturaDetalle
            {
                FacturaId = FacturaMap[key],
                ItemNumero = SafeInt(row.GetValueOrDefault("ItemFactura")),
                ProductoId = ProductoMap[codProd],
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
    private static async Task ImportRemitosAsync(SPCDbContext db) 
    {
        Console.Write("Importing Remitos... ");
        var existingCount = await db.Remitos.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            var existing = await db.Remitos.ToListAsync();
            foreach (var r in existing)
            {
                RemitoMap[(r.PuntoVenta, (int)r.NumeroRemito)] = r.Id;
            }
            return;
        }
        var csvPath = Path.Combine(DataDir, "remitos_c.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        var remitos = new List<Remito>();
        var keys = new List<(int sucursal, int nro)>();

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            var idSucursal = SafeInt(row.GetValueOrDefault("IdSucursal"));
            var nroRemito = SafeInt(row.GetValueOrDefault("NroRemito"));
            var codCliente = SafeInt(row.GetValueOrDefault("CodCliente"));
            if (!ClienteMap.ContainsKey(codCliente)) continue;

            var vendedorLegajo = SafeStr(row.GetValueOrDefault("CodVendedor"), 20);
            var nroFactura = SafeInt(row.GetValueOrDefault("NroFactura"));
            var tipoFactura = SafeStr(row.GetValueOrDefault("TipoFactura"), 2);

            var branchId = BranchMap.GetValueOrDefault(idSucursal, 1);
            var puntoVenta = idSucursal > 0 ? idSucursal : 2;

            int? facturaId = null;
            if (nroFactura > 0 && !string.IsNullOrEmpty(tipoFactura) && FacturaMap.ContainsKey((tipoFactura, nroFactura)))
            {
                facturaId = FacturaMap[(tipoFactura, nroFactura)];
            }

            var remito = new Remito
            {
                BranchId = branchId,
                PuntoVenta = puntoVenta,
                NumeroRemito = nroRemito,
                FechaRemito = SafeDate(row.GetValueOrDefault("FechaRemito")) ?? DateTime.Now,
                ClienteId = ClienteMap[codCliente],
                VendedorId = !string.IsNullOrEmpty(vendedorLegajo) && VendedorMap.ContainsKey(vendedorLegajo)
                    ? VendedorMap[vendedorLegajo]
                    : null,
                UnidadNegocio = SafeStr(row.GetValueOrDefault("UnidadNegocio"), 50),
                FacturaId = facturaId,
                TipoFactura = tipoFactura,
                Facturado = facturaId.HasValue,
                Aclaracion = SafeStr(row.GetValueOrDefault("AclaracionRemito"), 500),
                Anulado = false
            };

            remitos.Add(remito);
            keys.Add((idSucursal, nroRemito));
        }

        Console.Write($"({remitos.Count} records)... ");

        for (int i = 0; i < remitos.Count; i += BatchSize)
        {
            var batch = remitos.Skip(i).Take(BatchSize).ToList();
            await db.BulkInsertAsync(batch, new BulkConfig { SetOutputIdentity = true, BatchSize = BatchSize });

            for (int j = 0; j < batch.Count; j++)
            {
                RemitoMap[keys[i + j]] = batch[j].Id;
            }
            Console.Write(".");
        }

        Console.WriteLine(" OK");
    }

    private static async Task ImportRemitoDetallesAsync(SPCDbContext db) 
    {
        Console.Write("Importing RemitoDetalles... ");
        var existingCount = await db.RemitoDetalles.CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine($"SKIP ({existingCount} already exist)");
            return;
        }
        var csvPath = Path.Combine(DataDir, "remitos_d.csv");
        var lines = await File.ReadAllLinesAsync(csvPath);
        var header = lines[0].Split(';');

        var detalles = new List<RemitoDetalle>();

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line);
            var row = CreateDictionary(header, cols);

            var idSucursal = SafeInt(row.GetValueOrDefault("IdSucursal"));
            var nroRemito = SafeInt(row.GetValueOrDefault("NroRemito"));
            var key = (idSucursal, nroRemito);

            if (!RemitoMap.ContainsKey(key)) continue;

            var codProd = SafeStr(row.GetValueOrDefault("IDCodProd"), 50);
            if (string.IsNullOrEmpty(codProd) || !ProductoMap.ContainsKey(codProd)) continue;

            detalles.Add(new RemitoDetalle
            {
                RemitoId = RemitoMap[key],
                ItemNumero = SafeInt(row.GetValueOrDefault("ItemRemito")),
                ProductoId = ProductoMap[codProd],
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
            var codCliente = SafeInt(row.GetValueOrDefault("CodCliente"));
            if (!ClienteMap.ContainsKey(codCliente)) continue;

            var vendedorLegajo = SafeStr(row.GetValueOrDefault("CodVendedor"), 20);

            quotes.Add(new Quote
            {
                BranchId = 1,
                QuoteNumber = nroPresu,
                QuoteDate = SafeDate(row.GetValueOrDefault("FechaPresu")) ?? DateTime.Now,
                CustomerId = ClienteMap[codCliente],
                SalesRepId = !string.IsNullOrEmpty(vendedorLegajo) && VendedorMap.ContainsKey(vendedorLegajo)
                    ? VendedorMap[vendedorLegajo]
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
            if (string.IsNullOrEmpty(codProd) || !ProductoMap.ContainsKey(codProd)) continue;

            detalles.Add(new QuoteDetail
            {
                QuoteId = QuoteMap[nroPresu],
                ItemNumber = SafeInt(row.GetValueOrDefault("ItemPresu")),
                ProductId = ProductoMap[codProd],
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

            var codCliente = SafeInt(row.GetValueOrDefault("CodCliente"));
            if (!ClienteMap.ContainsKey(codCliente)) continue;

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
                CustomerId = ClienteMap[codCliente],
                SalesRepId = !string.IsNullOrEmpty(vendedorLegajo) && VendedorMap.ContainsKey(vendedorLegajo)
                    ? VendedorMap[vendedorLegajo]
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
            if (string.IsNullOrEmpty(codProd) || !ProductoMap.ContainsKey(codProd)) continue;

            detalles.Add(new CreditNoteDetail
            {
                CreditNoteId = CreditNoteMap[key],
                ItemNumber = SafeInt(row.GetValueOrDefault("ItemNotaCredito")),
                ProductId = ProductoMap[codProd],
                Quantity = SafeDecimal(row.GetValueOrDefault("Cantidad")),
                UnitPrice = SafeDecimal(row.GetValueOrDefault("PrecioUnitario")),
                DiscountPercent = SafeDecimal(row.GetValueOrDefault("PorcentajeDescuento")),
                DiscountAmount = SafeDecimal(row.GetValueOrDefault("ImporteDescuento")),
                Subtotal = SafeDecimal(row.GetValueOrDefault("TotalLinea")),
                UnitOfMeasure = SafeStr(row.GetValueOrDefault("UnidadMedida"), 20)
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

            var codCliente = SafeInt(row.GetValueOrDefault("CodCliente"));
            if (!ClienteMap.ContainsKey(codCliente)) continue;

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
                CustomerId = ClienteMap[codCliente],
                SalesRepId = !string.IsNullOrEmpty(vendedorLegajo) && VendedorMap.ContainsKey(vendedorLegajo)
                    ? VendedorMap[vendedorLegajo]
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
            if (string.IsNullOrEmpty(codProd) || !ProductoMap.ContainsKey(codProd)) continue;

            detalles.Add(new DebitNoteDetail
            {
                DebitNoteId = DebitNoteMap[key],
                ItemNumber = SafeInt(row.GetValueOrDefault("ItemDebito")),
                ProductId = ProductoMap[codProd],
                Quantity = SafeDecimal(row.GetValueOrDefault("Cantidad")),
                UnitPrice = SafeDecimal(row.GetValueOrDefault("PrecioUnitario")),
                DiscountPercent = SafeDecimal(row.GetValueOrDefault("PorcentajeDescuento")),
                DiscountAmount = SafeDecimal(row.GetValueOrDefault("ImporteDescuento")),
                Subtotal = SafeDecimal(row.GetValueOrDefault("TotalLinea")),
                UnitOfMeasure = SafeStr(row.GetValueOrDefault("UnidadMedida"), 20)
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

            var codCliente = SafeInt(row.GetValueOrDefault("CodCliente"));
            if (!ClienteMap.ContainsKey(codCliente)) continue;

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
                CustomerId = ClienteMap[codCliente],
                SalesRepId = !string.IsNullOrEmpty(vendedorLegajo) && VendedorMap.ContainsKey(vendedorLegajo)
                    ? VendedorMap[vendedorLegajo]
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
            if (string.IsNullOrEmpty(codProd) || !ProductoMap.ContainsKey(codProd)) continue;

            detalles.Add(new InternalDebitNoteDetail
            {
                InternalDebitNoteId = InternalDebitMap[key],
                ItemNumber = SafeInt(row.GetValueOrDefault("ItemDebitoI")),
                ProductId = ProductoMap[codProd],
                Quantity = SafeDecimal(row.GetValueOrDefault("Cantidad")),
                UnitPrice = SafeDecimal(row.GetValueOrDefault("PrecioUnitario")),
                DiscountPercent = SafeDecimal(row.GetValueOrDefault("PorcentajeDescuento")),
                DiscountAmount = SafeDecimal(row.GetValueOrDefault("ImporteDescuento")),
                Subtotal = SafeDecimal(row.GetValueOrDefault("TotalLinea")),
                UnitOfMeasure = SafeStr(row.GetValueOrDefault("UnidadMedida"), 20)
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
            var codCliente = SafeInt(row.GetValueOrDefault("IdCliente"));
            if (!ClienteMap.ContainsKey(codCliente)) continue;

            var vendedorLegajo = SafeStr(row.GetValueOrDefault("CodVendedor"), 20);

            var consignment = new Consignment
            {
                ConsignmentNumber = nro,
                ConsignmentDate = SafeDate(row.GetValueOrDefault("FechaC")) ?? DateTime.Now,
                CustomerId = ClienteMap[codCliente],
                SalesRepId = !string.IsNullOrEmpty(vendedorLegajo) && VendedorMap.ContainsKey(vendedorLegajo)
                    ? VendedorMap[vendedorLegajo]
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
            if (string.IsNullOrEmpty(codProd) || !ProductoMap.ContainsKey(codProd)) continue;

            detalles.Add(new ConsignmentDetail
            {
                ConsignmentId = ConsignmentMap[nro],
                ItemNumber = SafeInt(row.GetValueOrDefault("ItemConsignacion")),
                ProductId = ProductoMap[codProd],
                Quantity = SafeDecimal(row.GetValueOrDefault("Cantidad")),
                UnitOfMeasure = SafeStr(row.GetValueOrDefault("UnidadMedida"), 20)
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
            var codCliente = SafeInt(row.GetValueOrDefault("IDCliente"));
            if (!ClienteMap.ContainsKey(codCliente)) continue;

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
                CustomerId = ClienteMap[codCliente],
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

            var codCliente = SafeInt(row.GetValueOrDefault("IDCliente"));
            if (!ClienteMap.ContainsKey(codCliente)) continue;

            accounts.Add(new CurrentAccount
            {
                CustomerId = ClienteMap[codCliente],
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

            var codCliente = SafeInt(row.GetValueOrDefault("IDCliente"));
            if (!ClienteMap.ContainsKey(codCliente)) continue;

            var customerId = ClienteMap[codCliente];
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
        AddIfPresent(details, "TipoFactura", row);
        AddIfPresent(details, "NroFactura", row);
        AddIfPresent(details, "TipoNotaCredito", row);
        AddIfPresent(details, "NroNotaCredito", row);
        AddIfPresent(details, "TipoDebito", row);
        AddIfPresent(details, "NroDebito", row);
        AddIfPresent(details, "CodCliente", row);
        AddIfPresent(details, "IDCliente", row);

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
