using SPC.API.Contracts.Customers;

namespace SPC.API.Services;

/// <summary>
/// Service interface for Customer business operations
/// </summary>
public interface ICustomersService
{
    Task<IEnumerable<CustomerResponse>> GetAllAsync();
    Task<CustomerResponse?> GetByIdAsync(int id);
    Task<IEnumerable<CustomerResponse>> SearchAsync(string nombre);
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request);
    Task<CustomerResponse?> UpdateAsync(int id, UpdateCustomerRequest request);
    Task<bool> DeleteAsync(int id);
}
