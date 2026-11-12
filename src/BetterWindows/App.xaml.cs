using System.Reflection;
using System.Windows;
using BetterWindows.Core;
using BetterWindows.Monitors;
using BetterWindows.Native;
using BetterWindows.Services;
using BetterWindows.ShellWindows;
using BetterWindows.Tray;
using BetterWindows.Web;

namespace BetterWindows;

/// <summary>
/// Vstupní bod aplikace. Tady se vytvářejí a propojují služby a okna ("composition root").
/// </summary>
public partial class App : Application
{
    private SingleInstance? _singleInstance;
    private TrayIcon? _tray;
    private SettingsWindow? _settingsWindow;
    private MonitorService? _monitors;
    private ThemeService? _theme;
    private TopBarWindow? _topBar;

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
            Log.Info("Aplikace už běží, předávám řízení běžící instanci a končím.");
            Shutdown(0);
            return;
        }
        // Událost přichází z jiného vlákna -> přesun do UI vlákna přes Dispatcher.
        _singleInstance.SecondInstanceStarted += () => Dispatcher.InvokeAsync(ShowSettings);

        // --- Služby ---
        _monitors = new MonitorService(Dispatcher);
        _theme = new ThemeService(Dispatcher);
        WebViewHost.SetColorScheme(_theme.IsDark);

        // --- Okna shellu (zatím jen hlavní monitor) ---
        _topBar = new TopBarWindow(_monitors, _monitors.GetPrimary(), _theme.IsDark);
        _topBar.Show();

        _theme.Changed += dark =>
        {
            WebViewHost.SetColorScheme(dark);
            _topBar.SetDarkMode(dark);
        };
        _monitors.Changed += () => _topBar.RefreshMonitor();

        // --- Ikona v oznamovací oblasti ---
        _tray = new TrayIcon();
        _tray.SettingsRequested += ShowSettings;
        _tray.ExitRequested += () => Shutdown(0);
        _tray.ShowBackdrop(BackdropKind.SystemAcrylic);
#if DEBUG
        _tray.CrashTestRequested += () => throw new InvalidOperationException("Testovací pád vyvolaný z nabídky.");
        _tray.BackdropRequested += kind =>
        {
            _topBar.SetBackdrop(kind);
            _tray.ShowBackdrop(kind);
        };
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
        _topBar?.Close();
        _tray?.Dispose();
        _theme?.Dispose();
        _monitors?.Dispose();
        _singleInstance?.Dispose();
        Log.Info($"Konec (kód {e.ApplicationExitCode}).");
        base.OnExit(e);
    }
}
