namespace PdfBatch.Api.Models.Print;

public sealed record VoucherData(string VoucherNo, DateOnly Date, string Title, string PartyName,
    string InvoiceNo, decimal Amount, string Currency, string Method, string Narration);
