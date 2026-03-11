using SPC.Web.Services.Models;

namespace SPC.Web.Services;

/// <summary>
/// Interface for API communication service
/// </summary>
public interface IApiService
{
    // Customers
    Task<List<CustomerDto>> GetCustomersAsync();
    Task<List<CustomerDto>> BuscarCustomersAsync(string nombre);
    Task<CustomerDto?> GetCustomerAsync(int id);
    Task<CustomerDto?> CreateCustomerAsync(CreateCustomerDto cliente);
    Task<bool> UpdateCustomerAsync(int id, UpdateCustomerDto cliente);
    Task<bool> DeleteCustomerAsync(int id);
    
    // Products
    Task<List<ProductDto>> GetProductsAsync();
    Task<List<ProductDto>> BuscarProductsAsync(string termino);
    Task<ProductDto?> GetProductAsync(int id);
    Task<ProductDto?> CreateProductAsync(CreateProductDto producto);
    Task<bool> UpdateProductAsync(int id, UpdateProductDto producto);
    Task<bool> DeleteProductAsync(int id);
    
    // Auxiliary data for dropdowns
    Task<List<TaxConditionDto>> GetCondicionesIvaAsync();
    Task<List<SalesRepDto>> GetSalesRepesAsync();
    Task<List<SalesZoneDto>> GetZonasVentaAsync();
    Task<List<CategoryDto>> GetCategorysAsync();
    Task<List<UnitOfMeasureDto>> GetUnidadesMedidaAsync();
    Task<List<WarehouseDto>> GetWarehousesAsync();
    
    // Stock
    Task<List<StockResumenDto>> GetStockResumenAsync();
    Task<List<StockResumenDto>> BuscarStockAsync(string termino);
    Task<List<StockResumenDto>> GetStockBajoMinimoAsync();
    Task<List<StockDetalleDto>> GetStockByProductAsync(int productoId);
    
    // Invoices
    Task<List<InvoiceDto>> GetInvoicesAsync(int skip = 0, int take = 50);
    Task<InvoiceCompletaDto?> GetInvoiceAsync(int id);
    Task<List<InvoiceDto>> BuscarInvoicesAsync(string termino);
    Task<List<InvoiceDto>> GetInvoicesByCustomerAsync(int clienteId);
    Task<List<InvoiceDto>> GetInvoicesByFechaAsync(DateTime desde, DateTime hasta);
    Task<InvoicecionResumenDto?> GetInvoicecionResumenAsync();
    Task<int> GetInvoicesCountAsync();
    Task<InvoiceCompletaDto?> CreateInvoiceAsync(CreateInvoiceDto factura);
    
    // Branches
    Task<List<SucursalDto>> GetBranchesAsync();
}
