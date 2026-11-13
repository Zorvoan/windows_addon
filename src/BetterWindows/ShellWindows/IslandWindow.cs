using BetterWindows.Monitors;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BetterWindows.ShellWindows;

/// <summary>
/// Dynamic Island (ui/island.html) – černá "pilulka" nahoře uprostřed, nad horní lištou.
/// Okno je větší (kvůli rozbalení), viditelný je jen tvar od stránky (SetWindowRgn) – jako u docku.
/// </summary>
internal sealed class IslandWindow : ShellWindowBase
{
    // Výška: rozbalená pilulka (4 + 156 px) + místo pro její stín (~34 px).
    private const double WidthDip = 480, HeightDip = 200;

    public IslandWindow(ShellContext context, MonitorInfo monitor)
        : base("island", context, monitor)
    {
    }

    public HWND Handle => Hwnd;

    /// <summary>Poloha okna ostrůvku na obrazovce (fyzické px).</summary>
    public PixelRect Bounds => ComputeBounds(Monitor);

    protected override PixelRect ComputeBounds(MonitorInfo m)
    {
        var w = m.ToPixels(WidthDip);
        var left = m.Bounds.Left + (m.Bounds.Width - w) / 2;
        return new PixelRect(left, m.Bounds.Top, left + w, m.Bounds.Top + m.ToPixels(HeightDip));
    }

    protected override void OnHandleCreated() => SetHitRects([]);

    /// <summary>Znovu navrch (nad horní lištu, která se mohla přesunout nad nás).</summary>
    public void BringToTop()
    {
        if (!Hwnd.IsNull)
            PInvoke.SetWindowPos(Hwnd, HWND.HWND_TOPMOST, 0, 0, 0, 0,
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
    }
}
