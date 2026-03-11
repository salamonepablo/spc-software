namespace SPC.API.Services;

/// <summary>
/// Service for price calculations including discounts, VAT, and IIBB.
/// Centralizes all pricing logic to ensure consistency across documents.
/// </summary>
public interface IPricingService
{
    /// <summary>
    /// Calculates line item values including discount and subtotal.
    /// </summary>
    LineCalculationResult CalculateLine(
        decimal unitPrice,
        decimal quantity,
        decimal lineDiscountPercent,
        decimal vatPercent);
    
    /// <summary>
    /// Calculates document totals with multi-level discounts.
    /// Legacy method - use CalculateDocumentTypeA or CalculateDocumentTypeB instead.
    /// </summary>
    DocumentCalculationResult CalculateDocument(
        IEnumerable<LineCalculationResult> lines,
        decimal documentDiscountPercent,
        decimal vatPercent,
        decimal iibbPercent);
    
    /// <summary>
    /// Calculates document totals for Invoice A (net prices + VAT discriminated).
    /// VAT is ADDED to the net subtotal.
    /// IIBB only applies if company is perception agent.
    /// </summary>
    /// <param name="lines">Line calculation results</param>
    /// <param name="documentDiscountPercent">Document-level discount</param>
    /// <param name="vatPercent">VAT percentage to add</param>
    /// <param name="customerIIBBPercent">Customer's IIBB rate from padrón</param>
    /// <param name="isIIBBPerceptionAgent">Is the company an IIBB perception agent?</param>
    DocumentCalculationResult CalculateDocumentTypeA(
        IEnumerable<LineCalculationResult> lines,
        decimal documentDiscountPercent,
        decimal vatPercent,
        decimal customerIIBBPercent,
        bool isIIBBPerceptionAgent);
    
    /// <summary>
    /// Calculates document totals for Invoice B (final prices with VAT included).
    /// VAT is NOT added - it's extracted from the total as "IVA Contenido".
    /// Complies with Ley 27.743 - Régimen de Transparencia Fiscal.
    /// </summary>
    /// <param name="lines">Line calculation results (prices include VAT)</param>
    /// <param name="documentDiscountPercent">Document-level discount</param>
    /// <param name="vatPercent">VAT percentage (for calculating IVA Contenido)</param>
    DocumentCalculationResultTypeB CalculateDocumentTypeB(
        IEnumerable<LineCalculationResult> lines,
        decimal documentDiscountPercent,
        decimal vatPercent);
    
    /// <summary>
    /// Applies customer default discount if no override is provided.
    /// </summary>
    decimal ResolveDiscount(decimal? requestedDiscount, decimal customerDefaultDiscount);
}

/// <summary>
/// Result of a line item calculation
/// </summary>
public record LineCalculationResult
{
    /// <summary>Original unit price before discount</summary>
    public decimal UnitPrice { get; init; }
    
    /// <summary>Quantity</summary>
    public decimal Quantity { get; init; }
    
    /// <summary>Line discount percentage</summary>
    public decimal DiscountPercent { get; init; }
    
    /// <summary>Line discount amount</summary>
    public decimal DiscountAmount { get; init; }
    
    /// <summary>VAT percentage for this line</summary>
    public decimal VATPercent { get; init; }
    
    /// <summary>Line subtotal after discount (before VAT)</summary>
    public decimal Subtotal { get; init; }
    
    /// <summary>Gross amount before any discount: UnitPrice * Quantity</summary>
    public decimal GrossAmount { get; init; }
}

/// <summary>
/// Result of a document calculation
/// </summary>
public record DocumentCalculationResult
{
    /// <summary>Sum of line subtotals (after line discounts, before doc discount)</summary>
    public decimal LinesSubtotal { get; init; }
    
    /// <summary>Document-level discount percentage</summary>
    public decimal DocumentDiscountPercent { get; init; }
    
    /// <summary>Document-level discount amount</summary>
    public decimal DocumentDiscountAmount { get; init; }
    
    /// <summary>Net subtotal after all discounts (before VAT)</summary>
    public decimal NetSubtotal { get; init; }
    
    /// <summary>VAT percentage applied</summary>
    public decimal VATPercent { get; init; }
    
    /// <summary>VAT amount</summary>
    public decimal VATAmount { get; init; }
    
    /// <summary>IIBB perception percentage</summary>
    public decimal IIBBPercent { get; init; }
    
    /// <summary>IIBB perception amount</summary>
    public decimal IIBBAmount { get; init; }
    
    /// <summary>Final total: NetSubtotal + VAT + IIBB</summary>
    public decimal Total { get; init; }
}

/// <summary>
/// Result of a Invoice B calculation (prices include VAT)
/// </summary>
public record DocumentCalculationResultTypeB
{
    /// <summary>Sum of line subtotals (final prices with VAT)</summary>
    public decimal LinesSubtotal { get; init; }
    
    /// <summary>Document-level discount percentage</summary>
    public decimal DocumentDiscountPercent { get; init; }
    
    /// <summary>Document-level discount amount</summary>
    public decimal DocumentDiscountAmount { get; init; }
    
    /// <summary>Net subtotal (price without VAT, for internal calculations)</summary>
    public decimal NetSubtotal { get; init; }
    
    /// <summary>VAT percentage (for IVA Contenido calculation)</summary>
    public decimal VATPercent { get; init; }
    
    /// <summary>
    /// IVA Contenido - VAT contained in the final price.
    /// Required by Ley 27.743 - Régimen de Transparencia Fiscal.
    /// </summary>
    public decimal VATContained { get; init; }
    
    /// <summary>Final total (same as after discount, VAT already included)</summary>
    public decimal Total { get; init; }
}
