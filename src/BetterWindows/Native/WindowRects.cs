using BetterWindows.Monitors;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BetterWindows.Native;

/// <summary>Poloha okna na obrazovce v okamžiku minimalizace (animace do ostrůvku / do docku).</summary>
internal static class WindowRects
{
    /// <summary>
    /// Obdélník okna na začátku minimalizace. Okno může být už na cestě do „minimalizované“ polohy
    /// (-32000), pak vezmeme uloženou polohu (u maximalizovaného okna pracovní plochu monitoru).
    /// </summary>
    public static unsafe bool TryGetRestored(HWND hwnd, MonitorService monitors, out PixelRect rect)
    {
        RECT r;
        if (PInvoke.DwmGetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, &r, (uint)sizeof(RECT)).Succeeded
            && r.left > -30000 && r.right - r.left > 50 && r.bottom - r.top > 50)
        {
            rect = new PixelRect(r.left, r.top, r.right, r.bottom);
            return true;
        }

        var placement = new WINDOWPLACEMENT { length = (uint)sizeof(WINDOWPLACEMENT) };
        if (!PInvoke.GetWindowPlacement(hwnd, ref placement))
        {
            rect = default;
            return false;
        }
        // rcNormalPosition je v souřadnicích pracovní plochy (bez lišt) – posunout na obrazovku.
        var n = placement.rcNormalPosition;
        var mon = MonitorFromRect(n, monitors);
        var dx = mon is null ? 0 : mon.WorkArea.Left - mon.Bounds.Left;
        var dy = mon is null ? 0 : mon.WorkArea.Top - mon.Bounds.Top;
        const uint WPF_RESTORETOMAXIMIZED = 0x0002;
        if ((placement.flags & (WINDOWPLACEMENT_FLAGS)WPF_RESTORETOMAXIMIZED) != 0 && mon is not null)
            rect = mon.WorkArea;
        else
            rect = new PixelRect(n.left + dx, n.top + dy, n.right + dx, n.bottom + dy);
        return rect.Width > 50 && rect.Height > 50;
    }

    private static MonitorInfo? MonitorFromRect(RECT r, MonitorService monitors)
    {
        var cx = (r.left + r.right) / 2;
        var cy = (r.top + r.bottom) / 2;
        return monitors.GetAll().FirstOrDefault(m => cx >= m.Bounds.Left && cx < m.Bounds.Right && cy >= m.Bounds.Top && cy < m.Bounds.Bottom);
    }
}
