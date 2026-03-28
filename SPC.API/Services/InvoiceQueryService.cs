using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.Invoices;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Query-side implementation for invoice read operations (CQRS-lite).
/// Only depends on SPCDbContext for data access (read-only).
/// No business logic — just querying and mapping.
/// </summary>
public class InvoiceQueryService : IInvoiceQueryService
{
    private readonly SPCDbContext _db;

    public InvoiceQueryService(SPCDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<InvoiceResponse>> GetAllAsync(int skip = 0, int take = 50)
    {
        var invoices = await _db.Invoices
            .Include(f => f.Customer)
            .Include(f => f.SalesRep)
            .Include(f => f.Details)
            .OrderByDescending(f => f.InvoiceDate)
            .ThenByDescending(f => f.InvoiceNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return invoices.Select(MapToResponse);
    }

    public async Task<InvoiceCompletaResponse?> GetByIdAsync(int id)
    {
        var invoice = await _db.Invoices
            .Include(f => f.Customer)
            .Include(f => f.SalesRep)
            .Include(f => f.Details)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (invoice == null) return null;

        return new InvoiceCompletaResponse
        {
            Id = invoice.Id,
            InvoiceType = invoice.InvoiceType,
            PointOfSale = invoice.PointOfSale,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            CustomerId = invoice.CustomerId,
            CustomerCompanyName = invoice.Customer.CompanyName,
            CustomerCUIT = invoice.Customer.CUIT,
            SalesRepId = invoice.SalesRepId,
            SalesRepFirstName = invoice.SalesRep?.FirstName,
            Subtotal = invoice.Subtotal,
            VATAmount = invoice.VATAmount,
            IncludedVAT = invoice.IncludedVAT,
            IIBBPerceptionAmount = invoice.IIBBPerceptionAmount,
            DiscountAmount = invoice.DiscountAmount,
            Total = invoice.Total,
            CAE = invoice.CAE,
            CAEExpirationDate = invoice.CAEExpirationDate,
            IsVoided = invoice.IsVoided,
            ItemCount = invoice.Details.Count,
            Details = invoice.Details.Select(d => new InvoiceDetailResponse
            {
                Id = d.Id,
                ItemNumber = d.ItemNumber,
                ProductId = d.ProductId,
                ProductCode = d.Product.Code,
                ProductDescription = d.Product.Description,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                DiscountPercent = d.DiscountPercent,
                VATPercent = d.VATPercent,
                Subtotal = d.Subtotal
            }).OrderBy(d => d.ItemNumber).ToList()
        };
    }

    public async Task<IEnumerable<InvoiceResponse>> GetByCustomerAsync(int customerId)
    {
        var invoices = await _db.Invoices
            .Include(f => f.Customer)
            .Include(f => f.SalesRep)
            .Include(f => f.Details)
            .Where(f => f.CustomerId == customerId)
            .OrderByDescending(f => f.InvoiceDate)
            .ThenByDescending(f => f.InvoiceNumber)
            .ToListAsync();

        return invoices.Select(MapToResponse);
    }

    public async Task<IEnumerable<InvoiceResponse>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var invoices = await _db.Invoices
            .Include(f => f.Customer)
            .Include(f => f.SalesRep)
            .Include(f => f.Details)
            .Where(f => f.InvoiceDate >= from && f.InvoiceDate <= to)
            .OrderByDescending(f => f.InvoiceDate)
            .ThenByDescending(f => f.InvoiceNumber)
            .ToListAsync();

        return invoices.Select(MapToResponse);
    }

    public async Task<IEnumerable<InvoiceResponse>> SearchAsync(string term)
    {
        long.TryParse(term.Replace("-", ""), out var invoiceNumber);

        var invoices = await _db.Invoices
            .Include(f => f.Customer)
            .Include(f => f.SalesRep)
            .Include(f => f.Details)
            .Where(f => f.InvoiceNumber == invoiceNumber ||
                       f.Customer.CompanyName.Contains(term) ||
                       (f.Customer.CUIT != null && f.Customer.CUIT.Contains(term)))
            .OrderByDescending(f => f.InvoiceDate)
            .Take(100)
            .ToListAsync();

        return invoices.Select(MapToResponse);
    }

    public async Task<InvoicecionResumenResponse> GetSummaryAsync()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var yearStart = new DateTime(today.Year, 1, 1);

        var invoicesToday = await _db.Invoices
            .Where(f => f.InvoiceDate.Date == today && !f.IsVoided)
            .ToListAsync();

        var invoicesMonth = await _db.Invoices
            .Where(f => f.InvoiceDate >= monthStart && !f.IsVoided)
            .ToListAsync();

        var invoicesYear = await _db.Invoices
            .Where(f => f.InvoiceDate >= yearStart && !f.IsVoided)
            .ToListAsync();

        var totalInvoices = await _db.Invoices.CountAsync();

        return new InvoicecionResumenResponse
        {
            TotalInvoices = totalInvoices,
            InvoicesHoy = invoicesToday.Count,
            InvoicesMes = invoicesMonth.Count,
            MontoHoy = invoicesToday.Sum(f => f.Total),
            MontoMes = invoicesMonth.Sum(f => f.Total),
            MontoAnio = invoicesYear.Sum(f => f.Total)
        };
    }

    public async Task<int> GetCountAsync()
    {
        return await _db.Invoices.CountAsync();
    }

    // ===========================================
    // MAPPING
    // ===========================================

    private static InvoiceResponse MapToResponse(Invoice invoice)
    {
        return new InvoiceResponse
        {
            Id = invoice.Id,
            InvoiceType = invoice.InvoiceType,
            PointOfSale = invoice.PointOfSale,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            CustomerId = invoice.CustomerId,
            CustomerCompanyName = invoice.Customer.CompanyName,
            CustomerCUIT = invoice.Customer.CUIT,
            SalesRepId = invoice.SalesRepId,
            SalesRepFirstName = invoice.SalesRep?.FirstName,
            Subtotal = invoice.Subtotal,
            VATAmount = invoice.VATAmount,
            IncludedVAT = invoice.IncludedVAT,
            IIBBPerceptionAmount = invoice.IIBBPerceptionAmount,
            DiscountAmount = invoice.DiscountAmount,
            Total = invoice.Total,
            CAE = invoice.CAE,
            CAEExpirationDate = invoice.CAEExpirationDate,
            IsVoided = invoice.IsVoided,
            ItemCount = invoice.Details.Count
        };
    }
}
