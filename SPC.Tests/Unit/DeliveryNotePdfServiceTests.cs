using FluentAssertions;
using PdfSharp.Pdf.IO;
using SPC.API.Contracts.DeliveryNotes;
using SPC.API.Services;

namespace SPC.Tests.Unit;

public sealed class DeliveryNotePdfServiceTests
{
    [Fact]
    public async Task CreatesReadablePdfWithThreeCopyStartPages()
    {
        var note = new DeliveryNoteDetailResponse
        {
            Id = 1, BranchId = 1, DeliveryNoteNumber = 42, DeliveryNoteDate = DateTime.Today,
            CustomerId = 2, CustomerName = "Acme", BranchName = "Central",
            Details = [new() { ProductId = 5, ProductDescription = "Battery", Quantity = 1 }]
        };
        var bytes = new DeliveryNotePdfService().Create(note);
        using var stream = new MemoryStream(bytes);
        using var document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        document.PageCount.Should().BeGreaterThanOrEqualTo(3);
    }
}
