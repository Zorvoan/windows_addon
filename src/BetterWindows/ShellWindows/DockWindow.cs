using BetterWindows.Core;
using BetterWindows.Logic.Messaging;
using BetterWindows.Monitors;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

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

    /// <summary>Levý horní roh okna na obrazovce (fyzické pixely).</summary>
    public PixelRect ScreenBounds => ComputeBounds(Monitor);

    /// <summary>Převod CSS px (relativně k oknu) na fyzické pixely na obrazovce.</summary>
    public PixelRect ToScreen(CssRect r)
    {
        var b = ScreenBounds;
        var s = Monitor.Scale;
        return new PixelRect(
            b.Left + (int)Math.Floor(r.X * s), b.Top + (int)Math.Floor(r.Y * s),
            b.Left + (int)Math.Ceiling((r.X + r.Width) * s), b.Top + (int)Math.Ceiling((r.Y + r.Height) * s));
    }

    /// <summary>Nastaví tvar okna = sjednocení obdélníků (CSS px relativně k oknu).</summary>
    public unsafe void SetHitRects(IReadOnlyList<CssRect> rects)
    {
        if (Hwnd.IsNull)
            return;
        var s = Monitor.Scale;
        var region = PInvoke.CreateRectRgn(0, 0, 0, 0);
        foreach (var r in rects)
        {
            var part = PInvoke.CreateRectRgn((int)Math.Floor(r.X * s), (int)Math.Floor(r.Y * s),
                (int)Math.Ceiling((r.X + r.Width) * s), (int)Math.Ceiling((r.Y + r.Height) * s));
            PInvoke.CombineRgn(region, region, part, RGN_COMBINE_MODE.RGN_OR);
            PInvoke.DeleteObject(new HGDIOBJ(part.Value));
        }
        // Po úspěchu region patří systému (nemazat). Při neúspěchu ho uklidíme.
        if (PInvoke.SetWindowRgn(Hwnd, region, true) == 0)
            PInvoke.DeleteObject(new HGDIOBJ(region.Value));
    }

    public HWND Handle => Hwnd;
}
