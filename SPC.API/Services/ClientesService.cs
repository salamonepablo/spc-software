using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.Clientes;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Service implementation for Cliente business operations
/// </summary>
public class ClientesService : IClientesService
{
    private readonly SPCDbContext _db;

    public ClientesService(SPCDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ClienteResponse>> GetAllAsync()
    {
        var clientes = await _db.Clientes
            .Include(c => c.CondicionIva)
            .Include(c => c.Vendedor)
            .Include(c => c.ZonaVenta)
            .Where(c => c.Activo)
            .OrderBy(c => c.RazonSocial)
            .ToListAsync();

        return clientes.Select(MapToResponse);
    }

    public async Task<ClienteResponse?> GetByIdAsync(int id)
    {
        var cliente = await _db.Clientes
            .Include(c => c.CondicionIva)
            .Include(c => c.Vendedor)
            .Include(c => c.ZonaVenta)
            .FirstOrDefaultAsync(c => c.Id == id);

        return cliente != null ? MapToResponse(cliente) : null;
    }

    public async Task<IEnumerable<ClienteResponse>> SearchAsync(string termino)
    {
        // Try to parse as Id (codigo de cliente)
        int.TryParse(termino, out var clienteId);
        
        var clientes = await _db.Clientes
            .Include(c => c.CondicionIva)
            .Where(c => c.Activo &&
                   (c.Id == clienteId ||
                    c.RazonSocial.Contains(termino) ||
                    (c.NombreFantasia != null && c.NombreFantasia.Contains(termino))))
            .OrderBy(c => c.RazonSocial)
            .ToListAsync();

        return clientes.Select(MapToResponse);
    }

    public async Task<ClienteResponse> CreateAsync(CreateClienteRequest request)
    {
        var cliente = new Cliente
        {
            RazonSocial = request.RazonSocial,
            NombreFantasia = request.NombreFantasia,
            CUIT = request.CUIT,
            Direccion = request.Direccion,
            Localidad = request.Localidad,
            Provincia = request.Provincia,
            CodigoPostal = request.CodigoPostal,
            Telefono = request.Telefono,
            Celular = request.Celular,
            Email = request.Email,
            CondicionIvaId = request.CondicionIvaId,
            VendedorId = request.VendedorId,
            ZonaVentaId = request.ZonaVentaId,
            PorcentajeDescuento = request.PorcentajeDescuento,
            LimiteCredito = request.LimiteCredito,
            Observaciones = request.Observaciones,
            FechaAlta = DateTime.Now,
            Activo = true
        };

        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(cliente).Reference(c => c.CondicionIva).LoadAsync();
        await _db.Entry(cliente).Reference(c => c.Vendedor).LoadAsync();
        await _db.Entry(cliente).Reference(c => c.ZonaVenta).LoadAsync();

        return MapToResponse(cliente);
    }

    public async Task<ClienteResponse?> UpdateAsync(int id, UpdateClienteRequest request)
    {
        var cliente = await _db.Clientes.FindAsync(id);

        if (cliente == null)
            return null;

        // Update properties
        cliente.RazonSocial = request.RazonSocial;
        cliente.NombreFantasia = request.NombreFantasia;
        cliente.CUIT = request.CUIT;
        cliente.Direccion = request.Direccion;
        cliente.Localidad = request.Localidad;
        cliente.Provincia = request.Provincia;
        cliente.CodigoPostal = request.CodigoPostal;
        cliente.Telefono = request.Telefono;
        cliente.Celular = request.Celular;
        cliente.Email = request.Email;
        cliente.CondicionIvaId = request.CondicionIvaId;
        cliente.VendedorId = request.VendedorId;
        cliente.ZonaVentaId = request.ZonaVentaId;
        cliente.PorcentajeDescuento = request.PorcentajeDescuento;
        cliente.LimiteCredito = request.LimiteCredito;
        cliente.Observaciones = request.Observaciones;

        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(cliente).Reference(c => c.CondicionIva).LoadAsync();
        await _db.Entry(cliente).Reference(c => c.Vendedor).LoadAsync();
        await _db.Entry(cliente).Reference(c => c.ZonaVenta).LoadAsync();

        return MapToResponse(cliente);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var cliente = await _db.Clientes.FindAsync(id);

        if (cliente == null)
            return false;

        // Soft delete
        cliente.Activo = false;
        await _db.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Maps a Cliente entity to ClienteResponse DTO
    /// </summary>
    private static ClienteResponse MapToResponse(Cliente cliente)
    {
        return new ClienteResponse
        {
            Id = cliente.Id,
            RazonSocial = cliente.RazonSocial,
            NombreFantasia = cliente.NombreFantasia,
            CUIT = cliente.CUIT,
            Direccion = cliente.Direccion,
            Localidad = cliente.Localidad,
            Provincia = cliente.Provincia,
            CodigoPostal = cliente.CodigoPostal,
            Telefono = cliente.Telefono,
            Celular = cliente.Celular,
            Email = cliente.Email,
            PorcentajeDescuento = cliente.PorcentajeDescuento,
            LimiteCredito = cliente.LimiteCredito,
            Observaciones = cliente.Observaciones,
            Activo = cliente.Activo,
            FechaAlta = cliente.FechaAlta,
            CondicionIvaId = cliente.CondicionIvaId,
            CondicionIvaDescripcion = cliente.CondicionIva?.Descripcion,
            TipoFactura = cliente.CondicionIva?.TipoFactura,
            VendedorId = cliente.VendedorId,
            VendedorNombre = cliente.Vendedor?.Nombre,
            ZonaVentaId = cliente.ZonaVentaId,
            ZonaVentaNombre = cliente.ZonaVenta?.Nombre
        };
    }
}
