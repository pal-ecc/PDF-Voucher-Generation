namespace PdfBatch.Api.Core.Interfaces;

/// <summary>
/// Application entry points for the three export cases. Returns a ZIP, or null when no invoice matches.
/// The development repository has no generation history, so today's export includes all today's invoices.
/// </summary>
public interface IPdfGeneratorService : IPdfBatchService
{
    Task<byte[]?> GeneratePdfForNewVouchersAsync(CancellationToken cancellationToken);
    Task<byte[]?> GeneratePdfForGivenVoucherAsync(string voucherNo, CancellationToken cancellationToken);
    Task<byte[]?> GeneratePdfVoucherForGivenMonthYearAsync(int month, int year, CancellationToken cancellationToken);
}
