using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using SPC.API.Contracts.DeliveryNotes;

namespace SPC.API.Services;

public sealed class DeliveryNotePdfService : IDeliveryNotePdfService
{
    private static readonly string[] CopyLabels = ["ORIGINAL", "DUPLICADO", "TRIPLICADO"];
    private static readonly object FontResolverLock = new();

    public byte[] Create(DeliveryNoteDetailResponse note)
    {
        EnsureFontResolver();
        using var document = new PdfDocument();
        document.Info.Title = $"Remito {note.DeliveryNoteNumber}";
        foreach (var label in CopyLabels)
            DrawCopy(document, note, label);
        using var output = new MemoryStream();
        document.Save(output, closeStream: false);
        return output.ToArray();
    }

    private static void EnsureFontResolver()
    {
        lock (FontResolverLock)
            DeliveryNoteFontResolver.Register();
    }

    private static void DrawCopy(PdfDocument document, DeliveryNoteDetailResponse note, string label)
    {
        var page = document.AddPage();
        var graphics = XGraphics.FromPdfPage(page);
        var regular = new XFont("Carlito", 11);
        var bold = new XFont("Carlito", 11, XFontStyleEx.Bold);
        var title = new XFont("Carlito", 18, XFontStyleEx.Bold);
        var left = 36d;
        var y = 42d;
        graphics.DrawString("REMITO", title, XBrushes.Black, new XRect(left, y, page.Width - 72, 26), XStringFormats.TopLeft);
        graphics.DrawString(label, bold, XBrushes.Black, new XRect(left, y + 28, page.Width - 72, 18), XStringFormats.TopRight);
        y += 64;
        WriteLine(graphics, bold, regular, "Nº", $"{note.PointOfSale:0000}-{note.DeliveryNoteNumber:00000000}", ref y, page);
        WriteLine(graphics, regular, regular, "Fecha", note.DeliveryNoteDate.ToString("dd/MM/yyyy"), ref y, page);
        WriteLine(graphics, regular, regular, "Sucursal", note.BranchName, ref y, page);
        WriteLine(graphics, regular, regular, "Cliente", note.CustomerName, ref y, page);
        if (!string.IsNullOrWhiteSpace(note.CustomerTaxId)) WriteLine(graphics, regular, regular, "CUIT", note.CustomerTaxId, ref y, page);
        if (!string.IsNullOrWhiteSpace(note.DeliveryAddress)) WriteLine(graphics, regular, regular, "Entrega", $"{note.DeliveryAddress} {note.DeliveryCity}".Trim(), ref y, page);
        if (note.InvoiceNumber.HasValue)
            WriteLine(graphics, regular, regular, "Factura origen", $"{note.InvoiceType} {note.OriginalInvoicePointOfSale:0000}-{note.InvoiceNumber:00000000} ({note.OriginalInvoiceBranchName})", ref y, page);
        y += 10;
        graphics.DrawLine(XPens.Black, left, y, page.Width - left, y);
        y += 8;
        graphics.DrawString("Código", bold, XBrushes.Black, new XPoint(left, y));
        graphics.DrawString("Descripción", bold, XBrushes.Black, new XPoint(left + 70, y));
        graphics.DrawString("Cantidad", bold, XBrushes.Black, new XRect(page.Width - 120, y - 12, 84, 18), XStringFormats.TopRight);
        y += 18;
        foreach (var line in note.Details)
        {
            if (y > page.Height - 95)
            {
                page = document.AddPage();
                graphics = XGraphics.FromPdfPage(page);
                y = 42;
                graphics.DrawString($"REMITO {note.DeliveryNoteNumber} — {label} (continuación)", bold, XBrushes.Black, new XPoint(left, y));
                y += 24;
            }
            graphics.DrawString(line.ProductCode, regular, XBrushes.Black, new XPoint(left, y));
            graphics.DrawString(line.ProductDescription, regular, XBrushes.Black, new XRect(left + 70, y - 10, page.Width - 220, 24), XStringFormats.TopLeft);
            graphics.DrawString(line.Quantity.ToString("0.##"), regular, XBrushes.Black, new XRect(page.Width - 120, y - 10, 84, 24), XStringFormats.TopRight);
            y += 20;
        }
        if (!string.IsNullOrWhiteSpace(note.BusinessUnit)) WriteLine(graphics, regular, regular, "Unidad", note.BusinessUnit, ref y, page);
        if (!string.IsNullOrWhiteSpace(note.Clarification)) WriteLine(graphics, regular, regular, "Aclaración", note.Clarification, ref y, page);
        if (!string.IsNullOrWhiteSpace(note.Notes)) WriteLine(graphics, regular, regular, "Observaciones", note.Notes, ref y, page);
        graphics.DrawLine(XPens.Black, left, page.Height - 70, page.Width - left, page.Height - 70);
        graphics.DrawString("Firma y aclaración", regular, XBrushes.Black, new XRect(left, page.Height - 62, page.Width - 72, 24), XStringFormats.TopCenter);
    }

    private static void WriteLine(XGraphics graphics, XFont labelFont, XFont valueFont, string label, string value, ref double y, PdfPage page)
    {
        graphics.DrawString($"{label}:", labelFont, XBrushes.Black, new XPoint(36, y));
        graphics.DrawString(value, valueFont, XBrushes.Black, new XRect(120, y - 10, page.Width - 156, 24), XStringFormats.TopLeft);
        y += 20;
    }
}
