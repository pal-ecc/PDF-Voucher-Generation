using PdfSharpCore.Fonts;

namespace PdfBatch.Api.Fonts;

/// <summary>Explicit, preloaded fonts for cross-platform hosts. No filesystem I/O during rendering.</summary>
public sealed class PdfFontResolver(string regularPath, string boldPath) : IFontResolver
{
    public string DefaultFontName => "Arial";
    private readonly byte[] regular = File.ReadAllBytes(regularPath);
    private readonly byte[] bold = File.ReadAllBytes(boldPath);

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
        new(isBold ? "pdf-bold" : "pdf-regular", false, isItalic);

    public byte[] GetFont(string faceName) => faceName == "pdf-bold" ? bold : regular;
}
