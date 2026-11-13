using BetterWindows.Monitors;
using Windows.Win32.Graphics.Dwm;

namespace BetterWindows.ShellWindows;

/// <summary>
/// Start / Launchpad (ui/start.html): panel uprostřed obrazovky se systémovým rozmazáním,
/// vyhledáváním a mřížkou aplikací. Na rozdíl od lišty a docku BERE fokus (psaní do hledání).
/// </summary>
internal sealed class StartWindow : ShellWindowBase
{
    private const double WidthDip = 820, HeightDip = 600;

    public StartWindow(ShellContext context, MonitorInfo monitor)
        : base("start", context, monitor)
    {
    }

    protected override bool NoActivate => false;

    /// <summary>Tón rozmazání Startu: průhlednější než systémové (to je v tmavém režimu skoro plná šedá),
    /// ale sytější než lišta, ať je text v mřížce čitelný.</summary>
    protected override (byte A, byte R, byte G, byte B) AccentTint(bool dark) =>
        dark ? ((byte)0x70, (byte)0x14, (byte)0x14, (byte)0x18) : ((byte)0x70, (byte)0xF4, (byte)0xF4, (byte)0xF6);
    protected override DWM_WINDOW_CORNER_PREFERENCE CornerPreference => DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;

    protected override PixelRect ComputeBounds(MonitorInfo m)
    {
        var w = Math.Min(m.ToPixels(WidthDip), m.WorkArea.Width - m.ToPixels(40));
        var h = Math.Min(m.ToPixels(HeightDip), m.WorkArea.Height - m.ToPixels(40));
        var left = m.WorkArea.Left + (m.WorkArea.Width - w) / 2;
        var top = m.WorkArea.Top + (m.WorkArea.Height - h) / 2;
        return new PixelRect(left, top, left + w, top + h);
    }

    /// <summary>Při prvním zobrazení (kvůli načtení WebView2) je okno neviditelné přes prázdný tvar.</summary>
    protected override void OnHandleCreated() => SetHitRects([]);

    public void FocusContent() => Web?.Focus();

}
