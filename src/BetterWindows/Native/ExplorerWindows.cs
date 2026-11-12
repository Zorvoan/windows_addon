using BetterWindows.Core;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BetterWindows.Native;

/// <summary>
/// Hledání oken Průzkumníka (explorer.exe): plocha a hlavní panel.
///
/// Plocha Windows je strom oken:
///  - "Progman" = okno plochy,
///  - "SHELLDLL_DefView" = vrstva s ikonami (uvnitř "SysListView32"),
///  - "WorkerW" = okno, do kterého Windows kreslí tapetu.
/// Klasicky (do Windows 11 23H2) je SHELLDLL_DefView v samostatném WorkerW nejvyšší úrovně.
/// Od Windows 11 24H2 ("vyzdvižená plocha") jsou SHELLDLL_DefView i WorkerW přímo potomky Progman.
///
/// Cizí okna ovládáme jen neblokujícími voláními (ShowWindowAsync, SendMessageTimeout),
/// aby zaseknutý Průzkumník nezasekl i nás.
/// </summary>
internal static class ExplorerWindows
{
    public static HWND FindProgman() => PInvoke.FindWindow("Progman", null);

    /// <summary>Vrstva s ikonami plochy (hledá v Progman i v top-level WorkerW).</summary>
    public static HWND FindDefView()
    {
        var progman = FindProgman();
        if (!progman.IsNull)
        {
            var inProgman = PInvoke.FindWindowEx(progman, HWND.Null, "SHELLDLL_DefView", null);
            if (!inProgman.IsNull)
                return inProgman;
        }

        HWND found = HWND.Null;
        PInvoke.EnumWindows((top, _) =>
        {
            var defView = PInvoke.FindWindowEx(top, HWND.Null, "SHELLDLL_DefView", null);
            if (defView.IsNull)
                return true;
            found = defView;
            return false;
        }, 0);
        return found;
    }

    /// <summary>Skryje / ukáže ikony plochy. Nic se neukládá – restart Průzkumníka ikony vždy vrátí.</summary>
    public static bool SetDesktopIconsVisible(bool visible)
    {
        var defView = FindDefView();
        if (defView.IsNull)
        {
            Log.Warn("Vrstva ikon plochy (SHELLDLL_DefView) nenalezena.");
            return false;
        }
        PInvoke.ShowWindowAsync(defView, visible ? SHOW_WINDOW_CMD.SW_SHOWNA : SHOW_WINDOW_CMD.SW_HIDE);
        Log.Info($"Ikony plochy: {(visible ? "zobrazeny" : "skryty")}");
        return true;
    }

    /// <summary>Hlavní panel na hlavním monitoru + panely na dalších monitorech.</summary>
    public static List<HWND> FindTaskbars()
    {
        var list = new List<HWND>();
        var primary = PInvoke.FindWindow("Shell_TrayWnd", null);
        if (!primary.IsNull)
            list.Add(primary);

        var after = HWND.Null;
        while (true)
        {
            after = PInvoke.FindWindowEx(HWND.Null, after, "Shell_SecondaryTrayWnd", null);
            if (after.IsNull)
                break;
            list.Add(after);
        }
        return list;
    }

    public static string GetClassName(HWND hwnd)
    {
        Span<char> buffer = stackalloc char[256];
        int length;
        unsafe
        {
            fixed (char* p = buffer)
                length = PInvoke.GetClassName(hwnd, p, buffer.Length);
        }
        return length > 0 ? new string(buffer[..length]) : "";
    }
}
