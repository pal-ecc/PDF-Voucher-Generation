namespace PdfBatch.Api.Features.Print;

/// <summary>ZIP payload carried inside ResponseDto.Result.Data; TotalRecords counts PDF entries.</summary>
public sealed record PdfDownloadDto(string FileName, string ContentType, string ContentBase64);
