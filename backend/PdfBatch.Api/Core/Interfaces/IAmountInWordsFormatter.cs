namespace PdfBatch.Api.Core.Interfaces;

public interface IAmountInWordsFormatter
{
    string Format(decimal amount);
}
