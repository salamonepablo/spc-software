using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.DeliveryNotes;
using SPC.API.Data;
using SPC.API.Services;
using SPC.Shared.Models;

namespace SPC.Tests.Unit;

public sealed class DeliveryNoteCommandServiceTests : IDisposable
{
    private readonly SPCDbContext _db;
    private readonly DeliveryNoteCommandService _service;

    public DeliveryNoteCommandServiceTests()
    {
        _db = new SPCDbContext(new DbContextOptionsBuilder<SPCDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        _service = new DeliveryNoteCommandService(_db);
    }

    [Fact]
    public async Task CreateAsync_CopiesInvoiceLinesAndDoesNotChangeInvoice()
    {
        var invoice = new Invoice { Id = 4, BranchId = 2, InvoiceType = "B", PointOfSale = 7,
            InvoiceNumber = 91, CustomerId = 3, Details = [new InvoiceDetail { ItemNumber = 1,
                ProductId = 8, Quantity = 2.5m, UnitPrice = 10m }] };
        _db.Invoices.Add(invoice);
        _db.Branches.AddRange(new Branch { Id = 2, Code = "B2", Name = "Two", IsActive = true },
            new Branch { Id = 9, Code = "B9", Name = "Nine", IsActive = true });
        _db.Customers.Add(new Customer { Id = 3, CompanyName = "Customer", IsActive = true });
        _db.Products.Add(new Product { Id = 8, Code = "P", Description = "Part", IsActive = true });
        await _db.SaveChangesAsync();

        var result = await _service.CreateAsync(new CreateDeliveryNoteRequest {
            BranchId = 9, InvoiceId = 4, IdempotencyKey = "request-1" });

        result.Note.BranchId.Should().Be(9);
        result.Note.InvoiceId.Should().Be(4);
        result.Note.Details.Should().ContainSingle().Which.Quantity.Should().Be(2.5m);
        (_db.DeliveryNotes.Single()).IsInvoiced.Should().BeTrue();
        invoice.BranchId.Should().Be(2);
        invoice.InvoiceNumber.Should().Be(91);
    }

    [Fact]
    public async Task CreateAsync_RejectsVoidedSourceInvoice()
    {
        await SeedInvoiceAsync(isVoided: true);
        var act = () => _service.CreateAsync(new CreateDeliveryNoteRequest { BranchId = 2, InvoiceId = 4, IdempotencyKey = "void-invoice" });
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*anulada*");
        _db.DeliveryNotes.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_ManualNumberKeepsHigherSequenceFloor()
    {
        await SeedStandaloneAsync();
        _db.BranchDeliveryNoteSequences.Add(new BranchDeliveryNoteSequence { BranchId = 1, NextDeliveryNoteNumber = 100 });
        await _db.SaveChangesAsync();
        var request = StandaloneRequest("manual-floor");
        request.RequestedDeliveryNoteNumber = 40;
        var result = await _service.CreateAsync(request);
        result.Note.DeliveryNoteNumber.Should().Be(40);
        (await _db.BranchDeliveryNoteSequences.FindAsync(1))!.NextDeliveryNoteNumber.Should().Be(100);
    }

    [Fact]
    public async Task CreateAsync_InitializesSequenceFromExistingNoteMaximum()
    {
        await SeedStandaloneAsync();
        _db.DeliveryNotes.Add(new DeliveryNote { BranchId = 1, CustomerId = 1, DeliveryNoteNumber = 18 });
        await _db.SaveChangesAsync();
        var request = StandaloneRequest("max-floor");
        request.AdjustStock = false;
        var result = await _service.CreateAsync(request);
        result.Note.DeliveryNoteNumber.Should().Be(19);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateManualNumberBeforeStockDecrement()
    {
        await SeedStandaloneAsync();
        _db.DeliveryNotes.Add(new DeliveryNote { BranchId = 1, CustomerId = 1, DeliveryNoteNumber = 12 });
        await _db.SaveChangesAsync();
        var request = StandaloneRequest("duplicate-number");
        request.RequestedDeliveryNoteNumber = 12;
        var act = () => _service.CreateAsync(request);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*número*");
        (await _db.Stocks.SingleAsync()).Quantity.Should().Be(30m);
    }

    [Fact]
    public async Task CreateAsync_IdempotentRetryReturnsOriginalAndDecrementsStockOnce()
    {
        await SeedStandaloneAsync();
        var request = StandaloneRequest("stock-replay");
        var first = await _service.CreateAsync(request);
        var replay = await _service.CreateAsync(request);
        replay.IsReplay.Should().BeTrue();
        replay.Note.Id.Should().Be(first.Note.Id);
        (await _db.Stocks.SingleAsync()).Quantity.Should().Be(28m);
    }

    [Fact]
    public async Task CreateAsync_RejectsChangedPayloadForSameIdempotencyKey()
    {
        await SeedStandaloneAsync();
        await _service.CreateAsync(StandaloneRequest("changed-payload"));
        var changed = StandaloneRequest("changed-payload");
        changed.Details[0].Quantity = 4m;
        var act = () => _service.CreateAsync(changed);
        await act.Should().ThrowAsync<DeliveryNoteIdempotencyConflictException>();
    }

    [Fact]
    public async Task CreateAsync_StandaloneAdjustsSalesRepWarehouseButInvoiceOriginDoesNot()
    {
        await SeedStandaloneAsync();
        await _service.CreateAsync(StandaloneRequest("stock-deduct"));
        (await _db.Stocks.SingleAsync()).Quantity.Should().Be(28m);
        var invoice = new Invoice { Id = 4, BranchId = 1, InvoiceType = "B", PointOfSale = 1, InvoiceNumber = 91,
            CustomerId = 1, IsVoided = false, Details = [new InvoiceDetail { ItemNumber = 1, ProductId = 8, Quantity = 3m }] };
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        await _service.CreateAsync(new CreateDeliveryNoteRequest { BranchId = 1, InvoiceId = 4, AdjustStock = true, IdempotencyKey = "linked-no-stock" });
        (await _db.Stocks.SingleAsync()).Quantity.Should().Be(28m);
    }

    private async Task SeedInvoiceAsync(bool isVoided)
    {
        _db.Branches.Add(new Branch { Id = 2, Code = "B2", Name = "Two", IsActive = true });
        _db.Customers.Add(new Customer { Id = 3, CompanyName = "Customer", IsActive = true });
        _db.Products.Add(new Product { Id = 8, Code = "P", Description = "Part", IsActive = true });
        _db.Invoices.Add(new Invoice { Id = 4, BranchId = 2, InvoiceType = "B", InvoiceNumber = 91, CustomerId = 3, IsVoided = isVoided,
            Details = [new InvoiceDetail { ItemNumber = 1, ProductId = 8, Quantity = 1 }] });
        await _db.SaveChangesAsync();
    }

    private async Task SeedStandaloneAsync()
    {
        _db.Branches.Add(new Branch { Id = 1, Code = "B1", Name = "One", IsActive = true });
        _db.Customers.Add(new Customer { Id = 1, CompanyName = "Customer", IsActive = true });
        _db.Products.Add(new Product { Id = 8, Code = "P", Description = "Part", IsActive = true });
        _db.SalesReps.Add(new SalesRep { Id = 5, EmployeeCode = "S5", FirstName = "Seller", IsActive = true });
        _db.Warehouses.Add(new Warehouse { Id = 6, Name = "Seller warehouse", AssociatedSalesRepId = 5, IsActive = true });
        _db.Stocks.Add(new Stock { Id = 7, ProductId = 8, WarehouseId = 6, Quantity = 30m });
        await _db.SaveChangesAsync();
    }

    private static CreateDeliveryNoteRequest StandaloneRequest(string key) => new()
    {
        BranchId = 1, CustomerId = 1, SalesRepId = 5, IdempotencyKey = key,
        Details = [new CreateDeliveryNoteDetailRequest { ProductId = 8, Quantity = 2m }]
    };

    public void Dispose() => _db.Dispose();
}
