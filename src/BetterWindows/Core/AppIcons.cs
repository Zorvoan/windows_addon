using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BetterWindows.Core;

/// <summary>Vlastní ikona aplikace (Assets/app.ico, generuje tools/make_icon.py).</summary>
public static class AppIcons
{
    private static readonly Uri IconUri = new("pack://application:,,,/Assets/app.ico");

    /// <summary>Ikona pro WPF okna (titulek, hlavní panel).</summary>
    public static ImageSource WindowIcon { get; } = BitmapFrame.Create(IconUri);

    /// <summary>Ikona pro oznamovací oblast. Volající ji musí uvolnit (Dispose).</summary>
    public static System.Drawing.Icon CreateTrayIcon()
    {
        using var stream = Application.GetResourceStream(IconUri)!.Stream;
        // Z .ico vybere obrázek nejbližší velikosti malé ikony pro aktuální DPI.
        return new System.Drawing.Icon(stream, System.Windows.Forms.SystemInformation.SmallIconSize);
    }
}
