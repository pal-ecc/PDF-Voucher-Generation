namespace PdfBatch.Api.Models;

public class ResultDto
{
    public object? Data { get; set; }
    public int? TotalRecords { get; set; }
}

public class ResponseDto
{
    public ResultDto? Result { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Status { get; set; }
}
