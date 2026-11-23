namespace PdfBatch.Api.Models.Print;

public sealed record InvoiceData(string InvoiceNo, DateOnly Date, string CustomerName,
    string CustomerAddress, List<InvoiceLine> Lines, decimal TaxRate, string Currency)
{
    public decimal SubTotal => Lines.Sum(line => line.Total);
    public decimal Tax => decimal.Round(SubTotal * TaxRate, 2, MidpointRounding.AwayFromZero);
    public decimal GrandTotal => SubTotal + Tax;
}
