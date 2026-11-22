using System.Globalization;

namespace PdfBatch.Api.Features.Print;

public interface IAmountInWordsFormatter
{
    string Format(decimal amount);
}

/// <summary>International grouping; replace this DI registration for lakh/crore grouping.</summary>
public sealed class InternationalAmountInWordsFormatter : IAmountInWordsFormatter
{
    private static readonly string[] Small = ["zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen"];
    private static readonly string[] Tens = ["", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"];
    private static readonly (decimal Value, string Name)[] Scales = [(1_000_000_000_000_000_000_000_000_000m, "octillion"), (1_000_000_000_000_000_000_000_000m, "septillion"), (1_000_000_000_000_000_000_000m, "sextillion"), (1_000_000_000_000_000_000m, "quintillion"), (1_000_000_000_000_000m, "quadrillion"), (1_000_000_000_000m, "trillion"), (1_000_000_000m, "billion"), (1_000_000m, "million"), (1_000m, "thousand")];

    public string Format(decimal amount)
    {
        var rounded = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        var absolute = decimal.Abs(rounded);
        var whole = decimal.Truncate(absolute);
        var cents = ((absolute - whole) * 100).ToString("00", CultureInfo.InvariantCulture);
        return $"{(rounded < 0 ? "minus " : "")}{Words(whole)} and {cents}/100";
    }

    private static string Words(decimal value)
    {
        if (value < 20) return Small[(int)value];
        if (value < 100) return Tens[(int)(value / 10)] + (value % 10 == 0 ? "" : "-" + Words(value % 10));
        if (value < 1000) return Words(decimal.Truncate(value / 100)) + " hundred" + (value % 100 == 0 ? "" : " " + Words(value % 100));
        foreach (var (scale, name) in Scales)
            if (value >= scale)
                return Words(decimal.Truncate(value / scale)) + " " + name + (value % scale == 0 ? "" : " " + Words(value % scale));
        throw new ArgumentOutOfRangeException(nameof(value));
    }
}
