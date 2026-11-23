using System.Globalization;
using System.IO.Compression;
using Microsoft.Extensions.Logging.Abstractions;
using PdfBatch.Api.Features.Print;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Utils;

GlobalFontSettings.FontResolver = new FontResolver();
var outputDirectory = Path.GetFullPath(args.Length > 0 ? args[0] : ".local/html-renderer-checks");
Directory.CreateDirectory(outputDirectory);
var repository = new DevelopmentInvoiceRepository(TimeProvider.System);
var invoice = (await repository.GetAsync(1, CancellationToken.None))!;
var voucherDocument = new VoucherDocument();
var voucher = new VoucherData("V-" + invoice.InvoiceNo, invoice.Date, "Payment voucher",
    invoice.CustomerName, invoice.InvoiceNo, invoice.GrandTotal, invoice.Currency,
    "Not specified", "Against the referenced invoice.");

CheckPdf("Invoice.pdf", InvoiceDocument.Render(invoice), 595, 842, 1);
CheckPdf("Voucher.pdf", voucherDocument.Render(voucher), 595, 842, 1);
var longInvoice = invoice with
{
    Lines = Enumerable.Range(1, 120).Select(i => new InvoiceLine($"Allocation line {i}: services", 1, i)).ToList()
};
CheckPdf("Invoice-multipage.pdf", InvoiceDocument.Render(longInvoice), 595, 842, null);

var culture = CultureInfo.CurrentCulture;
try
{
    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
    CheckPdf("Voucher-fr-FR.pdf", voucherDocument.Render(voucher), 595, 842, 1);
}
finally { CultureInfo.CurrentCulture = culture; }

// Markup in business data must remain text and must not create clickable PDF annotations.
const string markup = "<a href='https://example.com'>Vendor & Company</a><img src='file:///missing.png'>";
CheckPdf("Voucher-encoded.pdf", voucherDocument.Render(voucher with { PartyName = markup }), 595, 842, 1);
CheckPdf("Invoice-encoded.pdf", InvoiceDocument.Render(invoice with
{
    CustomerName = markup,
    Lines = [new InvoiceLine("<b>Literal description</b> & supplies", 2, 10m)]
}), 595, 842, 1);

var service = new PdfGeneratorService(repository, voucherDocument, NullLogger<PdfGeneratorService>.Instance);
var zip = await service.CreateZipAsync(new PrintRequest(1), CancellationToken.None);
Require(zip is not null, "Single-invoice ZIP must exist.");
using (var archive = new ZipArchive(new MemoryStream(zip!), ZipArchiveMode.Read))
{
    Require(archive.Entries.Select(e => e.FullName).Order().SequenceEqual(new[] { "Invoice-1001.pdf", "Voucher-1001.pdf" }),
        "ZIP must retain invoice and voucher entry names.");
    foreach (var entry in archive.Entries)
    {
        using var content = new MemoryStream();
        using var stream = entry.Open();
        await stream.CopyToAsync(content);
        content.Position = 0;
        using var document = PdfReader.Open(content, PdfDocumentOpenMode.Import);
        Require(document.PageCount == 1, "Each sample PDF must be readable and have one page.");
    }
}
Require(await service.CreateZipAsync(new PrintRequest(999), CancellationToken.None) is null,
    "Missing invoices must keep the no-results contract.");
using var canceled = new CancellationTokenSource();
canceled.Cancel();
try
{
    await service.CreateZipAsync(new PrintRequest(1), canceled.Token);
    throw new InvalidOperationException("Cancellation was ignored.");
}
catch (OperationCanceledException) when (canceled.IsCancellationRequested) { }
Console.WriteLine("PASS: invoice, ECC allocation voucher, multipage rendering, culture, HTML encoding, ZIP contract, missing invoice and cancellation.");
Console.WriteLine("PDF samples: " + outputDirectory);

void CheckPdf(string name, byte[] bytes, double width, double height, int? expectedPages)
{
    Require(bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8), name + " must be a PDF.");
    using var document = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Import);
    Require(expectedPages is null ? document.PageCount > 1 : document.PageCount == expectedPages,
        name + " has an unexpected page count: " + document.PageCount);
    foreach (var page in document.Pages)
    {
        Require(Math.Abs(page.Width.Point - width) < 1 && Math.Abs(page.Height.Point - height) < 1,
            name + " must have A4 dimensions.");
        Require(page.Annotations.Count == 0, name + " must not contain injected links.");
    }
    File.WriteAllBytes(Path.Combine(outputDirectory, name), bytes);
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
