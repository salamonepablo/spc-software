using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.Customers;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Service implementation for Customer business operations
/// </summary>
public class CustomersService : ICustomersService
{
    private readonly SPCDbContext _db;

    public CustomersService(SPCDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<CustomerResponse>> GetAllAsync()
    {
        var clientes = await _db.Customers
            .Include(c => c.TaxCondition)
            .Include(c => c.SalesRep)
            .Include(c => c.SalesZone)
            .Where(c => c.Activo)
            .OrderBy(c => c.RazonSocial)
            .ToListAsync();

        return clientes.Select(MapToResponse);
    }

    public async Task<CustomerResponse?> GetByIdAsync(int id)
    {
        var cliente = await _db.Customers
            .Include(c => c.TaxCondition)
            .Include(c => c.SalesRep)
            .Include(c => c.SalesZone)
            .FirstOrDefaultAsync(c => c.Id == id);

        return cliente != null ? MapToResponse(cliente) : null;
    }

    public async Task<IEnumerable<CustomerResponse>> SearchAsync(string termino)
    {
        // Try to parse as Id (codigo de cliente)
        int.TryParse(termino, out var clienteId);
        
        var clientes = await _db.Customers
            .Include(c => c.TaxCondition)
            .Where(c => c.Activo &&
                   (c.Id == clienteId ||
                    c.RazonSocial.Contains(termino) ||
                    (c.NombreFantasia != null && c.NombreFantasia.Contains(termino))))
            .OrderBy(c => c.RazonSocial)
            .ToListAsync();

        return clientes.Select(MapToResponse);
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request)
    {
        var cliente = new Customer
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
            TaxConditionId = request.TaxConditionId,
            SalesRepId = request.SalesRepId,
            SalesZoneId = request.SalesZoneId,
            PorcentajeDescuento = request.PorcentajeDescuento,
            LimiteCredito = request.LimiteCredito,
            Observaciones = request.Observaciones,
            FechaAlta = DateTime.Now,
            Activo = true
        };

        _db.Customers.Add(cliente);
        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(cliente).Reference(c => c.TaxCondition).LoadAsync();
        await _db.Entry(cliente).Reference(c => c.SalesRep).LoadAsync();
        await _db.Entry(cliente).Reference(c => c.SalesZone).LoadAsync();

        return MapToResponse(cliente);
    }

    public async Task<CustomerResponse?> UpdateAsync(int id, UpdateCustomerRequest request)
    {
        var cliente = await _db.Customers.FindAsync(id);

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
        cliente.TaxConditionId = request.TaxConditionId;
        cliente.SalesRepId = request.SalesRepId;
        cliente.SalesZoneId = request.SalesZoneId;
        cliente.PorcentajeDescuento = request.PorcentajeDescuento;
        cliente.LimiteCredito = request.LimiteCredito;
        cliente.Observaciones = request.Observaciones;

        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(cliente).Reference(c => c.TaxCondition).LoadAsync();
        await _db.Entry(cliente).Reference(c => c.SalesRep).LoadAsync();
        await _db.Entry(cliente).Reference(c => c.SalesZone).LoadAsync();

        return MapToResponse(cliente);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var cliente = await _db.Customers.FindAsync(id);

        if (cliente == null)
            return false;

        // Soft delete
        cliente.Activo = false;
        await _db.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Maps a Customer entity to CustomerResponse DTO
    /// </summary>
    private static CustomerResponse MapToResponse(Customer cliente)
    {
        return new CustomerResponse
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
            TaxConditionId = cliente.TaxConditionId,
            TaxConditionDescripcion = cliente.TaxCondition?.Descripcion,
            TaxConditionCodigo = cliente.TaxCondition?.Codigo,
            TipoInvoice = cliente.TaxCondition?.TipoInvoice,
            AlicuotaIIBB = cliente.AlicuotaIIBB,
            ProvinciaPadronIIBB = cliente.ProvinciaPadronIIBB,
            SalesRepId = cliente.SalesRepId,
            SalesRepNombre = cliente.SalesRep?.Nombre,
            SalesZoneId = cliente.SalesZoneId,
            SalesZoneNombre = cliente.SalesZone?.Nombre
        };
    }
}
