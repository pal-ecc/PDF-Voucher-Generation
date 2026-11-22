namespace PdfBatch.Api.Features.Print;

public interface IInvoiceRepository
{
    Task<InvoiceData?> GetAsync(int invoiceId, CancellationToken cancellationToken);
    Task<IReadOnlyList<int>> FindIdsAsync(PrintSelection selection, CancellationToken cancellationToken);
}

/// <summary>IN-MEMORY DEVELOPMENT STUB. No database or persistence is configured.</summary>
public sealed class DevelopmentInvoiceRepository(TimeProvider clock) : IInvoiceRepository
{
    private DateOnly Today => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

    public Task<InvoiceData?> GetAsync(int invoiceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        InvoiceData? invoice = invoiceId switch
        {
            1 => Sample("1001", Today),
            2 => Sample("1002", Today),
            3 => Sample("1003", Today.AddMonths(-1)),
            _ => null
        };
        return Task.FromResult(invoice);
    }

    public async Task<IReadOnlyList<int>> FindIdsAsync(PrintSelection selection, CancellationToken cancellationToken)
    {
        var matches = new List<int>();
        foreach (var id in new[] { 1, 2, 3 })
        {
            var invoice = (await GetAsync(id, cancellationToken))!;
            if (selection.Mode switch
            {
                "today" => invoice.Date == Today,
                "voucher" => string.Equals($"V-{invoice.InvoiceNo}", selection.VoucherNo?.Trim(), StringComparison.OrdinalIgnoreCase),
                "month" => invoice.Date.Month == selection.Month && invoice.Date.Year == selection.Year,
                _ => false
            }) matches.Add(id);
        }
        return matches;
    }

    private static InvoiceData Sample(string number, DateOnly date) => new(number, date,
        "Sample Customer", "123 Sample Street\nKathmandu, Nepal",
        [new("Consulting services", 2, 1250.25m), new("Office supplies", 3, 150m)], 0.13m, "NPR");
}
