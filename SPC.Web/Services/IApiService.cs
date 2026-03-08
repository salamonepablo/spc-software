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
    
    // Productos
    Task<List<ProductoDto>> GetProductosAsync();
    Task<List<ProductoDto>> BuscarProductosAsync(string termino);
    Task<ProductoDto?> GetProductoAsync(int id);
    Task<ProductoDto?> CreateProductoAsync(CreateProductoDto producto);
    Task<bool> UpdateProductoAsync(int id, UpdateProductoDto producto);
    Task<bool> DeleteProductoAsync(int id);
    
    // Auxiliary data for dropdowns
    Task<List<CondicionIvaDto>> GetCondicionesIvaAsync();
    Task<List<VendedorDto>> GetVendedoresAsync();
    Task<List<ZonaVentaDto>> GetZonasVentaAsync();
    Task<List<RubroDto>> GetRubrosAsync();
    Task<List<UnidadMedidaDto>> GetUnidadesMedidaAsync();
    Task<List<DepositoDto>> GetDepositosAsync();
    
    // Stock
    Task<List<StockResumenDto>> GetStockResumenAsync();
    Task<List<StockResumenDto>> BuscarStockAsync(string termino);
    Task<List<StockResumenDto>> GetStockBajoMinimoAsync();
    Task<List<StockDetalleDto>> GetStockByProductoAsync(int productoId);
    
    // Facturas
    Task<List<FacturaDto>> GetFacturasAsync(int skip = 0, int take = 50);
    Task<FacturaCompletaDto?> GetFacturaAsync(int id);
    Task<List<FacturaDto>> BuscarFacturasAsync(string termino);
    Task<List<FacturaDto>> GetFacturasByClienteAsync(int clienteId);
    Task<List<FacturaDto>> GetFacturasByFechaAsync(DateTime desde, DateTime hasta);
    Task<FacturacionResumenDto?> GetFacturacionResumenAsync();
    Task<int> GetFacturasCountAsync();
    Task<FacturaCompletaDto?> CreateFacturaAsync(CreateFacturaDto factura);
    
    // Sucursales
    Task<List<SucursalDto>> GetSucursalesAsync();
}
