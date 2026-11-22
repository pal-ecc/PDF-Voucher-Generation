using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using PdfBatch.Api.Contracts;

namespace PdfBatch.Api.Features.Print;

[ApiController]
[Route("api/pdf")]
[Produces("application/json")]
public sealed class PdfController(IPdfGeneratorService service, TimeProvider clock) : ControllerBase
{
    [HttpPost("batch")]
    [ProducesResponseType<ResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ResponseDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ResponseDto>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ResponseDto>(StatusCodes.Status500InternalServerError)]
    public async Task<ResponseDto> Batch([FromBody] PrintRequest request, CancellationToken cancellationToken)
    {
        if (request.InvoiceId <= 0)
            return CreateResponse(StatusCodes.Status400BadRequest, "InvoiceId must be greater than zero.");
        return ZipResult(await service.CreateZipAsync(request, cancellationToken));
    }

    [HttpPost("batch/selection")]
    [ProducesResponseType<ResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ResponseDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ResponseDto>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ResponseDto>(StatusCodes.Status500InternalServerError)]
    public async Task<ResponseDto> Selection([FromBody] PrintSelection selection, CancellationToken cancellationToken)
    {
        // ApiController validates the selection before invoking the action.
        var zip = selection.Mode switch
        {
            "today" => await service.GeneratePdfForNewVouchersAsync(cancellationToken),
            "voucher" => await service.GeneratePdfForGivenVoucherAsync(selection.VoucherNo!, cancellationToken),
            "month" => await service.GeneratePdfVoucherForGivenMonthYearAsync(selection.Month!.Value, selection.Year!.Value, cancellationToken),
            _ => null
        };
        return ZipResult(zip);
    }

    private ResponseDto ZipResult(byte[]? zip)
    {
        if (zip is null)
            return CreateResponse(StatusCodes.Status404NotFound, "No invoices match the requested selection.");

        using var stream = new MemoryStream(zip, writable: false);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var fileName = $"documents-{clock.GetUtcNow():yyyyMMdd-HHmmss}.zip";
        return CreateResponse(StatusCodes.Status200OK, "PDFs generated successfully.", new ResultDto
        {
            Data = new PdfDownloadDto(fileName, "application/zip", Convert.ToBase64String(zip)),
            TotalRecords = archive.Entries.Count(entry => entry.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        });
    }

    private ResponseDto CreateResponse(int status, string message, ResultDto? result = null)
    {
        // Keep the HTTP status and the application's Status field consistent.
        Response.StatusCode = status;
        return new ResponseDto { Status = status, Message = message, Result = result };
    }
}
