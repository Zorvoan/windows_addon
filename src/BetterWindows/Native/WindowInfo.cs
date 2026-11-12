using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BetterWindows.Native;

/// <summary>Drobné dotazy na okna (titulek, proces, viditelnost...).</summary>
internal static class WindowInfo
{
    public static string GetTitle(HWND hwnd)
    {
        var length = PInvoke.GetWindowTextLength(hwnd);
        if (length <= 0)
            return "";
        Span<char> buffer = length < 512 ? stackalloc char[length + 1] : new char[length + 1];
        int copied;
        unsafe
        {
            fixed (char* p = buffer)
                copied = PInvoke.GetWindowText(hwnd, p, buffer.Length);
        }
        return new string(buffer[..copied]);
    }

    public static uint GetProcessId(HWND hwnd)
    {
        uint pid;
        unsafe
        {
            PInvoke.GetWindowThreadProcessId(hwnd, &pid);
        }
        return pid;
    }

    /// <summary>
    /// "Cloaked" okno je technicky viditelné, ale DWM ho nekreslí – např. okna na jiné
    /// virtuální ploše nebo uspané UWP aplikace. Takové do docku nepatří.
    /// </summary>
    public static bool IsCloaked(HWND hwnd)
    {
        int cloaked = 0;
        unsafe
        {
            return PInvoke.DwmGetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, &cloaked, sizeof(int)).Succeeded && cloaked != 0;
        }
    }

    /// <summary>
    /// "Skutečné" okno aplikace (jako na hlavním panelu / v Alt+Tab):
    /// viditelné, bez vlastníka, ne nástrojové, ne cloaked, s titulkem.
    /// </summary>
    public static bool IsAppWindow(HWND hwnd)
    {
        if (!PInvoke.IsWindowVisible(hwnd))
            return false;
        if (!PInvoke.GetWindow(hwnd, GET_WINDOW_CMD.GW_OWNER).IsNull)
            return false;
        var ex = WindowStyles.GetExStyle(hwnd);
        if ((ex & WINDOW_EX_STYLE.WS_EX_TOOLWINDOW) != 0 || (ex & WINDOW_EX_STYLE.WS_EX_NOACTIVATE) != 0)
            return false;
        if (PInvoke.GetWindowTextLength(hwnd) <= 0)
            return false;
        if (IsCloaked(hwnd))
            return false;
        var cls = ExplorerWindows.GetClassName(hwnd);
        return cls is not ("Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "Windows.UI.Core.CoreWindow");
    }

    public static bool IsDesktopOrTaskbar(HWND hwnd) =>
        ExplorerWindows.GetClassName(hwnd) is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd";
}
