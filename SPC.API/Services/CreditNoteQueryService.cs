using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.CreditNotes;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Query-side implementation for credit note read operations (CQRS-lite).
/// Only depends on SPCDbContext for data access (read-only).
/// No business logic — just querying and mapping.
/// </summary>
public class CreditNoteQueryService : ICreditNoteQueryService
{
    private readonly SPCDbContext _db;

    public CreditNoteQueryService(SPCDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<CreditNoteResponse>> GetAllAsync(int skip = 0, int take = 50)
    {
        var notes = await _db.CreditNotes
            .Include(n => n.Customer)
            .Include(n => n.SalesRep)
            .Include(n => n.Branch)
            .Include(n => n.Invoice)
            .Include(n => n.Details)
            .OrderByDescending(n => n.CreditNoteDate)
            .ThenByDescending(n => n.CreditNoteNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return notes.Select(MapToResponse);
    }

    public async Task<CreditNoteCompletaResponse?> GetByIdAsync(int id)
    {
        var note = await _db.CreditNotes
            .Include(n => n.Customer)
            .Include(n => n.SalesRep)
            .Include(n => n.Branch)
            .Include(n => n.Invoice)
            .Include(n => n.Details)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (note == null) return null;

        return MapToCompleteResponse(note);
    }

    public async Task<CreditNoteCompletaResponse?> GetByNumberAsync(long creditNoteNumber, int? customerId = null)
    {
        var query = _db.CreditNotes
            .Include(n => n.Customer)
            .Include(n => n.SalesRep)
            .Include(n => n.Branch)
            .Include(n => n.Invoice)
            .Include(n => n.Details)
                .ThenInclude(d => d.Product)
            .Where(n => n.CreditNoteNumber == creditNoteNumber);

        if (customerId.HasValue)
        {
            query = query.Where(n => n.CustomerId == customerId.Value);
        }

        var note = await query
            .OrderByDescending(n => n.CreditNoteDate)
            .ThenByDescending(n => n.Id)
            .FirstOrDefaultAsync();

        return note == null ? null : MapToCompleteResponse(note);
    }

    public async Task<IEnumerable<CreditNoteResponse>> GetByCustomerAsync(int customerId)
    {
        var notes = await _db.CreditNotes
            .Include(n => n.Customer)
            .Include(n => n.SalesRep)
            .Include(n => n.Branch)
            .Include(n => n.Invoice)
            .Include(n => n.Details)
            .Where(n => n.CustomerId == customerId)
            .OrderByDescending(n => n.CreditNoteDate)
            .ToListAsync();

        return notes.Select(MapToResponse);
    }

    public async Task<IEnumerable<CreditNoteResponse>> GetByInvoiceAsync(int invoiceId)
    {
        var notes = await _db.CreditNotes
            .Include(n => n.Customer)
            .Include(n => n.SalesRep)
            .Include(n => n.Branch)
            .Include(n => n.Invoice)
            .Include(n => n.Details)
            .Where(n => n.InvoiceId == invoiceId)
            .OrderByDescending(n => n.CreditNoteDate)
            .ToListAsync();

        return notes.Select(MapToResponse);
    }

    public async Task<IEnumerable<CreditNoteResponse>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var notes = await _db.CreditNotes
            .Include(n => n.Customer)
            .Include(n => n.SalesRep)
            .Include(n => n.Branch)
            .Include(n => n.Invoice)
            .Include(n => n.Details)
            .Where(n => n.CreditNoteDate >= from && n.CreditNoteDate <= to)
            .OrderByDescending(n => n.CreditNoteDate)
            .ToListAsync();

        return notes.Select(MapToResponse);
    }

    public async Task<IEnumerable<CreditNoteResponse>> SearchAsync(string term)
    {
        long.TryParse(term.Replace("-", ""), out var noteNumber);

        var notes = await _db.CreditNotes
            .Include(n => n.Customer)
            .Include(n => n.SalesRep)
            .Include(n => n.Branch)
            .Include(n => n.Invoice)
            .Include(n => n.Details)
            .Where(n => n.CreditNoteNumber == noteNumber ||
                       n.Customer!.CompanyName.Contains(term) ||
                       (n.Customer!.CUIT != null && n.Customer.CUIT.Contains(term)))
            .OrderByDescending(n => n.CreditNoteDate)
            .Take(100)
            .ToListAsync();

        return notes.Select(MapToResponse);
    }

    public async Task<int> GetCountAsync()
    {
        return await _db.CreditNotes.CountAsync();
    }

    // ===========================================
    // MAPPING
    // ===========================================

    private static CreditNoteResponse MapToResponse(CreditNote note)
    {
        return new CreditNoteResponse
        {
            Id = note.Id,
            VoucherType = note.VoucherType == VoucherType.CreditNoteA ? "A" : "B",
            PointOfSale = note.PointOfSale,
            CreditNoteNumber = note.CreditNoteNumber,
            CreditNoteDate = note.CreditNoteDate,
            CustomerId = note.CustomerId,
            CustomerName = note.Customer?.CompanyName ?? "",
            CustomerCUIT = note.Customer?.CUIT,
            SalesRepId = note.SalesRepId,
            SalesRepName = note.SalesRep?.FirstName,
            InvoiceId = note.InvoiceId,
            InvoiceNumber = note.Invoice != null
                ? $"{note.Invoice.InvoiceType} {note.Invoice.PointOfSale:D4}-{note.Invoice.InvoiceNumber:D8}"
                : null,
            Subtotal = note.Subtotal,
            VATPercent = note.VATPercent,
            VATAmount = note.VATAmount,
            IIBBPercent = note.IIBBPercent,
            IIBBAmount = note.IIBBAmount,
            DiscountPercent = note.DiscountPercent,
            DiscountAmount = note.DiscountAmount,
            Total = note.Total,
            CAE = note.CAE,
            CAEExpirationDate = note.CAEExpirationDate,
            IsVoided = note.IsVoided,
            ItemCount = note.Details.Count
        };
    }

    private static CreditNoteCompletaResponse MapToCompleteResponse(CreditNote note)
    {
        return new CreditNoteCompletaResponse
        {
            Id = note.Id,
            VoucherType = note.VoucherType == VoucherType.CreditNoteA ? "A" : "B",
            PointOfSale = note.PointOfSale,
            CreditNoteNumber = note.CreditNoteNumber,
            CreditNoteDate = note.CreditNoteDate,
            CustomerId = note.CustomerId,
            CustomerName = note.Customer?.CompanyName ?? "",
            CustomerCUIT = note.Customer?.CUIT,
            SalesRepId = note.SalesRepId,
            SalesRepName = note.SalesRep?.FirstName,
            InvoiceId = note.InvoiceId,
            InvoiceNumber = note.Invoice != null
                ? $"{note.Invoice.InvoiceType} {note.Invoice.PointOfSale:D4}-{note.Invoice.InvoiceNumber:D8}"
                : null,
            Subtotal = note.Subtotal,
            VATPercent = note.VATPercent,
            VATAmount = note.VATAmount,
            IIBBPercent = note.IIBBPercent,
            IIBBAmount = note.IIBBAmount,
            DiscountPercent = note.DiscountPercent,
            DiscountAmount = note.DiscountAmount,
            Total = note.Total,
            CAE = note.CAE,
            CAEExpirationDate = note.CAEExpirationDate,
            IsVoided = note.IsVoided,
            ItemCount = note.Details.Count,
            SalesCondition = note.SalesCondition,
            Notes = note.Notes,
            Details = note.Details.Select(d => new CreditNoteDetalleResponse
            {
                Id = d.Id,
                ItemNumber = d.ItemNumber,
                ProductId = d.ProductId,
                ProductCode = d.Product?.Code ?? "",
                ProductDescription = d.Product?.Description ?? "",
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                DiscountPercent = d.DiscountPercent,
                DiscountAmount = d.DiscountAmount,
                Subtotal = d.Subtotal
            }).OrderBy(d => d.ItemNumber).ToList()
        };
    }
}
