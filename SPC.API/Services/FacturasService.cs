using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.Facturas;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Facturas service implementation for invoice operations.
/// Handles different calculation rules for Factura A vs Factura B.
/// </summary>
public class FacturasService : IFacturasService
{
    private readonly SPCDbContext _db;
    private readonly ITaxConfigurationService _taxService;
    private readonly IPricingService _pricingService;
    private readonly ICompanySettingsService _companySettings;

    public FacturasService(
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

    public async Task<IEnumerable<FacturaResponse>> GetAllAsync(int skip = 0, int take = 50)
    {
        var facturas = await _db.Facturas
            .Include(f => f.Cliente)
            .Include(f => f.Vendedor)
            .Include(f => f.Detalles)
            .OrderByDescending(f => f.FechaFactura)
            .ThenByDescending(f => f.NumeroFactura)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return facturas.Select(MapToResponse);
    }

    public async Task<FacturaCompletaResponse?> GetByIdAsync(int id)
    {
        var factura = await _db.Facturas
            .Include(f => f.Cliente)
            .Include(f => f.Vendedor)
            .Include(f => f.Detalles)
                .ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (factura == null) return null;

        return new FacturaCompletaResponse
        {
            Id = factura.Id,
            TipoFactura = factura.TipoFactura,
            PuntoVenta = factura.PuntoVenta,
            NumeroFactura = factura.NumeroFactura,
            FechaFactura = factura.FechaFactura,
            ClienteId = factura.ClienteId,
            ClienteRazonSocial = factura.Cliente.RazonSocial,
            ClienteCUIT = factura.Cliente.CUIT,
            VendedorId = factura.VendedorId,
            VendedorNombre = factura.Vendedor?.Nombre,
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
            Detalles = factura.Detalles.Select(d => new FacturaDetalleResponse
            {
                Id = d.Id,
                ItemNumero = d.ItemNumero,
                ProductoId = d.ProductoId,
                ProductoCodigo = d.Producto.Codigo,
                ProductoDescripcion = d.Producto.Descripcion,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                PorcentajeDescuento = d.PorcentajeDescuento,
                PorcentajeIVA = d.PorcentajeIVA,
                Subtotal = d.Subtotal
            }).OrderBy(d => d.ItemNumero).ToList()
        };
    }

    public async Task<IEnumerable<FacturaResponse>> GetByClienteAsync(int clienteId)
    {
        var facturas = await _db.Facturas
            .Include(f => f.Cliente)
            .Include(f => f.Vendedor)
            .Include(f => f.Detalles)
            .Where(f => f.ClienteId == clienteId)
            .OrderByDescending(f => f.FechaFactura)
            .ThenByDescending(f => f.NumeroFactura)
            .ToListAsync();

        return facturas.Select(MapToResponse);
    }

    public async Task<IEnumerable<FacturaResponse>> GetByFechaAsync(DateTime desde, DateTime hasta)
    {
        var facturas = await _db.Facturas
            .Include(f => f.Cliente)
            .Include(f => f.Vendedor)
            .Include(f => f.Detalles)
            .Where(f => f.FechaFactura >= desde && f.FechaFactura <= hasta)
            .OrderByDescending(f => f.FechaFactura)
            .ThenByDescending(f => f.NumeroFactura)
            .ToListAsync();

        return facturas.Select(MapToResponse);
    }

    public async Task<IEnumerable<FacturaResponse>> SearchAsync(string termino)
    {
        // Try to parse as number for invoice search
        long.TryParse(termino.Replace("-", ""), out var numeroFactura);

        var facturas = await _db.Facturas
            .Include(f => f.Cliente)
            .Include(f => f.Vendedor)
            .Include(f => f.Detalles)
            .Where(f => f.NumeroFactura == numeroFactura ||
                       f.Cliente.RazonSocial.Contains(termino) ||
                       (f.Cliente.CUIT != null && f.Cliente.CUIT.Contains(termino)))
            .OrderByDescending(f => f.FechaFactura)
            .Take(100)
            .ToListAsync();

        return facturas.Select(MapToResponse);
    }

    public async Task<FacturacionResumenResponse> GetResumenAsync()
    {
        var hoy = DateTime.Today;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
        var inicioAnio = new DateTime(hoy.Year, 1, 1);

        var facturasHoy = await _db.Facturas
            .Where(f => f.FechaFactura.Date == hoy && !f.Anulada)
            .ToListAsync();

        var facturasMes = await _db.Facturas
            .Where(f => f.FechaFactura >= inicioMes && !f.Anulada)
            .ToListAsync();

        var facturasAnio = await _db.Facturas
            .Where(f => f.FechaFactura >= inicioAnio && !f.Anulada)
            .ToListAsync();

        var totalFacturas = await _db.Facturas.CountAsync();

        return new FacturacionResumenResponse
        {
            TotalFacturas = totalFacturas,
            FacturasHoy = facturasHoy.Count,
            FacturasMes = facturasMes.Count,
            MontoHoy = facturasHoy.Sum(f => f.Total),
            MontoMes = facturasMes.Sum(f => f.Total),
            MontoAnio = facturasAnio.Sum(f => f.Total)
        };
    }

    public async Task<int> GetCountAsync()
    {
        return await _db.Facturas.CountAsync();
    }

    // ===========================================
    // COMMANDS
    // ===========================================

    public async Task<FacturaCompletaResponse> CreateAsync(CreateFacturaRequest request)
    {
        // 1. Validate and load customer
        var cliente = await _db.Clientes
            .Include(c => c.CondicionIva)
            .FirstOrDefaultAsync(c => c.Id == request.ClienteId)
            ?? throw new InvalidOperationException($"Cliente {request.ClienteId} no encontrado");

        // 2. Validate and load branch
        var branch = await _db.Branches.FindAsync(request.BranchId)
            ?? throw new InvalidOperationException($"Sucursal {request.BranchId} no encontrada");

        // 3. Load all products for validation
        var productIds = request.Detalles.Select(d => d.ProductoId).Distinct().ToList();
        var productos = await _db.Productos
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var detalle in request.Detalles)
        {
            if (!productos.ContainsKey(detalle.ProductoId))
                throw new InvalidOperationException($"Producto {detalle.ProductoId} no encontrado");
        }

        // 4. Get VAT rate from configuration (not hardcoded!)
        var vatRate = await _taxService.GetDefaultVATRateAsync();

        // 5. Resolve document discount (use customer default if not specified)
        var documentDiscount = _pricingService.ResolveDiscount(
            request.PorcentajeDescuento,
            cliente.PorcentajeDescuento);

        // 6. Determine if this is Factura A or B
        bool isFacturaA = request.TipoFactura.ToUpperInvariant() == "A";

        // 7. Calculate line items
        // Factura A: uses PrecioFactura (net price, VAT will be added)
        // Factura B: uses PrecioPresupuesto (final price with VAT included)
        var lineResults = new List<(CreateFacturaDetalleRequest detalle, LineCalculationResult calc, Producto producto)>();
        foreach (var detalle in request.Detalles)
        {
            var producto = productos[detalle.ProductoId];
            
            // Select price based on invoice type
            decimal unitPrice;
            if (detalle.PrecioUnitario.HasValue)
            {
                unitPrice = detalle.PrecioUnitario.Value;
            }
            else
            {
                unitPrice = isFacturaA ? producto.PrecioFactura : producto.PrecioPresupuesto;
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
        Factura factura;
        var nextNumber = await GetNextInvoiceNumberAsync(request.TipoFactura, branch.PointOfSale);

        if (isFacturaA)
        {
            // Factura A: Net prices + VAT discriminated + IIBB if agent
            var docCalc = _pricingService.CalculateDocumentTypeA(
                lineResults.Select(l => l.calc),
                documentDiscount,
                vatRate,
                customerIIBBRate,
                isIIBBAgent);

            factura = new Factura
            {
                BranchId = request.BranchId,
                TipoFactura = "A",
                PuntoVenta = branch.PointOfSale,
                NumeroFactura = nextNumber,
                FechaFactura = DateTime.Now,
                ClienteId = request.ClienteId,
                VendedorId = request.VendedorId,
                
                PorcentajeIVA = vatRate,
                Subtotal = docCalc.NetSubtotal,
                ImporteIVA = docCalc.VATAmount,
                IVAContenido = 0, // Factura A: IVA discriminado, no contenido
                
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
            // Factura B: Final prices with VAT included, show IVA Contenido
            var docCalc = _pricingService.CalculateDocumentTypeB(
                lineResults.Select(l => l.calc),
                documentDiscount,
                vatRate);

            factura = new Factura
            {
                BranchId = request.BranchId,
                TipoFactura = "B",
                PuntoVenta = branch.PointOfSale,
                NumeroFactura = nextNumber,
                FechaFactura = DateTime.Now,
                ClienteId = request.ClienteId,
                VendedorId = request.VendedorId,
                
                PorcentajeIVA = vatRate,
                Subtotal = docCalc.Total, // In Factura B, Subtotal = Total
                ImporteIVA = 0, // Factura B: IVA not discriminated
                IVAContenido = docCalc.VATContained, // Ley 27.743
                
                AlicuotaIIBB = 0, // Factura B typically no IIBB perception
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
            factura.Detalles.Add(new FacturaDetalle
            {
                ItemNumero = itemNumber++,
                ProductoId = detalle.ProductoId,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = calc.UnitPrice,
                PorcentajeDescuento = calc.DiscountPercent,
                PorcentajeIVA = calc.VATPercent,
                Subtotal = calc.Subtotal
            });
        }

        // 11. Save to database
        _db.Facturas.Add(factura);
        await _db.SaveChangesAsync();

        // 12. Return complete response
        return (await GetByIdAsync(factura.Id))!;
    }

    public async Task<bool> AnularAsync(int id, string motivo)
    {
        var factura = await _db.Facturas.FindAsync(id);
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
    private async Task<long> GetNextInvoiceNumberAsync(string tipoFactura, int puntoVenta)
    {
        var lastNumber = await _db.Facturas
            .Where(f => f.TipoFactura == tipoFactura && f.PuntoVenta == puntoVenta)
            .MaxAsync(f => (long?)f.NumeroFactura) ?? 0;

        return lastNumber + 1;
    }

    // ===========================================
    // MAPPING
    // ===========================================

    private static FacturaResponse MapToResponse(Factura factura)
    {
        return new FacturaResponse
        {
            Id = factura.Id,
            TipoFactura = factura.TipoFactura,
            PuntoVenta = factura.PuntoVenta,
            NumeroFactura = factura.NumeroFactura,
            FechaFactura = factura.FechaFactura,
            ClienteId = factura.ClienteId,
            ClienteRazonSocial = factura.Cliente.RazonSocial,
            ClienteCUIT = factura.Cliente.CUIT,
            VendedorId = factura.VendedorId,
            VendedorNombre = factura.Vendedor?.Nombre,
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
