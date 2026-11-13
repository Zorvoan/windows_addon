using System.Reflection;
using System.Threading;
using System.Windows;
using BetterWindows.Core;
using BetterWindows.Tray;
using BetterWindows.Logic.Localization;

namespace BetterWindows;

/// <summary>
/// Vstupní bod aplikace: watchdog režim, jedna instance, obnova po pádu, ikona v oznamovací oblasti.
/// Služby a okna propojuje ShellController.
/// </summary>
public partial class App : Application
{
    private SingleInstance? _singleInstance;
    private TrayIcon? _tray;
    private ShellController? _shell;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Režim hlídacího procesu: žádná okna, jen čekat a případně obnovit systém.
        if (e.Args.Contains(Watchdog.Argument))
        {
            Watchdog.Run(e.Args);
            Shutdown(0);
            return;
        }

        Log.Init();
        CrashGuard.Install(this);
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        Log.Info($"Start {AppInfo.DisplayName} {version}, Windows {Environment.OSVersion.Version}, PID {Environment.ProcessId}");

        _singleInstance = SingleInstance.TryAcquire();
        if (_singleInstance is null)
        {
            Log.Info("Aplikace už běží, předávám řízení běžící instanci a končím.");
            Shutdown(0);
            return;
        }

        // Předchozí běh mohl skončit tak, že nestihl nic vrátit (zabití i s watchdogem).
        if (SystemGuard.RestoreFromFile("zbytky po předchozím běhu"))
            Log.Warn("Předchozí běh neskončil čistě – ikony plochy / hlavní panel obnoveny.");

        // Od teď: při pádu vrátit vše, co jsme na systému změnili.
        CrashGuard.RegisterEmergencyCleanup("Obnova systému", () => SystemGuard.RestoreCurrent("nouzový úklid"));
        Watchdog.Launch();

        try
        {
            _shell = new ShellController(Dispatcher);
        }
        catch (Exception ex)
        {
            Log.Error("Spuštění shellu selhalo", ex);
            MessageBox.Show(Loc.T("{app} se nepodařilo spustit:\n{error}\n\nPodrobnosti v logu:\n{log}", ("app", AppInfo.DisplayName), ("error", ex.Message), ("log", Log.CurrentFile)),
                AppInfo.DisplayName, MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }
        // Událost přichází z jiného vlákna -> přesun do UI vlákna přes Dispatcher.
        _singleInstance.SecondInstanceStarted += () => Dispatcher.InvokeAsync(() => _shell.ShowSettings());

        _tray = new TrayIcon();
        _tray.SettingsRequested += _shell.ShowSettings;
        _tray.RestoreSystemRequested += _shell.RestoreSystem;
        _tray.BackdropRequested += mode => _shell.Settings.Update(s =>
        {
            s.Appearance.TopBarBackdrop = mode;
            s.Appearance.DockBackdrop = mode;
        });
        _tray.ShowBackdrop(_shell.Settings.Current.Appearance.TopBarBackdrop);
        _shell.Settings.Changed += (_, now) => _tray.ShowBackdrop(now.Appearance.TopBarBackdrop);
        _tray.ExitRequested += () => Quit(0);
#if DEBUG
        _tray.UiErrorTestRequested += () => throw new InvalidOperationException("Testovací chyba v UI vlákně (aplikace má běžet dál).");
        _tray.CrashTestRequested += () => new Thread(() => throw new InvalidOperationException("Testovací pád procesu.")).Start();
#endif

        _shell.Start();
    }

    /// <summary>Řádné ukončení: nejdřív příznak (okna se pak zavírají „schválně“), pak Shutdown.</summary>
    public static void Quit(int code)
    {
        AppInfo.Exiting = true;
        Current.Shutdown(code);
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        AppInfo.Exiting = true; // odhlášení / vypnutí počítače
        base.OnSessionEnding(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppInfo.Exiting = true;
        Safe.Run("Ukončení shellu", () => _shell?.Dispose());
        Safe.Run("Obnova systému při ukončení", () => SystemGuard.RestoreCurrent("ukončení"));
        _tray?.Dispose();
        _singleInstance?.Dispose();
        Log.Info($"Konec (kód {e.ApplicationExitCode}).");
        base.OnExit(e);
    }
}
