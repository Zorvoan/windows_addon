using BetterWindows.Monitors;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace BetterWindows.Native;

/// <summary>
/// Ověření zprávy ABN_FULLSCREENAPP. Windows ji posílá, i když „přes celou obrazovku“ je jen plocha
/// (klik na plochu = do popředí jde okno Progman/WorkerW velké jako monitor) nebo naše vlastní okno
/// (zamykací obrazovka). Skutečná aplikace přes celou obrazovku = okno v popředí z jiného procesu,
/// které není plocha ani hlavní panel a pokrývá celý monitor.
/// </summary>
internal static class FullscreenCheck
{
    private static readonly HashSet<string> ShellClasses =
    [
        "Progman", "WorkerW", "SHELLDLL_DefView", "Shell_TrayWnd", "Shell_SecondaryTrayWnd",
    ];

    public static unsafe bool IsRealFullscreenApp(PixelRect monitor, out string reason)
    {
        var fg = PInvoke.GetForegroundWindow();
        if (fg.IsNull)
        {
            reason = "žádné okno v popředí";
            return false;
        }

        uint pid = 0;
        PInvoke.GetWindowThreadProcessId(fg, &pid);
        if (pid == (uint)Environment.ProcessId)
        {
            reason = "v popředí je naše okno";
            return false;
        }

        var cls = ExplorerWindows.GetClassName(fg);
        if (ShellClasses.Contains(cls))
        {
            reason = $"v popředí je plocha / hlavní panel ({cls})";
            return false;
        }

        if (!PInvoke.GetWindowRect(fg, out RECT rc))
        {
            reason = "nelze zjistit velikost okna v popředí";
            return false;
        }
        var covers = rc.left <= monitor.Left && rc.top <= monitor.Top && rc.right >= monitor.Right && rc.bottom >= monitor.Bottom;
        reason = covers ? $"okno {cls} pokrývá celý monitor" : $"okno {cls} nepokrývá celý monitor";
        return covers;
    }
}
