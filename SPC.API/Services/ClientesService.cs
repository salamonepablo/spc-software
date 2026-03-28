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

    public async Task<int> CountAsync()
    {
        return await _db.Customers.CountAsync(c => c.IsActive);
    }

    public async Task<IEnumerable<CustomerResponse>> GetAllAsync()
    {
        var clientes = await _db.Customers
            .Include(c => c.TaxCondition)
            .Include(c => c.SalesRep)
            .Include(c => c.SalesZone)
            .Where(c => c.IsActive)
            .OrderBy(c => c.CompanyName)
            .ToListAsync();

        return clientes.Select(MapToResponse);
    }

    public async Task<IEnumerable<CustomerResponse>> GetPagedAsync(int skip, int take)
    {
        var clientes = await _db.Customers
            .Include(c => c.TaxCondition)
            .Include(c => c.SalesRep)
            .Include(c => c.SalesZone)
            .Where(c => c.IsActive)
            .OrderBy(c => c.CompanyName)
            .Skip(skip)
            .Take(take)
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
        // Try to parse as Id (Code de cliente)
        int.TryParse(termino, out var clienteId);
        
        var clientes = await _db.Customers
            .Include(c => c.TaxCondition)
            .Where(c => c.IsActive &&
                   (c.Id == clienteId ||
                    c.CompanyName.Contains(termino) ||
                    (c.TradeName != null && c.TradeName.Contains(termino))))
            .OrderBy(c => c.CompanyName)
            .Take(20)
            .ToListAsync();

        return clientes.Select(MapToResponse);
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request)
    {
        var cliente = new Customer
        {
            CompanyName = request.CompanyName,
            TradeName = request.TradeName,
            CUIT = request.CUIT,
            Address = request.Address,
            City = request.City,
            Province = request.Province,
            PostalCode = request.PostalCode,
            Phone = request.Phone,
            Mobile = request.Mobile,
            Email = request.Email,
            TaxConditionId = request.TaxConditionId,
            SalesRepId = request.SalesRepId,
            SalesZoneId = request.SalesZoneId,
            DiscountPercent = request.DiscountPercent,
            CreditLimit = request.CreditLimit,
            Notes = request.Notes,
            CreatedDate = DateTime.Now,
            IsActive = true
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
        cliente.CompanyName = request.CompanyName;
        cliente.TradeName = request.TradeName;
        cliente.CUIT = request.CUIT;
        cliente.Address = request.Address;
        cliente.City = request.City;
        cliente.Province = request.Province;
        cliente.PostalCode = request.PostalCode;
        cliente.Phone = request.Phone;
        cliente.Mobile = request.Mobile;
        cliente.Email = request.Email;
        cliente.TaxConditionId = request.TaxConditionId;
        cliente.SalesRepId = request.SalesRepId;
        cliente.SalesZoneId = request.SalesZoneId;
        cliente.DiscountPercent = request.DiscountPercent;
        cliente.CreditLimit = request.CreditLimit;
        cliente.Notes = request.Notes;

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
        cliente.IsActive = false;
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
            CompanyName = cliente.CompanyName,
            TradeName = cliente.TradeName,
            CUIT = cliente.CUIT,
            Address = cliente.Address,
            City = cliente.City,
            Province = cliente.Province,
            PostalCode = cliente.PostalCode,
            Phone = cliente.Phone,
            Mobile = cliente.Mobile,
            Email = cliente.Email,
            DiscountPercent = cliente.DiscountPercent,
            CreditLimit = cliente.CreditLimit,
            Notes = cliente.Notes,
            IsActive = cliente.IsActive,
            CreatedDate = cliente.CreatedDate,
            TaxConditionId = cliente.TaxConditionId,
            TaxConditionDescription = cliente.TaxCondition?.Description,
            TaxConditionCode = cliente.TaxCondition?.Code,
            InvoiceType = cliente.TaxCondition?.InvoiceType,
            IIBBPercent = cliente.IIBBPercent,
            IIBBRegistryProvince = cliente.IIBBRegistryProvince,
            SalesRepId = cliente.SalesRepId,
            SalesRepFirstName = cliente.SalesRep?.FirstName,
            SalesZoneId = cliente.SalesZoneId,
            SalesZoneName = cliente.SalesZone?.Name
        };
    }
}
