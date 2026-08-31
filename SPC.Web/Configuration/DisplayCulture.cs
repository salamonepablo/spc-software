using System.Globalization;

namespace SPC.Web.Configuration;

public static class DisplayCulture
{
    public static CultureInfo Culture { get; } = CultureInfo.GetCultureInfo("es-AR");

    public static void Configure()
    {
        CultureInfo.DefaultThreadCurrentCulture = Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Culture;
    }
}
