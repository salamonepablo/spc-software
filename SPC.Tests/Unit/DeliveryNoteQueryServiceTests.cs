using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.API.Services;
using SPC.Shared.Models;

namespace SPC.Tests.Unit;

public sealed class DeliveryNoteQueryServiceTests
{
    [Fact]
    public async Task SearchAppliesFiltersAndPagingAndGetReturnsNullForMissingRecord()
    {
        var options = new DbContextOptionsBuilder<SPCDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new SPCDbContext(options);
        db.Branches.Add(new Branch { Id = 1, Name = "Branch", Code = "B" });
        db.Customers.Add(new Customer { Id = 1, CompanyName = "Acme" });
        db.Products.Add(new Product { Id = 1, Code = "P", Description = "Product" });
        db.DeliveryNotes.AddRange(
            new DeliveryNote { Id = 1, BranchId = 1, CustomerId = 1, DeliveryNoteNumber = 10, DeliveryNoteDate = new DateTime(2025, 1, 1), Details = [new DeliveryNoteDetail { ProductId = 1, ItemNumber = 1, Quantity = 2 }] },
            new DeliveryNote { Id = 2, BranchId = 1, CustomerId = 1, DeliveryNoteNumber = 11, DeliveryNoteDate = new DateTime(2025, 2, 1), Details = [new DeliveryNoteDetail { ProductId = 1, ItemNumber = 1, Quantity = 1 }] });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var service = new DeliveryNoteQueryService(db);
        var result = await service.SearchAsync(new() { BranchId = 1, FromDate = new DateTime(2025, 2, 1), Page = 1, PageSize = 1 });
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.DeliveryNoteNumber.Should().Be(11);
        var detail = await service.GetByIdAsync(1);
        detail.Should().NotBeNull();
        detail!.BranchName.Should().Be("Branch");
        detail.CustomerName.Should().Be("Acme");
        detail.Details.Should().ContainSingle().Which.ProductDescription.Should().Be("Product");
        (await service.GetByIdAsync(999)).Should().BeNull();
        await FluentActions.Invoking(() => service.SearchAsync(new() { Page = 0 })).Should().ThrowAsync<ArgumentOutOfRangeException>();
        db.ChangeTracker.Entries().Should().BeEmpty();
    }
}
