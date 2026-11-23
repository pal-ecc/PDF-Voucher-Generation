using System.Globalization;
using System.Net;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using TheArtOfDev.HtmlRenderer.Core.Entities;
using TheArtOfDev.HtmlRenderer.PdfSharp;

namespace PdfBatch.Api.Helpers.Pdf;

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
