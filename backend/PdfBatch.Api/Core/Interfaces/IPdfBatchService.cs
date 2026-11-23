using PdfBatch.Api.Models.Print;

namespace PdfBatch.Api.Core.Interfaces;

public interface IPdfBatchService
{
    Task<byte[]?> CreateZipAsync(PrintRequest request, CancellationToken cancellationToken);
    Task<byte[]?> CreateSelectionZipAsync(PrintSelection selection, CancellationToken cancellationToken);
}
