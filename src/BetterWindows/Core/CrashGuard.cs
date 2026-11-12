using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace BetterWindows.Core;

/// <summary>
/// Zachytí neošetřené výjimky, zapíše je do logu a spustí "nouzový úklid"
/// (např. vrácení hlavního panelu Windows – registruje se v kroku 11).
/// </summary>
public static class CrashGuard
{
    private static readonly List<(string Name, Action Action)> Cleanups = [];
    private static int _cleanupDone;

    /// <summary>Musí se zavolat dřív, než vznikne jakékoli WinForms okno (ikona v oznamovací oblasti).</summary>
    public static void Install(Application app)
    {
        // Výjimka v UI vlákně (WPF Dispatcher). Stav aplikace už nemusí být v pořádku,
        // takže po zalogování aplikaci řízeně ukončíme.
        app.DispatcherUnhandledException += (_, e) =>
        {
            e.Handled = true;
            HandleFatalUiException(app, "Neošetřená výjimka v UI vlákně", e.Exception);
        };

        // Kód volaný z WinForms (např. kliknutí v nabídce ikony v oznamovací oblasti) by WPF
        // nezachytil: WinForms má vlastní záchrannou síť a ukázal by svůj dialog
        // "Pokračovat / Ukončit", takže by se nespustil úklid. Přesměrujeme ho na stejnou cestu.
        System.Windows.Forms.Application.SetUnhandledExceptionMode(System.Windows.Forms.UnhandledExceptionMode.CatchException);
        System.Windows.Forms.Application.ThreadException += (_, e) =>
            HandleFatalUiException(app, "Neošetřená výjimka ve WinForms části (oznamovací oblast)", e.Exception);

        // Výjimka v jakémkoli jiném vlákně. Tady už proces končí a nejde to zastavit,
        // stihneme jen zalogovat a uklidit.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Log.Error($"Neošetřená výjimka (proces končí: {e.IsTerminating})", e.ExceptionObject as Exception);
            RunEmergencyCleanup();
        };

        // Chyba v Tasku, na který nikdo nečekal (await). Proces nepadá, jen zalogujeme.
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Warn("Nepozorovaná výjimka v Tasku", e.Exception);
            e.SetObserved();
        };
    }

    /// <summary>
    /// Zaregistruje akci, která se musí provést i při pádu (např. obnovit hlavní panel).
    /// Akce musí být rychlá a nesmí spoléhat na UI vlákno.
    /// </summary>
    public static void RegisterEmergencyCleanup(string name, Action action)
    {
        lock (Cleanups)
            Cleanups.Add((name, action));
    }

    /// <summary>Spustí všechny úklidové akce. Bezpečné volat víckrát, proběhne jen jednou.</summary>
    public static void RunEmergencyCleanup()
    {
        if (Interlocked.Exchange(ref _cleanupDone, 1) == 1)
            return;

        (string Name, Action Action)[] items;
        lock (Cleanups)
            items = [.. Cleanups];

        foreach (var (name, action) in items)
        {
            try
            {
                action();
                Log.Info($"Nouzový úklid '{name}' proběhl.");
            }
            catch (Exception ex)
            {
                Log.Error($"Nouzový úklid '{name}' selhal", ex);
            }
        }
    }

    private static void HandleFatalUiException(Application app, string context, Exception ex)
    {
        Log.Error(context, ex);
        RunEmergencyCleanup();
        ShowCrashMessage(ex);
        app.Shutdown(1);
    }

    private static void ShowCrashMessage(Exception ex)
    {
        try
        {
            MessageBox.Show(
                $"{AppInfo.DisplayName} narazilo na chybu a ukončí se.\n\n{ex.Message}\n\nPodrobnosti jsou v logu:\n{Log.CurrentFile}",
                AppInfo.DisplayName, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch
        {
            // Když nejde ukázat ani MessageBox, máme aspoň log.
        }
    }
}
