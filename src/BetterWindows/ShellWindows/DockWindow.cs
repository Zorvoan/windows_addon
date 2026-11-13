using BetterWindows.Core;
using BetterWindows.Monitors;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace BetterWindows.ShellWindows;

/// <summary>
/// Dock (ui/dock.html) dole uprostřed. Okno je široké přes monitor a vyšší než viditelný dock
/// (kvůli zvětšeným ikonám), ale VIDĚT a KLIKATELNÁ je jen oblast, kterou pošle stránka
/// (SetWindowRgn = tvar okna). Zbytek okna jako by neexistoval – kliknutí jde do oken pod ním.
/// Rozmazané pozadí kreslí samostatné DockBackdropWindow pod viditelným pásem.
/// </summary>
internal sealed class DockWindow : ShellWindowBase
{
    private const double BottomGapDip = 6;

    public DockWindow(ShellContext context, MonitorInfo monitor)
        : base("dock", context, monitor)
    {
    }

    /// <summary>Výška okna: zvětšená ikona + popisek + tečka + okraje.</summary>
    private double HeightDip
    {
        get
        {
            var dock = Context.Settings.Current.Dock;
            var max = dock.Magnification ? dock.MaxScale : 1.0;
            return Math.Ceiling(dock.IconSize * max + 72);
        }
    }

    protected override PixelRect ComputeBounds(MonitorInfo m)
    {
        var height = m.ToPixels(HeightDip);
        // Spodní okraj pracovní plochy: když je vidět hlavní panel Windows, dock bude nad ním.
        var bottom = m.WorkArea.Bottom - m.ToPixels(BottomGapDip);
        return new PixelRect(m.Bounds.Left, bottom - height, m.Bounds.Right, bottom);
    }

    protected override void OnHandleCreated()
    {
        // Než se načte stránka, okno nesmí zabírat nic (jinak by neviditelný pruh blokoval kliky).
        SetHitRects([]);
    }

    public HWND Handle => Hwnd;
}
