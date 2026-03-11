using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.Invoices;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Invoices service implementation for invoice operations.
/// Handles different calculation rules for Invoice A vs Invoice B.
/// </summary>
public class InvoicesService : IInvoicesService
{
    private readonly SPCDbContext _db;
    private readonly ITaxConfigurationService _taxService;
    private readonly IPricingService _pricingService;
    private readonly ICompanySettingsService _companySettings;

    public InvoicesService(
        SPCDbContext db,
        ITaxConfigurationService taxService,
        IPricingService pricingService,
        ICompanySettingsService companySettings)
    {
        _db = db;
        _taxService = taxService;
        _pricingService = pricingService;
        _companySettings = companySettings;
    }

    public async Task<IEnumerable<InvoiceResponse>> GetAllAsync(int skip = 0, int take = 50)
    {
        var facturas = await _db.Invoices
            .Include(f => f.Customer)
            .Include(f => f.SalesRep)
            .Include(f => f.Detalles)
            .OrderByDescending(f => f.FechaInvoice)
            .ThenByDescending(f => f.NumeroInvoice)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return facturas.Select(MapToResponse);
    }

    public async Task<InvoiceCompletaResponse?> GetByIdAsync(int id)
    {
        var factura = await _db.Invoices
            .Include(f => f.Customer)
            .Include(f => f.SalesRep)
            .Include(f => f.Detalles)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (factura == null) return null;

        return new InvoiceCompletaResponse
        {
            Id = factura.Id,
            TipoInvoice = factura.TipoInvoice,
            PuntoVenta = factura.PuntoVenta,
            NumeroInvoice = factura.NumeroInvoice,
            FechaInvoice = factura.FechaInvoice,
            CustomerId = factura.CustomerId,
            CustomerRazonSocial = factura.Customer.RazonSocial,
            CustomerCUIT = factura.Customer.CUIT,
            SalesRepId = factura.SalesRepId,
            SalesRepNombre = factura.SalesRep?.Nombre,
            Subtotal = factura.Subtotal,
            ImporteIVA = factura.ImporteIVA,
            IVAContenido = factura.IVAContenido,
            ImportePercepcionIIBB = factura.ImportePercepcionIIBB,
            ImporteDescuento = factura.ImporteDescuento,
            Total = factura.Total,
            CAE = factura.CAE,
            FechaVencimientoCAE = factura.FechaVencimientoCAE,
            Anulada = factura.Anulada,
            CantidadItems = factura.Detalles.Count,
            Detalles = factura.Detalles.Select(d => new InvoiceDetailResponse
            {
                Id = d.Id,
                ItemNumero = d.ItemNumero,
                ProductId = d.ProductId,
                ProductCodigo = d.Product.Codigo,
                ProductDescripcion = d.Product.Descripcion,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                PorcentajeDescuento = d.PorcentajeDescuento,
                PorcentajeIVA = d.PorcentajeIVA,
                Subtotal = d.Subtotal
            }).OrderBy(d => d.ItemNumero).ToList()
        };
    }

    public async Task<IEnumerable<InvoiceResponse>> GetByCustomerAsync(int clienteId)
    {
        var facturas = await _db.Invoices
            .Include(f => f.Customer)
            .Include(f => f.SalesRep)
            .Include(f => f.Detalles)
            .Where(f => f.CustomerId == clienteId)
            .OrderByDescending(f => f.FechaInvoice)
            .ThenByDescending(f => f.NumeroInvoice)
            .ToListAsync();

        return facturas.Select(MapToResponse);
    }

    public async Task<IEnumerable<InvoiceResponse>> GetByFechaAsync(DateTime desde, DateTime hasta)
    {
        var facturas = await _db.Invoices
            .Include(f => f.Customer)
            .Include(f => f.SalesRep)
            .Include(f => f.Detalles)
            .Where(f => f.FechaInvoice >= desde && f.FechaInvoice <= hasta)
            .OrderByDescending(f => f.FechaInvoice)
            .ThenByDescending(f => f.NumeroInvoice)
            .ToListAsync();

        return facturas.Select(MapToResponse);
    }

    public async Task<IEnumerable<InvoiceResponse>> SearchAsync(string termino)
    {
        // Try to parse as number for invoice search
        long.TryParse(termino.Replace("-", ""), out var numeroInvoice);

        var facturas = await _db.Invoices
            .Include(f => f.Customer)
            .Include(f => f.SalesRep)
            .Include(f => f.Detalles)
            .Where(f => f.NumeroInvoice == numeroInvoice ||
                       f.Customer.RazonSocial.Contains(termino) ||
                       (f.Customer.CUIT != null && f.Customer.CUIT.Contains(termino)))
            .OrderByDescending(f => f.FechaInvoice)
            .Take(100)
            .ToListAsync();

        return facturas.Select(MapToResponse);
    }

    public async Task<InvoicecionResumenResponse> GetResumenAsync()
    {
        var hoy = DateTime.Today;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
        var inicioAnio = new DateTime(hoy.Year, 1, 1);

        var facturasHoy = await _db.Invoices
            .Where(f => f.FechaInvoice.Date == hoy && !f.Anulada)
            .ToListAsync();

        var facturasMes = await _db.Invoices
            .Where(f => f.FechaInvoice >= inicioMes && !f.Anulada)
            .ToListAsync();

        var facturasAnio = await _db.Invoices
            .Where(f => f.FechaInvoice >= inicioAnio && !f.Anulada)
            .ToListAsync();

        var totalInvoices = await _db.Invoices.CountAsync();

        return new InvoicecionResumenResponse
        {
            TotalInvoices = totalInvoices,
            InvoicesHoy = facturasHoy.Count,
            InvoicesMes = facturasMes.Count,
            MontoHoy = facturasHoy.Sum(f => f.Total),
            MontoMes = facturasMes.Sum(f => f.Total),
            MontoAnio = facturasAnio.Sum(f => f.Total)
        };
    }

    public async Task<int> GetCountAsync()
    {
        return await _db.Invoices.CountAsync();
    }

    // ===========================================
    // COMMANDS
    // ===========================================

    public async Task<InvoiceCompletaResponse> CreateAsync(CreateInvoiceRequest request)
    {
        // 1. Validate and load customer
        var cliente = await _db.Customers
            .Include(c => c.TaxCondition)
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId)
            ?? throw new InvalidOperationException($"Customer {request.CustomerId} no encontrado");

        // 2. Validate and load branch
        var branch = await _db.Branches.FindAsync(request.BranchId)
            ?? throw new InvalidOperationException($"Sucursal {request.BranchId} no encontrada");

        // 3. Load all products for validation
        var productIds = request.Detalles.Select(d => d.ProductId).Distinct().ToList();
        var productos = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var detalle in request.Detalles)
        {
            if (!productos.ContainsKey(detalle.ProductId))
                throw new InvalidOperationException($"Product {detalle.ProductId} no encontrado");
        }

        // 4. Get VAT rate from configuration (not hardcoded!)
        var vatRate = await _taxService.GetDefaultVATRateAsync();

        // 5. Resolve document discount (use customer default if not specified)
        var documentDiscount = _pricingService.ResolveDiscount(
            request.PorcentajeDescuento,
            cliente.PorcentajeDescuento);

        // 6. Determine if this is Invoice A or B
        bool isInvoiceA = request.TipoInvoice.ToUpperInvariant() == "A";

        // 7. Calculate line items
        // Invoice A: uses PrecioInvoice (net price, VAT will be added)
        // Invoice B: uses PrecioQuote (final price with VAT included)
        var lineResults = new List<(CreateInvoiceDetailRequest detalle, LineCalculationResult calc, Product producto)>();
        foreach (var detalle in request.Detalles)
        {
            var producto = productos[detalle.ProductId];
            
            // Select price based on invoice type
            decimal unitPrice;
            if (detalle.PrecioUnitario.HasValue)
            {
                unitPrice = detalle.PrecioUnitario.Value;
            }
            else
            {
                unitPrice = isInvoiceA ? producto.PrecioInvoice : producto.PrecioQuote;
            }
            
            var lineVat = detalle.PorcentajeIVA ?? producto.PorcentajeIVA;
            
            var lineCalc = _pricingService.CalculateLine(
                unitPrice,
                detalle.Cantidad,
                detalle.PorcentajeDescuento,
                lineVat);
            
            lineResults.Add((detalle, lineCalc, producto));
        }

        // 8. Get company settings for IIBB agent status
        var isIIBBAgent = await _companySettings.IsIIBBPerceptionAgentAsync();
        
        // IIBB rate: use customer's rate from padrón, or request override
        var customerIIBBRate = request.AlicuotaIIBB > 0 
            ? request.AlicuotaIIBB 
            : cliente.AlicuotaIIBB;

        // 9. Calculate document totals based on invoice type
        Invoice factura;
        var nextNumber = await GetNextInvoiceNumberAsync(request.TipoInvoice, branch.PointOfSale);

        if (isInvoiceA)
        {
            // Invoice A: Net prices + VAT discriminated + IIBB if agent
            var docCalc = _pricingService.CalculateDocumentTypeA(
                lineResults.Select(l => l.calc),
                documentDiscount,
                vatRate,
                customerIIBBRate,
                isIIBBAgent);

            factura = new Invoice
            {
                BranchId = request.BranchId,
                TipoInvoice = "A",
                PuntoVenta = branch.PointOfSale,
                NumeroInvoice = nextNumber,
                FechaInvoice = DateTime.Now,
                CustomerId = request.CustomerId,
                SalesRepId = request.SalesRepId,
                
                PorcentajeIVA = vatRate,
                Subtotal = docCalc.NetSubtotal,
                ImporteIVA = docCalc.VATAmount,
                IVAContenido = 0, // Invoice A: IVA discriminado, no contenido
                
                AlicuotaIIBB = docCalc.IIBBPercent,
                ImportePercepcionIIBB = docCalc.IIBBAmount,
                PorcentajeDescuento = documentDiscount,
                ImporteDescuento = docCalc.DocumentDiscountAmount,
                Total = docCalc.Total,
                
                CondicionVenta = request.CondicionVenta,
                Observaciones = request.Observaciones,
                Anulada = false
            };
        }
        else
        {
            // Invoice B: Final prices with VAT included, show IVA Contenido
            var docCalc = _pricingService.CalculateDocumentTypeB(
                lineResults.Select(l => l.calc),
                documentDiscount,
                vatRate);

            factura = new Invoice
            {
                BranchId = request.BranchId,
                TipoInvoice = "B",
                PuntoVenta = branch.PointOfSale,
                NumeroInvoice = nextNumber,
                FechaInvoice = DateTime.Now,
                CustomerId = request.CustomerId,
                SalesRepId = request.SalesRepId,
                
                PorcentajeIVA = vatRate,
                Subtotal = docCalc.Total, // In Invoice B, Subtotal = Total
                ImporteIVA = 0, // Invoice B: IVA not discriminated
                IVAContenido = docCalc.VATContained, // Ley 27.743
                
                AlicuotaIIBB = 0, // Invoice B typically no IIBB perception
                ImportePercepcionIIBB = 0,
                PorcentajeDescuento = documentDiscount,
                ImporteDescuento = docCalc.DocumentDiscountAmount,
                Total = docCalc.Total,
                
                CondicionVenta = request.CondicionVenta,
                Observaciones = request.Observaciones,
                Anulada = false
            };
        }

        // 10. Create detail lines
        int itemNumber = 1;
        foreach (var (detalle, calc, producto) in lineResults)
        {
            factura.Detalles.Add(new InvoiceDetail
            {
                ItemNumero = itemNumber++,
                ProductId = detalle.ProductId,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = calc.UnitPrice,
                PorcentajeDescuento = calc.DiscountPercent,
                PorcentajeIVA = calc.VATPercent,
                Subtotal = calc.Subtotal
            });
        }

        // 11. Save to database
        _db.Invoices.Add(factura);
        await _db.SaveChangesAsync();

        // 12. Return complete response
        return (await GetByIdAsync(factura.Id))!;
    }

    public async Task<bool> AnularAsync(int id, string motivo)
    {
        var factura = await _db.Invoices.FindAsync(id);
        if (factura == null)
            return false;

        if (factura.Anulada)
            return false; // Already voided

        factura.Anulada = true;
        factura.Observaciones = string.IsNullOrEmpty(factura.Observaciones)
            ? $"ANULADA: {motivo}"
            : $"{factura.Observaciones} | ANULADA: {motivo}";

        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Gets the next invoice number for a given type and point of sale
    /// </summary>
    private async Task<long> GetNextInvoiceNumberAsync(string tipoInvoice, int puntoVenta)
    {
        var lastNumber = await _db.Invoices
            .Where(f => f.TipoInvoice == tipoInvoice && f.PuntoVenta == puntoVenta)
            .MaxAsync(f => (long?)f.NumeroInvoice) ?? 0;

        return lastNumber + 1;
    }

    // ===========================================
    // MAPPING
    // ===========================================

    private static InvoiceResponse MapToResponse(Invoice factura)
    {
        return new InvoiceResponse
        {
            Id = factura.Id,
            TipoInvoice = factura.TipoInvoice,
            PuntoVenta = factura.PuntoVenta,
            NumeroInvoice = factura.NumeroInvoice,
            FechaInvoice = factura.FechaInvoice,
            CustomerId = factura.CustomerId,
            CustomerRazonSocial = factura.Customer.RazonSocial,
            CustomerCUIT = factura.Customer.CUIT,
            SalesRepId = factura.SalesRepId,
            SalesRepNombre = factura.SalesRep?.Nombre,
            Subtotal = factura.Subtotal,
            ImporteIVA = factura.ImporteIVA,
            IVAContenido = factura.IVAContenido,
            ImportePercepcionIIBB = factura.ImportePercepcionIIBB,
            ImporteDescuento = factura.ImporteDescuento,
            Total = factura.Total,
            CAE = factura.CAE,
            FechaVencimientoCAE = factura.FechaVencimientoCAE,
            Anulada = factura.Anulada,
            CantidadItems = factura.Detalles.Count
        };
    }
}
