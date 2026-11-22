using System.Globalization;
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;
using MigraDocCore.Rendering;

namespace PdfBatch.Api.Features.Print;

public static class InvoiceDocument
{
    public static byte[] Render(InvoiceData invoice)
    {
        var document = PdfLayout.Create(10);
        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        PdfLayout.Margins(section, 36);
        PdfLayout.Header(section, "INVOICE", $"Invoice: {invoice.InvoiceNo}\nDate: {invoice.Date:yyyy-MM-dd}", 523, 24);

        var billTo = section.AddParagraph($"Bill to\n{invoice.CustomerName}\n{invoice.CustomerAddress}");
        billTo.Format.SpaceBefore = Unit.FromPoint(20);
        billTo.Format.SpaceAfter = Unit.FromPoint(16);

        var table = section.AddTable();
        table.Rows.LeftIndent = Unit.FromPoint(0);
        table.TopPadding = Unit.FromPoint(6);
        table.BottomPadding = Unit.FromPoint(6);
        foreach (var width in new[] { 25, 288, 40, 85, 85 }) table.AddColumn(Unit.FromPoint(width));
        var header = table.AddRow();
        header.HeadingFormat = true;
        header.Shading.Color = Colors.LightGray;
        header.Format.Font.Bold = true;
        var labels = new[] { "#", "Description", "Qty", "Unit price", "Total" };
        for (var i = 0; i < labels.Length; i++) SetCell(header, i, labels[i]);

        for (var i = 0; i < invoice.Lines.Count; i++)
        {
            var line = invoice.Lines[i];
            var row = table.AddRow();
            row.Borders.Bottom.Width = Unit.FromPoint(0.5);
            row.Borders.Bottom.Color = Colors.LightGray;
            SetCell(row, 0, (i + 1).ToString(CultureInfo.InvariantCulture));
            SetCell(row, 1, line.Description);
            SetCell(row, 2, line.Qty.ToString(CultureInfo.InvariantCulture));
            SetCell(row, 3, Money(line.UnitPrice));
            SetCell(row, 4, Money(line.Total));
        }

        var totals = section.AddParagraph();
        totals.Format.Alignment = ParagraphAlignment.Right;
        totals.Format.SpaceBefore = Unit.FromPoint(16);
        totals.Format.KeepTogether = true;
        totals.AddText($"Subtotal: {Money(invoice.SubTotal)}");
        totals.AddLineBreak();
        totals.AddText($"Tax ({invoice.TaxRate.ToString("0.##%", CultureInfo.InvariantCulture)}): {Money(invoice.Tax)}");
        totals.AddLineBreak();
        totals.AddFormattedText($"Total: {invoice.Currency} {Money(invoice.GrandTotal)}", TextFormat.Bold);

        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Alignment = ParagraphAlignment.Center;
        footer.AddPageField();
        footer.AddText(" / ");
        footer.AddNumPagesField();
        return PdfLayout.Render(document);
    }

    private static void SetCell(Row row, int index, string value)
    {
        row.Cells[index].Format.Alignment = index >= 2 ? ParagraphAlignment.Right : ParagraphAlignment.Left;
        row.Cells[index].AddParagraph(value);
    }

    internal static string Money(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}

public sealed class VoucherDocument(IAmountInWordsFormatter words)
{
    public byte[] Render(VoucherData voucher)
    {
        var document = PdfLayout.Create(11);
        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A5;
        section.PageSetup.Orientation = Orientation.Landscape;
        PdfLayout.Margins(section, 30);
        section.PageSetup.BottomMargin = Unit.FromPoint(60);
        section.PageSetup.FooterDistance = Unit.FromPoint(30);
        PdfLayout.Header(section, voucher.Title.ToUpperInvariant(), $"Voucher: {voucher.VoucherNo}\nDate: {voucher.Date:yyyy-MM-dd}", 535, 18);

        foreach (var (text, bold) in new (string, bool)[]
        {
            ($"Party: {voucher.PartyName}", false),
            ($"Against invoice: {voucher.InvoiceNo}", false),
            ($"Amount: {voucher.Currency} {InvoiceDocument.Money(voucher.Amount)}", true),
            ($"Amount in words: {words.Format(voucher.Amount)}", false),
            ($"Method: {voucher.Method}", false),
            ($"Narration: {voucher.Narration}", false)
        })
        {
            var paragraph = section.AddParagraph(text);
            paragraph.Format.SpaceBefore = Unit.FromPoint(8);
            paragraph.Format.Font.Bold = bold;
        }

        var signatures = section.Footers.Primary.AddTable();
        foreach (var width in new[] { 162, 24, 162, 24, 163 }) signatures.AddColumn(Unit.FromPoint(width));
        var row = signatures.AddRow();
        foreach (var (index, label) in new[] { (0, "Prepared by"), (2, "Approved by"), (4, "Received by") })
        {
            row.Cells[index].Borders.Top.Width = Unit.FromPoint(0.5);
            row.Cells[index].Format.Alignment = ParagraphAlignment.Center;
            row.Cells[index].AddParagraph(label).Format.SpaceBefore = Unit.FromPoint(5);
        }
        return PdfLayout.Render(document);
    }
}

internal static class PdfLayout
{
    public static Document Create(int fontSize)
    {
        var document = new Document();
        var normal = document.Styles[StyleNames.Normal]!;
        normal.Font.Name = "Arial";
        normal.Font.Size = Unit.FromPoint(fontSize);
        return document;
    }

    public static void Margins(Section section, int points)
    {
        section.PageSetup.TopMargin = Unit.FromPoint(points);
        section.PageSetup.BottomMargin = Unit.FromPoint(points);
        section.PageSetup.LeftMargin = Unit.FromPoint(points);
        section.PageSetup.RightMargin = Unit.FromPoint(points);
        section.PageSetup.FooterDistance = Unit.FromPoint(18);
    }

    public static void Header(Section section, string title, string reference, int width, int fontSize)
    {
        var table = section.AddTable();
        table.AddColumn(Unit.FromPoint(width * .6));
        table.AddColumn(Unit.FromPoint(width * .4));
        var row = table.AddRow();
        row.Cells[0].AddParagraph(title).Format.Font.Size = Unit.FromPoint(fontSize);
        row.Cells[0].Format.Font.Bold = true;
        row.Cells[1].Format.Alignment = ParagraphAlignment.Right;
        row.Cells[1].AddParagraph(reference);
    }

    public static byte[] Render(Document document)
    {
        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var output = new MemoryStream();
        using var pdf = renderer.PdfDocument;
        pdf.Save(output, closeStream: false);
        return output.ToArray();
    }
}
