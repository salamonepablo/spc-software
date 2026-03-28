using SPC.Web.Services.Models;

namespace SPC.Web.Services;

/// <summary>
/// Interface for API communication service
/// </summary>
public interface IApiService
{
    // Customers
    Task<int> GetCustomersCountAsync();
    Task<List<CustomerDto>> GetCustomersAsync(int skip = 0, int take = 50);
    Task<List<CustomerDto>> GetAllCustomersAsync();
    Task<List<CustomerDto>> BuscarCustomersAsync(string Name);
    Task<List<CustomerDto>> SearchCustomersAsync(string term);
    Task<CustomerDto?> GetCustomerAsync(int id);
    Task<CustomerDto?> CreateCustomerAsync(CreateCustomerDto cliente);
    Task<bool> UpdateCustomerAsync(int id, UpdateCustomerDto cliente);
    Task<bool> DeleteCustomerAsync(int id);
    
    // Products
    Task<List<ProductDto>> GetProductsAsync();
    Task<List<ProductDto>> BuscarProductsAsync(string termino);
    Task<List<ProductDto>> SearchProductsAsync(string term);
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
    
    // Quotes (Presupuestos)
    Task<List<QuoteDto>> GetQuotesAsync(int skip = 0, int take = 50);
    Task<QuoteCompletaDto?> GetQuoteAsync(int id);
    Task<List<QuoteDto>> BuscarQuotesAsync(string termino);
    Task<List<QuoteDto>> GetQuotesByCustomerAsync(int clienteId);
    Task<List<QuoteDto>> GetQuotesByFechaAsync(DateTime desde, DateTime hasta);
    Task<QuotesResumenDto?> GetQuotesResumenAsync();
    Task<int> GetQuotesCountAsync();
    Task<QuoteCompletaDto?> CreateQuoteAsync(CreateQuoteDto presupuesto);
    Task<bool> AnularQuoteAsync(int id, string motivo);

    // Current Account (Cuenta Corriente)
    Task<CurrentAccountDto?> GetCurrentAccountAsync(int customerId);
    Task<CurrentAccountMovementsDto?> GetCurrentAccountMovementsAsync(
        int customerId,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int? line = null,
        int skip = 0,
        int take = 50);
}
