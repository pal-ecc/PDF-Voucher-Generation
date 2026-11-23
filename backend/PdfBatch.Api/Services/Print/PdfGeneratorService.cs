using PdfBatch.Api.Core.Interfaces;
using PdfBatch.Api.Helpers.Pdf;
using PdfBatch.Api.Models.Print;
using System.IO.Compression;

namespace PdfBatch.Api.Services.Print;

public sealed class PdfGeneratorService : IPdfGeneratorService
{
    private readonly IInvoiceRepository _repository;
    private readonly VoucherDocument _voucherDocument;
    private readonly ILogger<PdfGeneratorService> _logger;

    public PdfGeneratorService(IInvoiceRepository repository, VoucherDocument voucherDocument,
        ILogger<PdfGeneratorService> logger)
    {
        _repository = repository;
        _voucherDocument = voucherDocument;
        _logger = logger;
    }

    // No persisted processing/retry log exists in the development repository.
    // Repeated exports intentionally include every matching invoice, including previously exported ones.
    public Task<byte[]?> GeneratePdfForNewVouchersAsync(CancellationToken cancellationToken) =>
        CreateSelectionZipAsync(new PrintSelection { Mode = "today" }, cancellationToken);

    public Task<byte[]?> GeneratePdfForGivenVoucherAsync(string voucherNo, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(voucherNo);
        return CreateSelectionZipAsync(new PrintSelection { Mode = "voucher", VoucherNo = voucherNo.Trim() }, cancellationToken);
    }

    public Task<byte[]?> GeneratePdfVoucherForGivenMonthYearAsync(int month, int year, CancellationToken cancellationToken)
    {
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
        if (year is < 1 or > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        return CreateSelectionZipAsync(new PrintSelection { Mode = "month", Month = month, Year = year }, cancellationToken);
    }

    public Task<byte[]?> CreateZipAsync(PrintRequest request, CancellationToken cancellationToken) =>
        request.InvoiceId > 0 ? CreateAsync([request.InvoiceId], cancellationToken)
            : throw new ArgumentOutOfRangeException(nameof(request), "InvoiceId must be greater than zero.");

    public async Task<byte[]?> CreateSelectionZipAsync(PrintSelection selection, CancellationToken cancellationToken) =>
        await CreateAsync(await _repository.FindIdsAsync(selection, cancellationToken), cancellationToken);

    private async Task<byte[]?> CreateAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ids.Count == 0) return null;
        await using var output = new MemoryStream();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var id in ids)
            {
                var invoice = await _repository.GetAsync(id, cancellationToken);
                if (invoice is null) return null;
                try
                {
                    await GeneratePdfAsyncForVoucher(invoice, AddAsync, cancellationToken);
                    _logger.LogInformation("Generated PDFs for invoice {InvoiceId}", id);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                    _logger.LogError("PDF generation failed for invoice {InvoiceId}", id);
                    // Let the error become a failed ResponseDto. Never report a failed export as success.
                    throw;
                }
            }

            async Task AddAsync(string name, byte[] data)
            {
                var safeName = SanitizeFileName(name);
                var unique = safeName;
                for (var suffix = 2; !names.Add(unique); suffix++)
                    unique = $"{Path.GetFileNameWithoutExtension(safeName)}-{suffix}.pdf";
                var entry = archive.CreateEntry(unique, CompressionLevel.NoCompression);
                await using var stream = entry.Open();
                await stream.WriteAsync(data, cancellationToken);
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
        return output.ToArray();
    }

    private async Task GeneratePdfAsyncForVoucher(InvoiceData invoice, Func<string, byte[], Task> addToZip,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // PdfSharpCore/HTML Renderer rendering is synchronous CPU work; cancellation is checked between
        // documents and passed to the ZIP writes. No blocking async waits or Task.Run wrappers.
        await addToZip($"Invoice-{invoice.InvoiceNo}.pdf", InvoiceDocument.Render(invoice));
        cancellationToken.ThrowIfCancellationRequested();
        var voucher = new VoucherData($"V-{invoice.InvoiceNo}", invoice.Date, "Payment voucher",
            invoice.CustomerName, invoice.InvoiceNo, invoice.GrandTotal, invoice.Currency,
            "Not specified", "Against the referenced invoice.");
        await addToZip($"Voucher-{invoice.InvoiceNo}.pdf", _voucherDocument.Render(voucher));
    }

    public static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        // Include separators and Windows-invalid characters even when hosted on Linux.
        return new string(name.Select(c => invalid.Contains(c) || "<>:\"/\\|?*".Contains(c) || char.IsControl(c) ? '_' : c).ToArray());
    }
}
