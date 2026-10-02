using System.Reflection;
using PdfSharp.Fonts;

namespace SPC.API.Services;

public sealed class DeliveryNoteFontResolver : IFontResolver
{
    private const string RegularResource = "SPC.API.Assets.Fonts.Carlito-Regular.ttf";
    private const string BoldResource = "SPC.API.Assets.Fonts.Carlito-Bold.ttf";

    public static void Register()
    {
        if (GlobalFontSettings.FontResolver is null)
            GlobalFontSettings.FontResolver = new DeliveryNoteFontResolver();
    }

    public string DefaultFontName => "Carlito";

    public byte[] GetFont(string faceName)
    {
        var resource = faceName switch
        {
            "Carlito#Regular" => RegularResource,
            "Carlito#Bold" => BoldResource,
            _ => throw new ArgumentException($"Unknown font face: {faceName}", nameof(faceName))
        };
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded font resource '{resource}' was not found");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        if (!familyName.Equals("Carlito", StringComparison.OrdinalIgnoreCase) &&
            !familyName.Equals("Arial", StringComparison.OrdinalIgnoreCase) &&
            !familyName.Equals("sans-serif", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Unsupported font family: {familyName}", nameof(familyName));
        return new FontResolverInfo(isBold ? "Carlito#Bold" : "Carlito#Regular");
    }
}
