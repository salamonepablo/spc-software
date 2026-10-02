using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.DeliveryNotes;
using SPC.API.Data;

namespace SPC.API.Services;

public sealed class DeliveryNoteQueryService(SPCDbContext db) : IDeliveryNoteQueryService
{
    public async Task<DeliveryNoteSearchResult> SearchAsync(DeliveryNoteSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(request), "La página debe ser positiva y el tamaño debe estar entre 1 y 100");
        if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate.Value.Date > request.ToDate.Value.Date)
            throw new ArgumentException("La fecha desde no puede ser posterior a la fecha hasta", nameof(request));

        var query = db.DeliveryNotes.AsNoTracking().AsQueryable();
        if (request.BranchId.HasValue) query = query.Where(note => note.BranchId == request.BranchId.Value);
        if (request.CustomerId.HasValue) query = query.Where(note => note.CustomerId == request.CustomerId.Value);
        if (request.InvoiceId.HasValue) query = query.Where(note => note.InvoiceId == request.InvoiceId.Value);
        if (request.FromDate.HasValue) query = query.Where(note => note.DeliveryNoteDate >= request.FromDate.Value.Date);
        if (request.ToDate.HasValue) query = query.Where(note => note.DeliveryNoteDate < request.ToDate.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(note => note.Customer.CompanyName.Contains(term) || note.DeliveryNoteNumber.ToString().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(note => note.DeliveryNoteDate).ThenByDescending(note => note.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(note => new DeliveryNoteSearchResponse
            {
                Id = note.Id, BranchId = note.BranchId, DeliveryNoteNumber = note.DeliveryNoteNumber,
                DeliveryNoteDate = note.DeliveryNoteDate, CustomerId = note.CustomerId,
                CustomerName = note.Customer.CompanyName, InvoiceNumber = note.Invoice == null ? null : note.Invoice.InvoiceNumber
            }).ToListAsync(cancellationToken);
        return new DeliveryNoteSearchResult { Page = request.Page, PageSize = request.PageSize, TotalCount = total, Items = items };
    }

    public Task<DeliveryNoteDetailResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.DeliveryNotes.AsNoTracking().Where(note => note.Id == id)
            .Select(note => new DeliveryNoteDetailResponse
            {
                Id = note.Id, BranchId = note.BranchId, BranchName = note.Branch!.Name,
                OriginalInvoiceBranchId = note.Invoice == null ? note.BranchId : note.Invoice.BranchId,
                OriginalInvoiceBranchName = note.Invoice == null ? note.Branch!.Name : note.Invoice.Branch!.Name,
                InvoiceId = note.InvoiceId, InvoiceType = note.InvoiceType,
                OriginalInvoicePointOfSale = note.Invoice == null ? null : note.Invoice.PointOfSale, PointOfSale = note.PointOfSale,
                DeliveryNoteNumber = note.DeliveryNoteNumber, DeliveryNoteDate = note.DeliveryNoteDate,
                InvoiceNumber = note.Invoice == null ? null : note.Invoice.InvoiceNumber,
                InvoiceDate = note.Invoice == null ? null : note.Invoice.InvoiceDate,
                CustomerId = note.CustomerId, CustomerName = note.Customer.CompanyName,
                CustomerTaxId = note.Customer.CUIT, SalesRepId = note.SalesRepId,
                SalesRepName = note.SalesRep == null ? null : note.SalesRep.FirstName + " " + note.SalesRep.LastName,
                DeliveryAddress = note.DeliveryAddress, DeliveryCity = note.DeliveryCity,
                BusinessUnit = note.BusinessUnit, Clarification = note.Clarification, Notes = note.Notes,
                Details = note.Details.OrderBy(line => line.ItemNumber).Select(line => new DeliveryNoteLineResponse
                {
                    ItemNumber = line.ItemNumber, ProductId = line.ProductId, ProductCode = line.Product.Code,
                    ProductDescription = line.Product.Description, Quantity = line.Quantity
                }).ToList()
            }).SingleOrDefaultAsync(cancellationToken);
}
