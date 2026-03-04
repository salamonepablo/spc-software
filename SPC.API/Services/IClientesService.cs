using SPC.API.Contracts.Clientes;

namespace SPC.API.Services;

/// <summary>
/// Service interface for Cliente business operations
/// </summary>
public interface IClientesService
{
    Task<IEnumerable<ClienteResponse>> GetAllAsync();
    Task<ClienteResponse?> GetByIdAsync(int id);
    Task<IEnumerable<ClienteResponse>> SearchAsync(string nombre);
    Task<ClienteResponse> CreateAsync(CreateClienteRequest request);
    Task<ClienteResponse?> UpdateAsync(int id, UpdateClienteRequest request);
    Task<bool> DeleteAsync(int id);
}
