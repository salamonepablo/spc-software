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

    public async Task<int> GetCustomersCountAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<CountResponse>("/api/clientes/count");
            return result?.Total ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customers count");
            return 0;
        }
    }

    public async Task<List<CustomerDto>> GetCustomersAsync(int skip = 0, int take = 50)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<CustomerDto>>($"/api/clientes?skip={skip}&take={take}");
            return result ?? new List<CustomerDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching clientes");
            return new List<CustomerDto>();
        }
    }

    public async Task<List<CustomerDto>> GetAllCustomersAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<CustomerDto>>("/api/clientes/all");
            return result ?? new List<CustomerDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all clientes");
            return new List<CustomerDto>();
        }
    }

    public async Task<List<CustomerDto>> BuscarCustomersAsync(string Name)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<CustomerDto>>($"/api/clientes/buscar?Name={Uri.EscapeDataString(Name)}");
            return result ?? new List<CustomerDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching clientes with term: {Name}", Name);
            return new List<CustomerDto>();
        }
    }

    public Task<List<CustomerDto>> SearchCustomersAsync(string term)
    {
        return BuscarCustomersAsync(term);
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
            var result = await _http.GetFromJsonAsync<List<ProductDto>>($"/api/productos/buscar?Description={Uri.EscapeDataString(termino)}");
            return result ?? new List<ProductDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching productos with term: {Termino}", termino);
            return new List<ProductDto>();
        }
    }

    public Task<List<ProductDto>> SearchProductsAsync(string term)
    {
        return BuscarProductsAsync(term);
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
            var result = await _http.GetFromJsonAsync<List<TaxConditionDto>>("/api/TaxConditions");
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
            var result = await _http.GetFromJsonAsync<List<UnitOfMeasureDto>>("/api/UnitsOfMeasure");
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
            var result = await _http.GetFromJsonAsync<List<InvoiceDto>>($"/api/invoices?skip={skip}&take={take}");
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
            return await _http.GetFromJsonAsync<InvoiceCompletaDto>($"/api/invoices/{id}");
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

    public async Task<InvoiceCompletaDto?> GetInvoiceByDocumentAsync(string invoiceType, long invoiceNumber, int? pointOfSale = null, int? customerId = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (pointOfSale.HasValue)
            {
                queryParams.Add($"pointOfSale={pointOfSale.Value}");
            }
            if (customerId.HasValue)
            {
                queryParams.Add($"customerId={customerId.Value}");
            }

            var queryString = queryParams.Count == 0 ? "" : $"?{string.Join("&", queryParams)}";
            return await _http.GetFromJsonAsync<InvoiceCompletaDto>(
                $"/api/invoices/by-document/{Uri.EscapeDataString(invoiceType)}/{invoiceNumber}{queryString}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching factura {InvoiceType} {InvoiceNumber}", invoiceType, invoiceNumber);
            return null;
        }
    }

    public async Task<List<InvoiceDto>> BuscarInvoicesAsync(string termino)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<InvoiceDto>>($"/api/invoices/buscar?termino={Uri.EscapeDataString(termino)}");
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
            var result = await _http.GetFromJsonAsync<List<InvoiceDto>>($"/api/invoices/cliente/{clienteId}");
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
            var result = await _http.GetFromJsonAsync<List<InvoiceDto>>($"/api/invoices/fecha?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}");
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
            return await _http.GetFromJsonAsync<InvoicecionResumenDto>("/api/invoices/resumen");
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
            var result = await _http.GetFromJsonAsync<CountResponse>("/api/invoices/count");
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

    private class CurrentAccountGuardrailErrorDto
    {
        public string? Code { get; set; }
        public string? Message { get; set; }
        public string? GuardrailMode { get; set; }
        public int RangeDays { get; set; }
    }

    public async Task<InvoiceCompletaDto?> CreateInvoiceAsync(CreateInvoiceDto factura)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/invoices", factura);
            
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

    #region Quotes (Presupuestos)

    public async Task<List<QuoteDto>> GetQuotesAsync(int skip = 0, int take = 50)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<QuoteDto>>($"/api/quotes?skip={skip}&take={take}");
            return result ?? new List<QuoteDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching presupuestos");
            return new List<QuoteDto>();
        }
    }

    public async Task<QuoteCompletaDto?> GetQuoteAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<QuoteCompletaDto>($"/api/quotes/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching presupuesto {Id}", id);
            return null;
        }
    }

    public async Task<QuoteCompletaDto?> GetQuoteByNumberAsync(long quoteNumber)
    {
        try
        {
            return await _http.GetFromJsonAsync<QuoteCompletaDto>($"/api/quotes/by-number/{quoteNumber}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching presupuesto by quote number {QuoteNumber}", quoteNumber);
            return null;
        }
    }

    public async Task<List<QuoteDto>> BuscarQuotesAsync(string termino)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<QuoteDto>>($"/api/quotes/buscar?termino={Uri.EscapeDataString(termino)}");
            return result ?? new List<QuoteDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching presupuestos with term: {Termino}", termino);
            return new List<QuoteDto>();
        }
    }

    public async Task<List<QuoteDto>> GetQuotesByCustomerAsync(int clienteId)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<QuoteDto>>($"/api/quotes/cliente/{clienteId}");
            return result ?? new List<QuoteDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching presupuestos for cliente {Id}", clienteId);
            return new List<QuoteDto>();
        }
    }

    public async Task<List<QuoteDto>> GetQuotesByFechaAsync(DateTime desde, DateTime hasta)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<QuoteDto>>($"/api/quotes/fecha?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}");
            return result ?? new List<QuoteDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching presupuestos by date range");
            return new List<QuoteDto>();
        }
    }

    public async Task<QuotesResumenDto?> GetQuotesResumenAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<QuotesResumenDto>("/api/quotes/resumen");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching presupuestos resumen");
            return null;
        }
    }

    public async Task<int> GetQuotesCountAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<CountResponse>("/api/quotes/count");
            return result?.Total ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching presupuestos count");
            return 0;
        }
    }

    public async Task<QuoteCompletaDto?> CreateQuoteAsync(CreateQuoteDto presupuesto)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/quotes", presupuesto);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<QuoteCompletaDto>();
            }
            
            _logger.LogWarning("Failed to create presupuesto. Status: {Status}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating presupuesto");
            return null;
        }
    }

    public async Task<bool> AnularQuoteAsync(int id, string motivo)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"/api/quotes/{id}/anular", new { Reason = motivo });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error anulando presupuesto {Id}", id);
            return false;
        }
    }

    #endregion

    #region Current Account (Cuenta Corriente)

    public async Task<CurrentAccountDto?> GetCurrentAccountAsync(int customerId)
    {
        try
        {
            return await _http.GetFromJsonAsync<CurrentAccountDto>($"/api/current-accounts/{customerId}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching current account for customer {CustomerId}", customerId);
            return null;
        }
    }

    public async Task<CurrentAccountMovementsDto?> GetCurrentAccountMovementsAsync(
        int customerId,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int? line = null,
        int skip = 0,
        int take = 50)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"skip={skip}",
                $"take={take}"
            };

            if (dateFrom.HasValue)
                queryParams.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
            if (dateTo.HasValue)
                queryParams.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");
            if (line.HasValue)
                queryParams.Add($"line={line.Value}");

            var queryString = string.Join("&", queryParams);
            return await _http.GetFromJsonAsync<CurrentAccountMovementsDto>(
                $"/api/current-accounts/{customerId}/movements?{queryString}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching current account movements for customer {CustomerId}", customerId);
            return null;
        }
    }

    public async Task<PaymentDetailDto?> GetPaymentByNumberAsync(long paymentNumber, int? customerId = null)
    {
        try
        {
            var route = customerId.HasValue
                ? $"/api/payments/{paymentNumber}?customerId={customerId.Value}"
                : $"/api/payments/{paymentNumber}";

            return await _http.GetFromJsonAsync<PaymentDetailDto>(route);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching payment {PaymentNumber}", paymentNumber);
            return null;
        }
    }

    public async Task<CurrentAccountMovementsDto?> GetCurrentAccountMovementsByRangeAsync(
        int customerId,
        DateTime dateFrom,
        DateTime dateTo,
        int? line = null)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"dateFrom={dateFrom:yyyy-MM-dd}",
                $"dateTo={dateTo:yyyy-MM-dd}"
            };

            if (line.HasValue)
            {
                queryParams.Add($"line={line.Value}");
            }

            var queryString = string.Join("&", queryParams);
            var response = await _http.GetAsync($"/api/current-accounts/{customerId}/movements/range?{queryString}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CurrentAccountMovementsDto>();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var guardrail = await response.Content.ReadFromJsonAsync<CurrentAccountGuardrailErrorDto>();
                _logger.LogWarning(
                    "Current account range search rejected for customer {CustomerId}: {Code} - {Message}",
                    customerId,
                    guardrail?.Code,
                    guardrail?.Message);

                return new CurrentAccountMovementsDto
                {
                    CustomerId = customerId,
                    GuardrailApplied = true,
                    GuardrailMode = guardrail?.GuardrailMode ?? "rejected",
                    WarningCode = guardrail?.Code,
                    WarningMessage = guardrail?.Message ?? "La búsqueda fue rechazada por las reglas de rango de cuenta corriente.",
                    RangeDays = guardrail?.RangeDays ?? 0
                };
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching full-range current account movements for customer {CustomerId}", customerId);
            return null;
        }
    }

    public async Task<CreditNoteDetailDto?> GetCreditNoteByNumberAsync(long creditNoteNumber, int? customerId = null, string? voucherType = null, int? pointOfSale = null)
    {
        try
        {
            var route = BuildNoteByNumberRoute("/api/notas-credito/number", creditNoteNumber, customerId, voucherType, pointOfSale);

            return await _http.GetFromJsonAsync<CreditNoteDetailDto>(route);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching credit note {CreditNoteNumber}", creditNoteNumber);
            return null;
        }
    }

    public async Task<DebitNoteDetailDto?> GetDebitNoteByNumberAsync(long debitNoteNumber, int? customerId = null, string? voucherType = null, int? pointOfSale = null)
    {
        try
        {
            var route = BuildNoteByNumberRoute("/api/notas-debito/number", debitNoteNumber, customerId, voucherType, pointOfSale);

            return await _http.GetFromJsonAsync<DebitNoteDetailDto>(route);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching debit note {DebitNoteNumber}", debitNoteNumber);
            return null;
        }
    }

    private static string BuildNoteByNumberRoute(string baseRoute, long documentNumber, int? customerId, string? voucherType, int? pointOfSale)
    {
        var queryParams = new List<string>();
        if (customerId.HasValue)
        {
            queryParams.Add($"customerId={customerId.Value}");
        }
        if (!string.IsNullOrWhiteSpace(voucherType))
        {
            queryParams.Add($"voucherType={Uri.EscapeDataString(voucherType)}");
        }
        if (pointOfSale.HasValue)
        {
            queryParams.Add($"pointOfSale={pointOfSale.Value}");
        }

        var queryString = queryParams.Count == 0 ? "" : $"?{string.Join("&", queryParams)}";
        return $"{baseRoute}/{documentNumber}{queryString}";
    }

    #endregion
}
