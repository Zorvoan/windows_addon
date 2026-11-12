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
    private static readonly Queue<DateTime> RecentUiErrors = new();
    private static int _cleanupDone;

    /// <summary>Kolik chyb v UI vlákně během 30 s už bereme jako "aplikace je rozbitá".</summary>
    private const int FatalErrorBurst = 5;

    /// <summary>Musí se zavolat dřív, než vznikne jakékoli WinForms okno (ikona v oznamovací oblasti).</summary>
    public static void Install(Application app)
    {
        // Výjimka v UI vlákně (WPF Dispatcher). Jedna chyba (např. ve widgetu) nesmí shodit
        // celý shell: zalogujeme a běžíme dál. Až když chyby chodí v rychlém sledu,
        // je aplikace zjevně rozbitá -> úklid a řízené ukončení.
        app.DispatcherUnhandledException += (_, e) =>
        {
            e.Handled = true;
            HandleUiException(app, "Neošetřená výjimka v UI vlákně", e.Exception);
        };

        // Kód volaný z WinForms (např. kliknutí v nabídce ikony v oznamovací oblasti) by WPF
        // nezachytil: WinForms má vlastní záchrannou síť a ukázal by svůj dialog
        // "Pokračovat / Ukončit", takže by se nespustil úklid. Přesměrujeme ho na stejnou cestu.
        System.Windows.Forms.Application.SetUnhandledExceptionMode(System.Windows.Forms.UnhandledExceptionMode.CatchException);
        System.Windows.Forms.Application.ThreadException += (_, e) =>
            HandleUiException(app, "Neošetřená výjimka ve WinForms části (oznamovací oblast)", e.Exception);

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

    private static void HandleUiException(Application app, string context, Exception ex)
    {
        Log.Error(context, ex);

        var now = DateTime.UtcNow;
        RecentUiErrors.Enqueue(now);
        while (RecentUiErrors.Count > 0 && now - RecentUiErrors.Peek() > TimeSpan.FromSeconds(30))
            RecentUiErrors.Dequeue();
        if (RecentUiErrors.Count < FatalErrorBurst)
            return;

        Log.Error($"{FatalErrorBurst} chyb během 30 s – aplikace se ukončí.");
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
