namespace PdfBatch.Api.Models.Print;

public sealed record InvoiceLine(string Description, int Qty, decimal UnitPrice)
{
    public decimal Total => Qty * UnitPrice;
}
