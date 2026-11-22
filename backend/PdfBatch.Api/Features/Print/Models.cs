using System.ComponentModel.DataAnnotations;

namespace PdfBatch.Api.Features.Print;

public sealed record PrintRequest(int InvoiceId);

// A separate endpoint preserves the single-invoice contract while supporting the three batch filters.
public sealed record PrintSelection : IValidatableObject
{
    public required string Mode { get; init; }
    [StringLength(100)] public string? VoucherNo { get; init; }
    public int? Month { get; init; }
    public int? Year { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Mode is not ("today" or "voucher" or "month"))
            yield return new("Mode must be today, voucher, or month.", [nameof(Mode)]);
        if (Mode == "voucher" && string.IsNullOrWhiteSpace(VoucherNo))
            yield return new("A voucher number is required.", [nameof(VoucherNo)]);
        if (Mode == "month" && (Month is null or < 1 or > 12 || Year is null or < 1 or > 9999))
            yield return new("A valid month and year are required.", [nameof(Month), nameof(Year)]);
    }
}

public sealed record InvoiceLine(string Description, int Qty, decimal UnitPrice)
{
    public decimal Total => Qty * UnitPrice;
}

public sealed record InvoiceData(string InvoiceNo, DateOnly Date, string CustomerName,
    string CustomerAddress, List<InvoiceLine> Lines, decimal TaxRate, string Currency)
{
    public decimal SubTotal => Lines.Sum(line => line.Total);
    public decimal Tax => decimal.Round(SubTotal * TaxRate, 2, MidpointRounding.AwayFromZero);
    public decimal GrandTotal => SubTotal + Tax;
}

public sealed record VoucherData(string VoucherNo, DateOnly Date, string Title, string PartyName,
    string InvoiceNo, decimal Amount, string Currency, string Method, string Narration);
