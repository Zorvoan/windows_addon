using System.Reflection;
using System.Windows;
using BetterWindows.Core;
using BetterWindows.Tray;
using BetterWindows.ShellWindows;
using Windows.Win32;

namespace BetterWindows;

/// <summary>
/// Vstupní bod aplikace. Tady se vytvářejí a propojují služby a okna ("composition root").
/// </summary>
public partial class App : Application
{
    private SingleInstance? _singleInstance;
    private TrayIcon? _tray;
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log.Init();
        CrashGuard.Install(this);
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        Log.Info($"Start {AppInfo.DisplayName} {version}, Windows {Environment.OSVersion.Version}, PID {Environment.ProcessId}");

        _singleInstance = SingleInstance.TryAcquire();
        if (_singleInstance is null)
        {
            // Povolíme běžící instanci, aby si směla přenést okno do popředí
            // (Windows to jinak cizímu procesu nedovolí a okno by jen blikalo na hlavním panelu).
            PInvoke.AllowSetForegroundWindow(PInvoke.ASFW_ANY);
            Log.Info("Aplikace už běží, předávám řízení běžící instanci a končím.");
            Shutdown(0);
            return;
        }
        // Událost přichází z jiného vlákna -> přesun do UI vlákna přes Dispatcher.
        _singleInstance.SecondInstanceStarted += () => Dispatcher.InvokeAsync(ShowSettings);

        _tray = new TrayIcon();
        _tray.SettingsRequested += ShowSettings;
        _tray.ExitRequested += () => Shutdown(0);
#if DEBUG
        _tray.CrashTestRequested += () => throw new InvalidOperationException("Testovací pád vyvolaný z nabídky.");
#endif
    }

    private void ShowSettings()
    {
        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow();
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Show();
            return;
        }

        if (_settingsWindow.WindowState == WindowState.Minimized)
            _settingsWindow.WindowState = WindowState.Normal;
        _settingsWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _settingsWindow?.Close();
        _tray?.Dispose();
        _singleInstance?.Dispose();
        Log.Info($"Konec (kód {e.ApplicationExitCode}).");
        base.OnExit(e);
    }
}
