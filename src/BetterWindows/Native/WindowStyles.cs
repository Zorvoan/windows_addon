using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BetterWindows.Native;

/// <summary>Úpravy rozšířených stylů okna (WS_EX_*).</summary>
internal static class WindowStyles
{
    /// <summary>
    /// Udělá z okna "panel shellu":
    ///  - WS_EX_TOOLWINDOW: okno není v Alt+Tab ani na hlavním panelu,
    ///  - WS_EX_NOACTIVATE: kliknutí na okno nevezme fokus aplikaci, se kterou zrovna pracuješ.
    /// </summary>
    public static void MakeShellPanel(HWND hwnd)
    {
        var style = (WINDOW_EX_STYLE)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        style |= WINDOW_EX_STYLE.WS_EX_TOOLWINDOW | WINDOW_EX_STYLE.WS_EX_NOACTIVATE;
        style &= ~WINDOW_EX_STYLE.WS_EX_APPWINDOW;
        PInvoke.SetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (int)style);
    }
}
