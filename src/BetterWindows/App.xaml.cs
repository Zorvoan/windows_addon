using System.Reflection;
using System.Threading;
using System.Windows;
using BetterWindows.Core;
using BetterWindows.Tray;

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

        _shell = new ShellController(Dispatcher);
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
        _tray.ExitRequested += () => Shutdown(0);
#if DEBUG
        _tray.UiErrorTestRequested += () => throw new InvalidOperationException("Testovací chyba v UI vlákně (aplikace má běžet dál).");
        _tray.CrashTestRequested += () => new Thread(() => throw new InvalidOperationException("Testovací pád procesu.")).Start();
#endif

        _shell.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Safe.Run("Ukončení shellu", () => _shell?.Dispose());
        Safe.Run("Obnova systému při ukončení", () => SystemGuard.RestoreCurrent("ukončení"));
        _tray?.Dispose();
        _singleInstance?.Dispose();
        Log.Info($"Konec (kód {e.ApplicationExitCode}).");
        base.OnExit(e);
    }
}
