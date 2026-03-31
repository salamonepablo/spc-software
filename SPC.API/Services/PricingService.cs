namespace SPC.API.Services;

/// <summary>
/// Pricing service implementation.
/// Handles all price calculations for invoices, credit notes, debit notes, and quotes.
/// </summary>
public class PricingService : IPricingService
{
    /// <summary>
    /// Calculates line item values.
    /// Formula: 
    ///   GrossAmount = UnitPrice * Quantity
    ///   DiscountAmount = GrossAmount * (DiscountPercent / 100)
    ///   Subtotal = GrossAmount - DiscountAmount
    /// </summary>
    public LineCalculationResult CalculateLine(
        decimal unitPrice,
        decimal quantity,
        decimal lineDiscountPercent,
        decimal vatPercent)
    {
        var grossAmount = unitPrice * quantity;
        var discountAmount = grossAmount * (lineDiscountPercent / 100m);
        var subtotal = grossAmount - discountAmount;
        
        return new LineCalculationResult
        {
            UnitPrice = unitPrice,
            Quantity = quantity,
            DiscountPercent = lineDiscountPercent,
            DiscountAmount = Math.Round(discountAmount, 2),
            VATPercent = vatPercent,
            GrossAmount = Math.Round(grossAmount, 2),
            Subtotal = Math.Round(subtotal, 2)
        };
    }
    
    /// <summary>
    /// Calculates document totals with multi-level discounts.
    /// Formula:
    ///   LinesSubtotal = Sum of line subtotals
    ///   DocumentDiscountAmount = LinesSubtotal * (DocumentDiscountPercent / 100)
    ///   NetSubtotal = LinesSubtotal - DocumentDiscountAmount
    ///   VATAmount = NetSubtotal * (VATPercent / 100)
    ///   IIBBAmount = (NetSubtotal + VATAmount) * (IIBBPercent / 100)
    ///   Total = NetSubtotal + VATAmount + IIBBAmount
    /// 
    /// Note: IIBB is calculated on the total with VAT as per Argentine regulations.
    /// </summary>
    public DocumentCalculationResult CalculateDocument(
        IEnumerable<LineCalculationResult> lines,
        decimal documentDiscountPercent,
        decimal vatPercent,
        decimal iibbPercent)
    {
        var linesList = lines.ToList();
        var linesSubtotal = linesList.Sum(l => l.Subtotal);
        
        var documentDiscountAmount = linesSubtotal * (documentDiscountPercent / 100m);
        var netSubtotal = linesSubtotal - documentDiscountAmount;
        
        var vatAmount = netSubtotal * (vatPercent / 100m);
        
        // IIBB is calculated on total with VAT
        var baseForIIBB = netSubtotal + vatAmount;
        var iibbAmount = baseForIIBB * (iibbPercent / 100m);
        
        var total = netSubtotal + vatAmount + iibbAmount;
        
        return new DocumentCalculationResult
        {
            LinesSubtotal = Math.Round(linesSubtotal, 2),
            DocumentDiscountPercent = documentDiscountPercent,
            DocumentDiscountAmount = Math.Round(documentDiscountAmount, 2),
            NetSubtotal = Math.Round(netSubtotal, 2),
            VATPercent = vatPercent,
            VATAmount = Math.Round(vatAmount, 2),
            IIBBPercent = iibbPercent,
            IIBBAmount = Math.Round(iibbAmount, 2),
            Total = Math.Round(total, 2)
        };
    }
    
    /// <summary>
    /// Calculates document totals for Invoice A (net prices + VAT discriminated).
    /// VAT is ADDED to the net subtotal.
    /// IIBB only applies if company is perception agent.
    /// </summary>
    public DocumentCalculationResult CalculateDocumentTypeA(
        IEnumerable<LineCalculationResult> lines,
        decimal documentDiscountPercent,
        decimal vatPercent,
        decimal customerIIBBPercent,
        bool isIIBBPerceptionAgent)
    {
        var linesList = lines.ToList();
        var linesSubtotal = linesList.Sum(l => l.Subtotal);
        
        var documentDiscountAmount = linesSubtotal * (documentDiscountPercent / 100m);
        var netSubtotal = linesSubtotal - documentDiscountAmount;
        
        // Invoice A: Add VAT to net subtotal
        var vatAmount = netSubtotal * (vatPercent / 100m);
        
        // IIBB only if company is perception agent
        decimal iibbAmount = 0m;
        decimal iibbPercent = 0m;
        if (isIIBBPerceptionAgent && customerIIBBPercent > 0)
        {
            iibbPercent = customerIIBBPercent;
            var baseForIIBB = netSubtotal + vatAmount;
            iibbAmount = baseForIIBB * (iibbPercent / 100m);
        }
        
        var total = netSubtotal + vatAmount + iibbAmount;
        
        return new DocumentCalculationResult
        {
            LinesSubtotal = Math.Round(linesSubtotal, 2),
            DocumentDiscountPercent = documentDiscountPercent,
            DocumentDiscountAmount = Math.Round(documentDiscountAmount, 2),
            NetSubtotal = Math.Round(netSubtotal, 2),
            VATPercent = vatPercent,
            VATAmount = Math.Round(vatAmount, 2),
            IIBBPercent = iibbPercent,
            IIBBAmount = Math.Round(iibbAmount, 2),
            Total = Math.Round(total, 2)
        };
    }
    
    /// <summary>
    /// Calculates document totals for Invoice B (final prices with VAT included).
    /// VAT is NOT added - it's extracted from the total as "IVA Contenido".
    /// Formula: IVA Contenido = Total / (1 + VAT%) * VAT%
    /// </summary>
    public DocumentCalculationResultTypeB CalculateDocumentTypeB(
        IEnumerable<LineCalculationResult> lines,
        decimal documentDiscountPercent,
        decimal vatPercent)
    {
        var linesList = lines.ToList();
        var linesSubtotal = linesList.Sum(l => l.Subtotal);
        
        var documentDiscountAmount = linesSubtotal * (documentDiscountPercent / 100m);
        var total = linesSubtotal - documentDiscountAmount;
        
        // Invoice B: Price already includes VAT
        // Calculate IVA Contenido for transparency (Ley 27.743)
        // IVA Contenido = Total / 1.21 * 0.21 = Total * 0.21 / 1.21
        var vatMultiplier = vatPercent / 100m;
        var vatContained = total * vatMultiplier / (1 + vatMultiplier);
        
        // Net subtotal (before VAT, for internal use)
        var netSubtotal = total - vatContained;
        
        return new DocumentCalculationResultTypeB
        {
            LinesSubtotal = Math.Round(linesSubtotal, 2),
            DocumentDiscountPercent = documentDiscountPercent,
            DocumentDiscountAmount = Math.Round(documentDiscountAmount, 2),
            NetSubtotal = Math.Round(netSubtotal, 2),
            VATPercent = vatPercent,
            VATContained = Math.Round(vatContained, 2),
            Total = Math.Round(total, 2)
        };
    }
    
    /// <summary>
    /// Resolves discount: uses requested if provided, otherwise customer default.
    /// </summary>
    public decimal ResolveDiscount(decimal? requestedDiscount, decimal customerDefaultDiscount)
    {
        // If explicitly provided (even if 0), use it
        // Otherwise, fall back to customer default
        return requestedDiscount ?? customerDefaultDiscount;
    }
}
