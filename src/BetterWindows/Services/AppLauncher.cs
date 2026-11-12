using System.Diagnostics;
using System.IO;
using BetterWindows.Core;
using BetterWindows.Logic.Apps;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BetterWindows.Services;

/// <summary>Spuštění aplikace, přepnutí na okno, minimalizace a zavření.</summary>
internal static class AppLauncher
{
    public static void Launch(string appId)
    {
        var (kind, value) = AppId.Parse(appId);
        ProcessStartInfo info;
        switch (kind)
        {
            case AppIdKind.Exe:
                info = new ProcessStartInfo(value)
                {
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(value) ?? "",
                };
                break;
            case AppIdKind.Aumid:
                // Aplikace z balíčku se spouští přes Shell: shell:AppsFolder\<AUMID>.
                info = new ProcessStartInfo("explorer.exe", "shell:AppsFolder\\" + value) { UseShellExecute = true };
                break;
            default:
                Log.Warn($"Aplikaci {appId} neumím spustit (neznámá identita).");
                return;
        }
        Log.Info($"Spouštím {appId}");
        using var _ = Process.Start(info);
    }

    /// <summary>
    /// Přepne na okno. Windows dovolí přenést okno do popředí jen procesu, který dostal
    /// poslední vstup od uživatele – to je náš dock (klik), takže by to mělo projít.
    /// </summary>
    public static void Activate(long hwndValue)
    {
        var hwnd = new HWND((nint)hwndValue);
        if (PInvoke.IsIconic(hwnd))
            PInvoke.ShowWindowAsync(hwnd, SHOW_WINDOW_CMD.SW_RESTORE);
        if (!PInvoke.SetForegroundWindow(hwnd))
            Log.Warn($"SetForegroundWindow(0x{hwndValue:X}) Windows odmítl (pravidla pro popředí / okno správce).");
    }

    public static void Minimize(IEnumerable<long> hwnds)
    {
        foreach (var h in hwnds)
            PInvoke.ShowWindowAsync(new HWND((nint)h), SHOW_WINDOW_CMD.SW_MINIMIZE);
    }

    /// <summary>Požádá okna o zavření (jako křížek). Aplikace se může zeptat na uložení.</summary>
    public static void Close(IEnumerable<long> hwnds)
    {
        foreach (var h in hwnds)
        {
            if (!PInvoke.PostMessage(new HWND((nint)h), PInvoke.WM_CLOSE, default, default))
                Log.Warn($"WM_CLOSE do okna 0x{h:X} neprošel (okno správce? UIPI).");
        }
    }
}
