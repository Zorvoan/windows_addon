using System.Windows;
using System.Windows.Media;

namespace BetterWindows.Themes;

/// <summary>Přepne barvy nativních WPF prvků (kontextová nabídka) podle světlého/tmavého režimu.</summary>
internal static class ThemeResources
{
    public static void Apply(bool dark)
    {
        var r = Application.Current.Resources;
        Set(r, "Bw.Menu.Background", dark ? "#F2202024" : "#F7FAFAFC");
        Set(r, "Bw.Menu.Border", dark ? "#33FFFFFF" : "#1A000000");
        Set(r, "Bw.Menu.Foreground", dark ? "#FFF5F5F7" : "#FF141418");
        Set(r, "Bw.Menu.Muted", dark ? "#99F5F5F7" : "#99141418");
        Set(r, "Bw.Menu.Hover", dark ? "#FF4A78F5" : "#FF3A6DF0");
        Set(r, "Bw.Menu.Separator", dark ? "#22FFFFFF" : "#1A000000");
    }

    private static void Set(ResourceDictionary r, string key, string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        r[key] = brush;
    }
}
