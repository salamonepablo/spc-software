extern alias SPCWEB;

using System.Globalization;
using FluentAssertions;
using DisplayCulture = SPCWEB::SPC.Web.Configuration.DisplayCulture;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SPC.Tests.Unit;

public class DisplayCultureTests
{
    [Fact]
    public void Configure_AppliesArgentinianDefaults_AndFormatsImplicitC0Currency()
    {
        // Arrange
        const decimal amount = 1234.56m;
        var originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        var originalDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;

        try
        {
            // Act
            DisplayCulture.Configure();
            var formattedAmount = FormatCurrencyOnNewThread(amount);

            // Assert
            CultureInfo.DefaultThreadCurrentCulture.Should().BeSameAs(DisplayCulture.Culture);
            CultureInfo.DefaultThreadCurrentUICulture.Should().BeSameAs(DisplayCulture.Culture);
            formattedAmount.Should().Be(amount.ToString("C0", DisplayCulture.Culture));
            formattedAmount.Should().Contain("1.235");
            formattedAmount.Should().NotContain(DisplayCulture.Culture.NumberFormat.CurrencyDecimalSeparator);
        }
        finally
        {
            CultureInfo.DefaultThreadCurrentCulture = originalDefaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = originalDefaultUICulture;
        }
    }

    private static string FormatCurrencyOnNewThread(decimal amount)
    {
        string? formattedAmount = null;
        var thread = new Thread(() => formattedAmount = amount.ToString("C0"));

        thread.Start();
        thread.Join();

        return formattedAmount!;
    }
}
