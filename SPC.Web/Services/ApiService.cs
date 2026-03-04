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

    #region Clientes

    public async Task<List<ClienteDto>> GetClientesAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<ClienteDto>>("/api/clientes");
            return result ?? new List<ClienteDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching clientes");
            return new List<ClienteDto>();
        }
    }

    public async Task<List<ClienteDto>> BuscarClientesAsync(string nombre)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<ClienteDto>>($"/api/clientes/buscar?nombre={Uri.EscapeDataString(nombre)}");
            return result ?? new List<ClienteDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching clientes with term: {Nombre}", nombre);
            return new List<ClienteDto>();
        }
    }

    public async Task<ClienteDto?> GetClienteAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<ClienteDto>($"/api/clientes/{id}");
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

    public async Task<ClienteDto?> CreateClienteAsync(CreateClienteDto cliente)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/clientes", cliente);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ClienteDto>();
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

    public async Task<bool> UpdateClienteAsync(int id, UpdateClienteDto cliente)
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

    public async Task<bool> DeleteClienteAsync(int id)
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

    #region Productos

    public async Task<List<ProductoDto>> GetProductosAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<ProductoDto>>("/api/productos");
            return result ?? new List<ProductoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching productos");
            return new List<ProductoDto>();
        }
    }

    public async Task<List<ProductoDto>> BuscarProductosAsync(string termino)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<ProductoDto>>($"/api/productos/buscar?q={Uri.EscapeDataString(termino)}");
            return result ?? new List<ProductoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching productos with term: {Termino}", termino);
            return new List<ProductoDto>();
        }
    }

    public async Task<ProductoDto?> GetProductoAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<ProductoDto>($"/api/productos/{id}");
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

    public async Task<ProductoDto?> CreateProductoAsync(CreateProductoDto producto)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/productos", producto);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProductoDto>();
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

    public async Task<bool> UpdateProductoAsync(int id, UpdateProductoDto producto)
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

    public async Task<bool> DeleteProductoAsync(int id)
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

    public async Task<List<CondicionIvaDto>> GetCondicionesIvaAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<CondicionIvaDto>>("/api/condicionesiva");
            return result ?? new List<CondicionIvaDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching condiciones IVA");
            return new List<CondicionIvaDto>();
        }
    }

    public async Task<List<VendedorDto>> GetVendedoresAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<VendedorDto>>("/api/vendedores");
            return result ?? new List<VendedorDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching vendedores");
            return new List<VendedorDto>();
        }
    }

    public async Task<List<ZonaVentaDto>> GetZonasVentaAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<ZonaVentaDto>>("/api/zonasventas");
            return result ?? new List<ZonaVentaDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching zonas venta");
            return new List<ZonaVentaDto>();
        }
    }

    public async Task<List<RubroDto>> GetRubrosAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<RubroDto>>("/api/rubros");
            return result ?? new List<RubroDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching rubros");
            return new List<RubroDto>();
        }
    }

    public async Task<List<UnidadMedidaDto>> GetUnidadesMedidaAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<UnidadMedidaDto>>("/api/unidadesmedida");
            return result ?? new List<UnidadMedidaDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching unidades medida");
            return new List<UnidadMedidaDto>();
        }
    }

    #endregion
}
