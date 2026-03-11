namespace SPC.Shared.Models;

/// <summary>
/// Linea de cuenta corriente a la que aplica un movimiento.
/// </summary>
public enum AccountLineType
{
    /// <summary>Linea 1: Invoices, NC, ND (documentos fiscales)</summary>
    Billing = 1,
    
    /// <summary>Linea 2: Quotes (documentos internos, sin IVA)</summary>
    Budget = 2
}

/// <summary>
/// Tipos de documento para movimientos de cuenta corriente.
/// </summary>
public enum DocumentType
{
    // Generic types (for migration/legacy compatibility)
    Invoice = 1,            // FA (generic)
    CreditNote = 3,         // NC (generic)
    DebitNote = 5,          // ND (generic)
    InternalDebitNote = 7,  // NDI
    Quote = 20,             // PR Quote
    Payment = 30,           // PA Pago
    Receipt = 31,           // RE Recibo
    Other = 99,             // Otros
    
    // Billing (Linea 1) - Documentos fiscales
    InvoiceA = 101,           // FA-A Invoice A
    InvoiceB = 102,           // FA-B Invoice B
    CreditNoteA = 103,        // NC-A Nota Credito A
    CreditNoteB = 104,        // NC-B Nota Credito B
    DebitNoteA = 105,         // ND-A Nota Debito A (fiscal)
    DebitNoteB = 106,         // ND-B Nota Debito B (fiscal)
    PaymentBilling = 108,     // PA-L1 Pago imputado a Billing
    PaymentVoidBilling = 109, // AN-PA-L1 Anulacion de Pago Billing
    
    // Budget (Linea 2) - Documentos internos
    QuoteVoid = 121,            // AN-PR Anulacion Quote
    PaymentBudget = 122,        // PA-L2 Pago imputado a Budget
    PaymentVoidBudget = 123,    // AN-PA-L2 Anulacion de Pago Budget
    InternalDebitA = 124,       // NDI-A Debito Interno A (ajustes, inflacion)
    InternalDebitB = 125        // NDI-B Debito Interno B (ajustes, inflacion)
}

/// <summary>
/// Tipo de direccion del cliente.
/// </summary>
public enum AddressType
{
    /// <summary>Direccion fiscal (facturacion)</summary>
    Fiscal = 1,
    
    /// <summary>Direccion de entrega (deposito, sucursal del cliente)</summary>
    Delivery = 2,
    
    /// <summary>Ambas (fiscal y entrega)</summary>
    Both = 3
}

/// <summary>
/// Tipo de forma de pago.
/// </summary>
public enum PaymentMethodType
{
    Cash = 1,       // Efectivo
    Check = 2,      // Cheque
    Transfer = 3,   // Transferencia
    Card = 4,       // Tarjeta
    Barter = 5,     // Trueque (Rezago, Mercaderia)
    Other = 99
}

/// <summary>
/// Tipo de comprobante fiscal (AFIP).
/// </summary>
public enum VoucherType
{
    // Invoices
    InvoiceA = 1,
    InvoiceB = 6,
    
    // Notas de Credito
    CreditNoteA = 3,
    CreditNoteB = 8,
    
    // Notas de Debito
    DebitNoteA = 2,
    DebitNoteB = 7,
    
    // Documentos internos (no fiscales)
    InternalDebitNote = 100,  // NDI - goes to Budget line
    Quote = 101               // Quote - goes to Budget line
}
