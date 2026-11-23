using PdfBatch.Api.Models.Print;

namespace PdfBatch.Api.Core.Interfaces;

public interface IInvoiceRepository
{
    Task<InvoiceData?> GetAsync(int invoiceId, CancellationToken cancellationToken);
    Task<IReadOnlyList<int>> FindIdsAsync(PrintSelection selection, CancellationToken cancellationToken);
}
