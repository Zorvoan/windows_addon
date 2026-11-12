using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace BetterWindows.Native;

/// <summary>
/// AppBar = okno, které si u Windows "zarezervuje" okraj obrazovky (jako hlavní panel).
/// Maximalizovaná okna pak pod něj nezajedou. Komunikuje se přes SHAppBarMessage.
/// </summary>
internal static class AppBarNative
{
    public static void Register(HWND hwnd, uint callbackMessage)
    {
        var data = Create(hwnd);
        data.uCallbackMessage = callbackMessage;
        PInvoke.SHAppBarMessage(PInvoke.ABM_NEW, ref data);
    }

    /// <summary>Odregistruje AppBar. Bezpečné volat i pro už neexistující okno (po pádu).</summary>
    public static void Remove(HWND hwnd)
    {
        var data = Create(hwnd);
        PInvoke.SHAppBarMessage(PInvoke.ABM_REMOVE, ref data);
    }

    /// <summary>
    /// Požádá o místo u horního okraje. Windows může obdélník upravit (např. když je
    /// nahoře hlavní panel) – vrací skutečně přidělený obdélník.
    /// </summary>
    public static RECT RequestTop(HWND hwnd, RECT monitor, int height)
    {
        var data = Create(hwnd);
        data.uEdge = PInvoke.ABE_TOP;
        data.rc = new RECT { left = monitor.left, top = monitor.top, right = monitor.right, bottom = monitor.top + height };

        // 1. Zeptat se, kde smíme být (Windows posune kvůli jiným AppBarům).
        PInvoke.SHAppBarMessage(PInvoke.ABM_QUERYPOS, ref data);
        // 2. Výšku si vynutíme od přiděleného horního okraje.
        data.rc.bottom = data.rc.top + height;
        // 3. Potvrdit pozici.
        PInvoke.SHAppBarMessage(PInvoke.ABM_SETPOS, ref data);
        return data.rc;
    }

    /// <summary>Stav hlavního panelu (ABS_AUTOHIDE / ABS_ALWAYSONTOP).</summary>
    public static int GetTaskbarState()
    {
        var data = Create(HWND.Null);
        return (int)PInvoke.SHAppBarMessage(PInvoke.ABM_GETSTATE, ref data);
    }

    /// <summary>Nastaví stav hlavního panelu. POZOR: Windows si ho UKLÁDÁ (přežije restart).</summary>
    public static void SetTaskbarState(HWND taskbar, int state)
    {
        var data = Create(taskbar);
        data.lParam = new LPARAM(state);
        PInvoke.SHAppBarMessage(PInvoke.ABM_SETSTATE, ref data);
    }

    private static APPBARDATA Create(HWND hwnd) => new()
    {
        cbSize = (uint)Marshal.SizeOf<APPBARDATA>(),
        hWnd = hwnd,
    };
}
