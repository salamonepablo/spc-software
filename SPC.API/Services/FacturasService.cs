using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.Facturas;
using SPC.API.Data;

namespace SPC.API.Services;

/// <summary>
/// Facturas service implementation for invoice queries
/// </summary>
public class FacturasService : IFacturasService
{
    private readonly SPCDbContext _db;

    public FacturasService(SPCDbContext db)
    {
        _db = db;
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

    private static FacturaResponse MapToResponse(SPC.Shared.Models.Factura factura)
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
