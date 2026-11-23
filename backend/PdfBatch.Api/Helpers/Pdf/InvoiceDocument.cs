using PdfBatch.Api.Models.Print;
using System.Globalization;
using System.Net;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using TheArtOfDev.HtmlRenderer.Core.Entities;
using TheArtOfDev.HtmlRenderer.PdfSharp;

namespace PdfBatch.Api.Helpers.Pdf;

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
