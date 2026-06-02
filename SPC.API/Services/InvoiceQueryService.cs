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

        return MapToCompleteResponse(invoice);
    }

    public async Task<InvoiceCompletaResponse?> GetByDocumentAsync(string invoiceType, long invoiceNumber, int? pointOfSale = null, int? customerId = null)
    {
        var normalizedType = invoiceType.Trim().ToUpperInvariant();
        var query = _db.Invoices
            .Include(f => f.Customer)
            .Include(f => f.SalesRep)
            .Include(f => f.Details)
                .ThenInclude(d => d.Product)
            .Where(f => f.InvoiceType == normalizedType && f.InvoiceNumber == invoiceNumber);

        if (pointOfSale.HasValue)
        {
            query = query.Where(f => f.PointOfSale == pointOfSale.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(f => f.CustomerId == customerId.Value);
        }

        var invoice = await query
            .OrderByDescending(f => f.InvoiceDate)
            .ThenByDescending(f => f.Id)
            .FirstOrDefaultAsync();

        return invoice == null ? null : MapToCompleteResponse(invoice);
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
        var trimmedTerm = term.Trim();
        var document = OfficialDocumentSearchParser.Parse(trimmedTerm);
        var query = _db.Invoices
            .Include(f => f.Customer)
            .Include(f => f.SalesRep)
            .Include(f => f.Details)
            .AsQueryable();

        if (document.Number.HasValue)
        {
            query = query.Where(f => f.InvoiceNumber == document.Number.Value);

            if (!string.IsNullOrWhiteSpace(document.Type))
            {
                query = query.Where(f => f.InvoiceType == document.Type);
            }

            if (document.PointOfSale.HasValue)
            {
                query = query.Where(f => f.PointOfSale == document.PointOfSale.Value);
            }
        }
        else
        {
            query = query.Where(f => f.Customer.CompanyName.Contains(trimmedTerm) ||
                                     (f.Customer.CUIT != null && f.Customer.CUIT.Contains(trimmedTerm)));
        }

        var invoices = await query
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

    private static InvoiceCompletaResponse MapToCompleteResponse(Invoice invoice)
    {
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
