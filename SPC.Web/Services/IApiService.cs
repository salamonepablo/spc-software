using SPC.Web.Services.Models;

namespace SPC.Web.Services;

/// <summary>
/// Interface for API communication service
/// </summary>
public interface IApiService
{
    // Clientes
    Task<List<ClienteDto>> GetClientesAsync();
    Task<List<ClienteDto>> BuscarClientesAsync(string nombre);
    Task<ClienteDto?> GetClienteAsync(int id);
    Task<ClienteDto?> CreateClienteAsync(CreateClienteDto cliente);
    Task<bool> UpdateClienteAsync(int id, UpdateClienteDto cliente);
    Task<bool> DeleteClienteAsync(int id);
    
    // Auxiliary data for dropdowns
    Task<List<CondicionIvaDto>> GetCondicionesIvaAsync();
    Task<List<VendedorDto>> GetVendedoresAsync();
    Task<List<ZonaVentaDto>> GetZonasVentaAsync();
}
