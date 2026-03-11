using System.Net.Http.Json;
using SPC.Web.Services.Models;

namespace SPC.Web.Services;

/// <summary>
/// Service for communicating with SPC.API
/// </summary>
public class ApiService : IApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<ApiService> _logger;

    public ApiService(HttpClient http, ILogger<ApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    #region Customers

    public async Task<List<CustomerDto>> GetCustomersAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<CustomerDto>>("/api/clientes");
            return result ?? new List<CustomerDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching clientes");
            return new List<CustomerDto>();
        }
    }

    public async Task<List<CustomerDto>> BuscarCustomersAsync(string nombre)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<CustomerDto>>($"/api/clientes/buscar?nombre={Uri.EscapeDataString(nombre)}");
            return result ?? new List<CustomerDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching clientes with term: {Nombre}", nombre);
            return new List<CustomerDto>();
        }
    }

    public async Task<CustomerDto?> GetCustomerAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<CustomerDto>($"/api/clientes/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cliente {Id}", id);
            return null;
        }
    }

    public async Task<CustomerDto?> CreateCustomerAsync(CreateCustomerDto cliente)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/clientes", cliente);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CustomerDto>();
            }
            
            _logger.LogWarning("Failed to create cliente. Status: {Status}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cliente");
            return null;
        }
    }

    public async Task<bool> UpdateCustomerAsync(int id, UpdateCustomerDto cliente)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"/api/clientes/{id}", cliente);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cliente {Id}", id);
            return false;
        }
    }

    public async Task<bool> DeleteCustomerAsync(int id)
    {
        try
        {
            var response = await _http.DeleteAsync($"/api/clientes/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cliente {Id}", id);
            return false;
        }
    }

    #endregion

    #region Products

    public async Task<List<ProductDto>> GetProductsAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<ProductDto>>("/api/productos");
            return result ?? new List<ProductDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching productos");
            return new List<ProductDto>();
        }
    }

    public async Task<List<ProductDto>> BuscarProductsAsync(string termino)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<ProductDto>>($"/api/productos/buscar?descripcion={Uri.EscapeDataString(termino)}");
            return result ?? new List<ProductDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching productos with term: {Termino}", termino);
            return new List<ProductDto>();
        }
    }

    public async Task<ProductDto?> GetProductAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<ProductDto>($"/api/productos/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching producto {Id}", id);
            return null;
        }
    }

    public async Task<ProductDto?> CreateProductAsync(CreateProductDto producto)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/productos", producto);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProductDto>();
            }
            
            _logger.LogWarning("Failed to create producto. Status: {Status}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating producto");
            return null;
        }
    }

    public async Task<bool> UpdateProductAsync(int id, UpdateProductDto producto)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"/api/productos/{id}", producto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating producto {Id}", id);
            return false;
        }
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        try
        {
            var response = await _http.DeleteAsync($"/api/productos/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting producto {Id}", id);
            return false;
        }
    }

    #endregion

    #region Auxiliary Data

    public async Task<List<TaxConditionDto>> GetCondicionesIvaAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<TaxConditionDto>>("/api/condicionesiva");
            return result ?? new List<TaxConditionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching condiciones IVA");
            return new List<TaxConditionDto>();
        }
    }

    public async Task<List<SalesRepDto>> GetSalesRepesAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<SalesRepDto>>("/api/vendedores");
            return result ?? new List<SalesRepDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching vendedores");
            return new List<SalesRepDto>();
        }
    }

    public async Task<List<SalesZoneDto>> GetZonasVentaAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<SalesZoneDto>>("/api/zonasventas");
            return result ?? new List<SalesZoneDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching zonas venta");
            return new List<SalesZoneDto>();
        }
    }

    public async Task<List<CategoryDto>> GetCategorysAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<CategoryDto>>("/api/rubros");
            return result ?? new List<CategoryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching rubros");
            return new List<CategoryDto>();
        }
    }

    public async Task<List<UnitOfMeasureDto>> GetUnidadesMedidaAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<UnitOfMeasureDto>>("/api/unidadesmedida");
            return result ?? new List<UnitOfMeasureDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching unidades medida");
            return new List<UnitOfMeasureDto>();
        }
    }

    public async Task<List<WarehouseDto>> GetWarehousesAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<WarehouseDto>>("/api/depositos");
            return result ?? new List<WarehouseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching depositos");
            return new List<WarehouseDto>();
        }
    }

    #endregion

    #region Stock

    public async Task<List<StockResumenDto>> GetStockResumenAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<StockResumenDto>>("/api/stock/resumen");
            return result ?? new List<StockResumenDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching stock resumen");
            return new List<StockResumenDto>();
        }
    }

    public async Task<List<StockResumenDto>> BuscarStockAsync(string termino)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<StockResumenDto>>($"/api/stock/buscar?termino={Uri.EscapeDataString(termino)}");
            return result ?? new List<StockResumenDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching stock with term: {Termino}", termino);
            return new List<StockResumenDto>();
        }
    }

    public async Task<List<StockResumenDto>> GetStockBajoMinimoAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<StockResumenDto>>("/api/stock/bajominimo");
            return result ?? new List<StockResumenDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching stock bajo minimo");
            return new List<StockResumenDto>();
        }
    }

    public async Task<List<StockDetalleDto>> GetStockByProductAsync(int productoId)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<StockDetalleDto>>($"/api/stock/producto/{productoId}");
            return result ?? new List<StockDetalleDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching stock for producto {Id}", productoId);
            return new List<StockDetalleDto>();
        }
    }

    #endregion

    #region Invoices

    public async Task<List<InvoiceDto>> GetInvoicesAsync(int skip = 0, int take = 50)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<InvoiceDto>>($"/api/facturas?skip={skip}&take={take}");
            return result ?? new List<InvoiceDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching facturas");
            return new List<InvoiceDto>();
        }
    }

    public async Task<InvoiceCompletaDto?> GetInvoiceAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<InvoiceCompletaDto>($"/api/facturas/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching factura {Id}", id);
            return null;
        }
    }

    public async Task<List<InvoiceDto>> BuscarInvoicesAsync(string termino)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<InvoiceDto>>($"/api/facturas/buscar?termino={Uri.EscapeDataString(termino)}");
            return result ?? new List<InvoiceDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching facturas with term: {Termino}", termino);
            return new List<InvoiceDto>();
        }
    }

    public async Task<List<InvoiceDto>> GetInvoicesByCustomerAsync(int clienteId)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<InvoiceDto>>($"/api/facturas/cliente/{clienteId}");
            return result ?? new List<InvoiceDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching facturas for cliente {Id}", clienteId);
            return new List<InvoiceDto>();
        }
    }

    public async Task<List<InvoiceDto>> GetInvoicesByFechaAsync(DateTime desde, DateTime hasta)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<InvoiceDto>>($"/api/facturas/fecha?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}");
            return result ?? new List<InvoiceDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching facturas by date range");
            return new List<InvoiceDto>();
        }
    }

    public async Task<InvoicecionResumenDto?> GetInvoicecionResumenAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<InvoicecionResumenDto>("/api/facturas/resumen");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching facturacion resumen");
            return null;
        }
    }

    public async Task<int> GetInvoicesCountAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<CountResponse>("/api/facturas/count");
            return result?.Total ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching facturas count");
            return 0;
        }
    }

    private class CountResponse
    {
        public int Total { get; set; }
    }

    public async Task<InvoiceCompletaDto?> CreateInvoiceAsync(CreateInvoiceDto factura)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/facturas", factura);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<InvoiceCompletaDto>();
            }
            
            _logger.LogWarning("Failed to create factura. Status: {Status}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating factura");
            return null;
        }
    }

    public async Task<List<SucursalDto>> GetBranchesAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<SucursalDto>>("/api/sucursales");
            return result ?? new List<SucursalDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sucursales");
            return new List<SucursalDto>();
        }
    }

    #endregion
}
