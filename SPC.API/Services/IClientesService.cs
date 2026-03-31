using SPC.API.Contracts.Customers;

namespace SPC.API.Services;

/// <summary>
/// Service interface for Customer business operations
/// </summary>
public interface ICustomersService
{
    Task<int> CountAsync();
    Task<IEnumerable<CustomerResponse>> GetAllAsync();
    Task<IEnumerable<CustomerResponse>> GetPagedAsync(int skip, int take);
    Task<CustomerResponse?> GetByIdAsync(int id);
    Task<IEnumerable<CustomerResponse>> SearchAsync(string Name);
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request);
    Task<CustomerResponse?> UpdateAsync(int id, UpdateCustomerRequest request);
    Task<bool> DeleteAsync(int id);
}
