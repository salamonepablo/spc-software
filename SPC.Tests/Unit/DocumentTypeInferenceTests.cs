using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.API.Services.CurrentAccount;
using SPC.Shared.Models;

namespace SPC.Tests.Unit;

public class DocumentTypeInferenceTests
{
    [Fact]
    public async Task InferFromDescription_AmbiguousPaymentWithInvoice_ReturnsFactura()
    {
        // "Pago con factura" contains both "pago" and "factura"
        // Factura has higher precedence, so it should win
        using var db = CreateDbContext();
        var resolver = new DocumentTypeResolver(db);

        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.Other,
            DocumentNumber = 1001,
            Description = "Pago con factura"
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("FA", "factura has higher precedence than pago");
    }

    [Fact]
    public async Task InferFromDescription_CreditNoteWithPaymentMention_ReturnsCreditNote()
    {
        // "NC por devolución menos pago" contains "nc" and "pago"
        // Nota de Crédito has higher precedence than Pago
        using var db = CreateDbContext();
        var resolver = new DocumentTypeResolver(db);

        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.Other,
            DocumentNumber = 2001,
            Description = "NC por devolución menos pago"
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("NCA", "nota de crédito has higher precedence than pago");
    }

    [Fact]
    public async Task InferFromDescription_SimplePayment_ReturnsPayment()
    {
        // "Pago transferencia" only contains payment keywords
        using var db = CreateDbContext();
        var resolver = new DocumentTypeResolver(db);

        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.Other,
            DocumentNumber = 3001,
            Description = "Pago transferencia"
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("PG");
    }

    [Fact]
    public async Task InferFromDescription_InvoiceBWithNumber_ReturnsInvoiceB()
    {
        // "Factura B número 1234" should detect both "factura" and "b"
        using var db = CreateDbContext();
        var resolver = new DocumentTypeResolver(db);

        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.Other,
            DocumentNumber = 4001,
            Description = "Factura B número 1234"
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("FB");
    }

    [Fact]
    public async Task InferFromDescription_InvoiceWithTypeA_ReturnsInvoiceA()
    {
        // "Factura tipo A" should detect both "factura" and "tipo a"
        using var db = CreateDbContext();
        var resolver = new DocumentTypeResolver(db);

        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.Other,
            DocumentNumber = 5001,
            Description = "Factura tipo A"
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("FA");
    }

    [Fact]
    public async Task InferFromDescription_EmptyDescription_ReturnsOther()
    {
        // Empty or null description should fall back to OT
        using var db = CreateDbContext();
        var resolver = new DocumentTypeResolver(db);

        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.Other,
            DocumentNumber = 6001,
            Description = ""
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("OT");
    }

    [Fact]
    public async Task InferFromDescription_NullDescription_ReturnsOther()
    {
        // Null description should fall back to OT
        using var db = CreateDbContext();
        var resolver = new DocumentTypeResolver(db);

        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.Other,
            DocumentNumber = 7001,
            Description = null
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("OT");
    }

    [Fact]
    public async Task InferFromDescription_CreditNoteShortForm_ReturnsCreditNoteA()
    {
        // "NC " prefix should be detected
        using var db = CreateDbContext();
        var resolver = new DocumentTypeResolver(db);

        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.Other,
            DocumentNumber = 8001,
            Description = "NC 001234"
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("NCA");
    }

    [Fact]
    public async Task InferFromDescription_DebitNoteShortForm_ReturnsDebitNoteA()
    {
        // "ND " prefix should be detected
        using var db = CreateDbContext();
        var resolver = new DocumentTypeResolver(db);

        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.Other,
            DocumentNumber = 9001,
            Description = "ND 005678"
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("NDA");
    }

    [Fact]
    public async Task InferFromDescription_QuoteKeyword_ReturnsPresupuesto()
    {
        // "quote" should be detected
        using var db = CreateDbContext();
        var resolver = new DocumentTypeResolver(db);

        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.Other,
            DocumentNumber = 10001,
            Description = "Quote for project ABC"
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("PR");
    }

    [Fact]
    public async Task InferFromDescription_FactAbbreviation_ReturnsInvoice()
    {
        // "fact" abbreviation should be detected
        using var db = CreateDbContext();
        var resolver = new DocumentTypeResolver(db);

        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.Other,
            DocumentNumber = 11001,
            Description = "Fact 12345"
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("FA");
    }

    [Fact]
    public async Task InferFromDescription_ReceiptKeyword_ReturnsPayment()
    {
        // "receipt" should map to payment
        using var db = CreateDbContext();
        var resolver = new DocumentTypeResolver(db);

        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.Other,
            DocumentNumber = 12001,
            Description = "Receipt of payment"
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("PG");
    }

    [Fact]
    public async Task InferFromDescription_ComplexMixedDescription_PrioritizesHighestPrecedence()
    {
        // "Pago de factura con nota de crédito aplicada" contains pago, factura, and nota de crédito
        // Factura has highest precedence
        using var db = CreateDbContext();
        var resolver = new DocumentTypeResolver(db);

        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.Other,
            DocumentNumber = 13001,
            Description = "Pago de factura con nota de crédito aplicada"
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("FA", "factura has highest precedence");
    }

    [Fact]
    public async Task InferFromDescription_DebitNoteWithPayment_ReturnsDebitNote()
    {
        // "Nota de débito por pago rechazado" contains debito and pago
        // Nota de Débito has higher precedence than Pago
        using var db = CreateDbContext();
        var resolver = new DocumentTypeResolver(db);

        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.Other,
            DocumentNumber = 14001,
            Description = "Nota de débito por pago rechazado"
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("NDA", "nota de débito has higher precedence than pago");
    }

    [Fact]
    public async Task InferFromDescription_PresupuestoWithPayment_ReturnsPresupuesto()
    {
        // "Presupuesto pendiente de pago" contains presupuesto and pago
        // Presupuesto has higher precedence than Pago
        using var db = CreateDbContext();
        var resolver = new DocumentTypeResolver(db);

        var movement = new CurrentAccountMovement
        {
            CustomerId = 1,
            MovementDate = DateTime.UtcNow,
            DocumentType = DocumentType.Other,
            DocumentNumber = 15001,
            Description = "Presupuesto pendiente de pago"
        };

        var resolved = await resolver.ResolveAsync(movement);

        resolved.ShortCode.Should().Be("PR", "presupuesto has higher precedence than pago");
    }

    private static SPCDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SPCDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new SPCDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
