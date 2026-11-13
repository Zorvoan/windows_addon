using BetterWindows.Core;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace BetterWindows.Native;

/// <summary>
/// Přenesení vlastního okna do popředí (nabídka Start potřebuje klávesnici).
///
/// Windows dovolí SetForegroundWindow jen procesu, který dostal poslední vstup od uživatele.
/// Při otevření klávesou Win to nejsme my (klávesu jen sledujeme háčkem), takže Windows
/// odmítne. OBEZLIČKA (vědomá, popsaná v TESTING.md): na chvíli "propojíme" vstupní frontu
/// našeho vlákna s vláknem okna v popředí (AttachThreadInput) – pak smíme fokus převzít.
/// Výsledek se vždy ověřuje; když selže, volající to musí řešit (Start se zavře).
/// </summary>
internal static class ForegroundHelper
{
    public static unsafe bool BringToForeground(HWND hwnd)
    {
        if (PInvoke.SetForegroundWindow(hwnd) && PInvoke.GetForegroundWindow() == hwnd)
            return true;

        var foreground = PInvoke.GetForegroundWindow();
        var foregroundThread = foreground.IsNull ? 0 : PInvoke.GetWindowThreadProcessId(foreground, null);
        var ourThread = PInvoke.GetCurrentThreadId();
        var attached = foregroundThread != 0 && foregroundThread != ourThread &&
                       PInvoke.AttachThreadInput(ourThread, foregroundThread, true);
        try
        {
            PInvoke.BringWindowToTop(hwnd);
            PInvoke.SetForegroundWindow(hwnd);
        }
        finally
        {
            if (attached)
                PInvoke.AttachThreadInput(ourThread, foregroundThread, false);
        }

        var ok = PInvoke.GetForegroundWindow() == hwnd;
        if (!ok)
            Log.Warn("Okno se nepodařilo přenést do popředí (Windows odmítl).");
        return ok;
    }
}
