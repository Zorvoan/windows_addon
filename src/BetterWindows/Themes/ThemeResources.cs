using System.Windows;
using System.Windows.Media;

namespace BetterWindows.Themes;

/// <summary>Přepne barvy nativních WPF prvků (kontextová nabídka) podle světlého/tmavého režimu.</summary>
internal static class ThemeResources
{
    public static void Apply(bool dark)
    {
        var r = Application.Current.Resources;
        // Barvy z grafitové škály v ui/src/lib/styles/tokens.css (plocha, okraj 8–14 %, tři role textu, akcent).
        Set(r, "Bw.Menu.Background", dark ? "#F519191B" : "#F7FFFFFF");
        Set(r, "Bw.Menu.Border", dark ? "#24EDEDED" : "#24171717");
        Set(r, "Bw.Menu.Foreground", dark ? "#FFEDEDED" : "#FF171717");
        Set(r, "Bw.Menu.Muted", dark ? "#FF85858B" : "#FF737373");
        Set(r, "Bw.Menu.Hover", dark ? "#FF232325" : "#FFF4F4F5");
        Set(r, "Bw.Menu.Separator", dark ? "#14EDEDED" : "#14171717");
    }

    private static void Set(ResourceDictionary r, string key, string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        r[key] = brush;
    }
}
