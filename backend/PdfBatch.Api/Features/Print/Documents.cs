using System.Globalization;
using System.Net;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using TheArtOfDev.HtmlRenderer.Core.Entities;
using TheArtOfDev.HtmlRenderer.PdfSharp;

namespace PdfBatch.Api.Features.Print;

public static class InvoiceDocument
{
    public static byte[] Render(InvoiceData invoice)
    {
        string htmlContent = PdfLayout.StartHtml("Invoice", 10) + @"
<table class='pdf-title-row'>
  <tr><td><h2 class='pdf-title'>INVOICE</h2></td></tr>
</table>
<table class='pdf-header-table'>
  <tr>
    <td style='width:50%'>
      <div><span class='pdf-header-label'>Bill to:</span> <span class='pdf-header-value'>" + PdfLayout.Text(invoice.CustomerName) + @"</span></div>
      <div>" + PdfLayout.Text(invoice.CustomerAddress) + @"</div>
    </td>
    <td style='width:50%'>
      <div><span class='pdf-header-label'>Invoice No.:</span> <span class='pdf-header-value'>" + PdfLayout.Text(invoice.InvoiceNo) + @"</span></div>
      <div><span class='pdf-header-label'>Invoice Date:</span> " + PdfLayout.Date(invoice.Date) + @"</div>
      <div><span class='pdf-header-label'>Invoice Currency:</span> " + PdfLayout.Text(invoice.Currency) + @"</div>
    </td>
  </tr>
</table>
<table class='pdf-table-layout' cellspacing='0' cellpadding='0'>
  <thead><tr>
    <th style='width:5%'>#</th><th style='width:55%'>Description</th>
    <th style='width:8%;text-align:right'>Qty</th>
    <th style='width:16%;text-align:right'>Unit price</th>
    <th style='width:16%;text-align:right'>Total</th>
  </tr></thead><tbody>";

        for (var i = 0; i < invoice.Lines.Count; i++)
        {
            var line = invoice.Lines[i];
            htmlContent += @"
  <tr>
    <td>" + (i + 1).ToString(CultureInfo.InvariantCulture) + @"</td>
    <td>" + PdfLayout.Text(line.Description) + @"</td>
    <td style='text-align:right'>" + line.Qty.ToString(CultureInfo.InvariantCulture) + @"</td>
    <td style='text-align:right'>" + Money(line.UnitPrice) + @"</td>
    <td style='text-align:right'>" + Money(line.Total) + @"</td>
  </tr>";
        }

        htmlContent += @"
  </tbody>
</table>
<div class='pdf-totals'>
  Subtotal: " + Money(invoice.SubTotal) + @"<br />
  Tax (" + invoice.TaxRate.ToString("0.##%", CultureInfo.InvariantCulture) + @"): " + Money(invoice.Tax) + @"<br />
  <span class='pdf-bold-font'>Total: " + PdfLayout.Text(invoice.Currency) + " " + Money(invoice.GrandTotal) + @"</span>
</div>
</body>
</html>";
        var config = new PdfGenerateConfig { PageSize = PageSize.A4 };
        config.SetMargins(36);
        return PdfLayout.Render(htmlContent, config, (graphics, page, index, count) =>
            graphics.DrawString($"{index + 1} / {count}", new XFont("Arial", 9), XBrushes.Black,
                new XRect(36, page.Height.Point - 27, page.Width.Point - 72, 12), XStringFormats.Center));
    }

    internal static string Money(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}

internal static class PdfLayout
{
    // Only server-owned markup is rendered. All business values are encoded before concatenation.
    public static string Text(string value) => WebUtility.HtmlEncode(value)
        .Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n')
        .Replace("\n", "<br />", StringComparison.Ordinal);

    public static string Date(DateOnly date) => date.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

    public static string StartHtml(string title, int fontSize) => @"
<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='utf-8' />
<title>" + Text(title) + @"</title>
<style>
  body { margin:0; font-family:Arial; font-size:" + fontSize.ToString(CultureInfo.InvariantCulture) + @"pt; color:#222; }
  table { width:100%; border-collapse:collapse; }
  td, th { vertical-align:top; text-align:left; }
  p { margin:8pt 0 0 0; }
  .pdf-title-row td { padding:3pt 4pt; }
  .pdf-title { margin:0; color:#2978a8; font-size:21pt; }
  .pdf-header-table { margin-top:8pt; }
  .pdf-header-table td { padding:4pt; border:0.5pt solid #d9d9d9; }
  .pdf-header-label, .pdf-header-value { font-weight:bold; }
  .pdf-table-layout { margin-top:10pt; }
  .pdf-table-layout th { color:#2978a8; background-color:#f3f7fa; }
  .pdf-table-layout td, .pdf-table-layout th { padding:4pt; border:0.5pt solid #d9d9d9; }
  .pdf-table-layout tr { page-break-inside:avoid; }
  .pdf-bold-font { font-weight:bold; color:#2978a8; }
  .pdf-totals { text-align:right; margin-top:14pt; page-break-inside:avoid; }
</style>
</head>
<body>";

    public static byte[] Render(string htmlContent, PdfGenerateConfig config,
        Action<XGraphics, PdfPage, int, int>? drawFooter = null)
    {
        using var document = new PdfDocument();
        PdfGenerator.AddPdfPages(document, htmlContent, config,
            stylesheetLoad: BlockStylesheet, imageLoad: BlockImage);
        for (var i = 0; drawFooter is not null && i < document.PageCount; i++)
        {
            var page = document.Pages[i];
            using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            drawFooter(graphics, page, i, document.PageCount);
        }
        using var output = new MemoryStream();
        document.Save(output, closeStream: false);
        return output.ToArray();
    }

    // Templates are text/table only. Prevent accidental filesystem/network resource loading.
    private static void BlockStylesheet(object? sender, HtmlStylesheetLoadEventArgs args) => args.SetStyleSheet = " ";
    private static void BlockImage(object? sender, HtmlImageLoadEventArgs args) => args.Callback();
}
