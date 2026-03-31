using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.Invoices;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Command-side implementation for invoice write operations (CQRS-lite).
/// Contains all business logic for creating and voiding invoices.
/// Depends on ITaxConfigurationService, IPricingService, ICompanySettingsService
/// for calculation rules, IInvoiceQueryService for post-creation retrieval,
/// and ICurrentAccountService for recording billing movements.
/// </summary>
public class InvoiceCommandService : IInvoiceCommandService
{
    private readonly SPCDbContext _db;
    private readonly ITaxConfigurationService _taxService;
    private readonly IPricingService _pricingService;
    private readonly ICompanySettingsService _companySettings;
    private readonly IInvoiceQueryService _queryService;
    private readonly ICurrentAccountService _currentAccountService;

    public InvoiceCommandService(
        SPCDbContext db,
        ITaxConfigurationService taxService,
        IPricingService pricingService,
        ICompanySettingsService companySettings,
        IInvoiceQueryService queryService,
        ICurrentAccountService currentAccountService)
    {
        _db = db;
        _taxService = taxService;
        _pricingService = pricingService;
        _companySettings = companySettings;
        _queryService = queryService;
        _currentAccountService = currentAccountService;
    }

    public async Task<InvoiceCompletaResponse> CreateAsync(CreateInvoiceRequest request)
    {
        // 1. Validate and load customer
        var customer = await _db.Customers
            .Include(c => c.TaxCondition)
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId)
            ?? throw new InvalidOperationException($"Customer {request.CustomerId} no encontrado");

        // 2. Validate and load branch
        var branch = await _db.Branches.FindAsync(request.BranchId)
            ?? throw new InvalidOperationException($"Sucursal {request.BranchId} no encontrada");

        // 3. Load all products for validation
        var productIds = request.Details.Select(d => d.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var detail in request.Details)
        {
            if (!products.ContainsKey(detail.ProductId))
                throw new InvalidOperationException($"Product {detail.ProductId} no encontrado");
        }

        // 4. Get VAT rate from configuration (not hardcoded!)
        var vatRate = await _taxService.GetDefaultVATRateAsync();

        // 5. Resolve document discount (use customer default if not specified)
        var documentDiscount = _pricingService.ResolveDiscount(
            request.DiscountPercent,
            customer.DiscountPercent);

        // 6. Determine if this is Invoice A or B
        bool isInvoiceA = request.InvoiceType.ToUpperInvariant() == "A";

        // 7. Calculate line items
        var lineResults = new List<(CreateInvoiceDetailRequest detail, LineCalculationResult calc, Product product)>();
        foreach (var detail in request.Details)
        {
            var product = products[detail.ProductId];

            decimal unitPrice;
            if (detail.UnitPrice.HasValue)
            {
                unitPrice = detail.UnitPrice.Value;
            }
            else
            {
                unitPrice = isInvoiceA ? product.InvoicePrice : product.QuotePrice;
            }

            var lineVat = detail.VATPercent ?? product.VATPercent;

            var lineCalc = _pricingService.CalculateLine(
                unitPrice,
                detail.Quantity,
                detail.DiscountPercent,
                lineVat);

            lineResults.Add((detail, lineCalc, product));
        }

        // 8. Get company settings for IIBB agent status
        var isIIBBAgent = await _companySettings.IsIIBBPerceptionAgentAsync();

        var customerIIBBRate = request.IIBBPercent > 0
            ? request.IIBBPercent
            : customer.IIBBPercent;

        // 9. Calculate document totals based on invoice type
        Invoice invoice;
        var nextNumber = await GetNextInvoiceNumberAsync(request.InvoiceType, branch.PointOfSale);

        if (isInvoiceA)
        {
            var docCalc = _pricingService.CalculateDocumentTypeA(
                lineResults.Select(l => l.calc),
                documentDiscount,
                vatRate,
                customerIIBBRate,
                isIIBBAgent);

            invoice = new Invoice
            {
                BranchId = request.BranchId,
                InvoiceType = "A",
                PointOfSale = branch.PointOfSale,
                InvoiceNumber = nextNumber,
                InvoiceDate = DateTime.Now,
                CustomerId = request.CustomerId,
                SalesRepId = request.SalesRepId,
                VATPercent = vatRate,
                Subtotal = docCalc.NetSubtotal,
                VATAmount = docCalc.VATAmount,
                IncludedVAT = 0,
                IIBBPercent = docCalc.IIBBPercent,
                IIBBPerceptionAmount = docCalc.IIBBAmount,
                DiscountPercent = documentDiscount,
                DiscountAmount = docCalc.DocumentDiscountAmount,
                Total = docCalc.Total,
                SalesCondition = request.SalesCondition,
                Notes = request.Notes,
                IsVoided = false
            };
        }
        else
        {
            var docCalc = _pricingService.CalculateDocumentTypeB(
                lineResults.Select(l => l.calc),
                documentDiscount,
                vatRate);

            invoice = new Invoice
            {
                BranchId = request.BranchId,
                InvoiceType = "B",
                PointOfSale = branch.PointOfSale,
                InvoiceNumber = nextNumber,
                InvoiceDate = DateTime.Now,
                CustomerId = request.CustomerId,
                SalesRepId = request.SalesRepId,
                VATPercent = vatRate,
                Subtotal = docCalc.Total,
                VATAmount = 0,
                IncludedVAT = docCalc.VATContained,
                IIBBPercent = 0,
                IIBBPerceptionAmount = 0,
                DiscountPercent = documentDiscount,
                DiscountAmount = docCalc.DocumentDiscountAmount,
                Total = docCalc.Total,
                SalesCondition = request.SalesCondition,
                Notes = request.Notes,
                IsVoided = false
            };
        }

        // 10. Create detail lines
        int itemNumber = 1;
        foreach (var (detail, calc, product) in lineResults)
        {
            invoice.Details.Add(new InvoiceDetail
            {
                ItemNumber = itemNumber++,
                ProductId = detail.ProductId,
                Quantity = detail.Quantity,
                UnitPrice = calc.UnitPrice,
                DiscountPercent = calc.DiscountPercent,
                VATPercent = calc.VATPercent,
                Subtotal = calc.Subtotal
            });
        }

        // 11. Save to database
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        // 12. Record movement in current account (Billing line - L1)
        var docType = isInvoiceA ? DocumentType.InvoiceA : DocumentType.InvoiceB;
        await _currentAccountService.RecordMovementAsync(
            customerId: request.CustomerId,
            documentType: docType,
            documentNumber: invoice.InvoiceNumber,
            billingAmount: invoice.Total,
            budgetAmount: 0,
            description: $"Factura {invoice.InvoiceType} {branch.PointOfSale:D4}-{invoice.InvoiceNumber:D8}");

        // 13. Return complete response via query service
        return (await _queryService.GetByIdAsync(invoice.Id))!;
    }

    public async Task<bool> VoidAsync(int id, string reason)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Branch)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
            return false;

        if (invoice.IsVoided)
            return false;

        invoice.IsVoided = true;
        invoice.Notes = string.IsNullOrEmpty(invoice.Notes)
            ? $"IsVoided: {reason}"
            : $"{invoice.Notes} | IsVoided: {reason}";

        await _db.SaveChangesAsync();

        // Reverse the impact on Current Account (negative amount)
        var docType = invoice.InvoiceType == "A" ? DocumentType.InvoiceA : DocumentType.InvoiceB;
        await _currentAccountService.RecordMovementAsync(
            customerId: invoice.CustomerId,
            documentType: docType,
            documentNumber: invoice.InvoiceNumber,
            billingAmount: -invoice.Total,
            budgetAmount: 0,
            description: $"Anulación Factura {invoice.InvoiceType} {invoice.PointOfSale:D4}-{invoice.InvoiceNumber:D8}: {reason}");

        return true;
    }

    private async Task<long> GetNextInvoiceNumberAsync(string invoiceType, int pointOfSale)
    {
        var lastNumber = await _db.Invoices
            .Where(f => f.InvoiceType == invoiceType && f.PointOfSale == pointOfSale)
            .MaxAsync(f => (long?)f.InvoiceNumber) ?? 0;

        return lastNumber + 1;
    }
}
