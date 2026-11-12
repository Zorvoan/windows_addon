namespace BetterWindows.Monitors;

/// <summary>Obdélník ve FYZICKÝCH pixelech (ne ve WPF jednotkách).</summary>
public readonly record struct PixelRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;
    public int Height => Bottom - Top;
}

/// <summary>
/// Jeden monitor. Okna shellu se vždy vytvářejí "pro monitor", takže podpora
/// více monitorů znamená jen vytvořit víc oken.
/// </summary>
/// <param name="Bounds">Celá plocha monitoru.</param>
/// <param name="WorkArea">Plocha bez hlavního panelu Windows a AppBarů.</param>
/// <param name="Dpi">Efektivní DPI (96 = 100 %, 144 = 150 % ...).</param>
public sealed record MonitorInfo(nint Handle, string DeviceName, PixelRect Bounds, PixelRect WorkArea, uint Dpi, bool IsPrimary)
{
    /// <summary>Měřítko: 1.0 = 100 %, 1.5 = 150 % ...</summary>
    public double Scale => Dpi / 96.0;

    /// <summary>Převod WPF jednotek (DIP) na fyzické pixely tohoto monitoru.</summary>
    public int ToPixels(double dip) => (int)Math.Round(dip * Scale);
}
