using System.Diagnostics;
using System.Reflection;
using System.Windows.Threading;
using BetterWindows.Bridge;
using BetterWindows.Logic.Messaging;
using BetterWindows.Logic.Settings;
using BetterWindows.Monitors;
using BetterWindows.Native;
using BetterWindows.Services;
using BetterWindows.ShellWindows;
using BetterWindows.Web;

namespace BetterWindows.Core;

/// <summary>
/// Propojuje služby, okna a zprávy z UI. Každá část se spouští zvlášť přes Safe.Run,
/// takže chyba v jedné (např. počasí) nevypne ostatní.
/// </summary>
internal sealed class ShellController : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly MessageHub _hub;
    private readonly SettingsService _settings;
    private readonly ThemeService _theme;
    private readonly MonitorService _monitors;
    private readonly ShellMessageWindow _shellMessages;
    private readonly ShellContext _context;

    private readonly WallpaperService _wallpaper;
    private readonly DesktopLayerHost _desktop;

    private TopBarWindow? _topBar;
    private SettingsWindow? _settingsWindow;
    private bool _fullscreen;

    public SettingsService Settings => _settings;

    public ShellController(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _hub = new MessageHub(dispatcher);
        _settings = new SettingsService();
        _theme = new ThemeService(dispatcher);
        _theme.SetMode(_settings.Current.Appearance.Theme);
        _monitors = new MonitorService(dispatcher);
        _shellMessages = new ShellMessageWindow();
        _context = new ShellContext(dispatcher, _hub, _monitors, _theme, _settings);

        _wallpaper = new WallpaperService(_settings, dispatcher);
        _wallpaper.Changed += w => _hub.Publish(MessageTypes.WallpaperState, w);
        _hub.Publish(MessageTypes.WallpaperState, _wallpaper.Current);
        _desktop = new DesktopLayerHost(_context);
        _desktop.StatusChanged += () => PublishDiagnostics();

        WebViewHost.SetColorScheme(_theme.IsDark);
        RegisterMessageHandlers();
        PublishSettings();
        PublishDiagnostics();

        _settings.Changed += (old, now) => Safe.Run("Použití nastavení", () => ApplySettings(old, now));
        _theme.Changed += dark => Safe.Run("Změna motivu", () => OnThemeChanged(dark));
        _monitors.Changed += () => Safe.Run("Změna monitorů", OnMonitorsChanged);
        _shellMessages.ExplorerRestarted += () => Safe.Run("Restart Průzkumníka", OnExplorerRestarted);
    }

    public void Start()
    {
        // Plocha první (leží vespod), pak lišta.
        Safe.Run("Vrstva plochy", _desktop.Start);
        Safe.Run("Horní lišta", StartTopBar);
    }

    // ================= Okna =================

    private void StartTopBar()
    {
        _topBar = new TopBarWindow(_context, _monitors.GetPrimary());
        _topBar.SetBackdrop(_settings.Current.Appearance.TopBarBackdrop);
        _topBar.FullscreenChanged += OnFullscreenChanged;
        _topBar.Show();
    }

    public void ShowSettings()
    {
        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow(_hub);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Show();
            _settingsWindow.Activate();
            return;
        }

        if (_settingsWindow.WindowState == System.Windows.WindowState.Minimized)
            _settingsWindow.WindowState = System.Windows.WindowState.Normal;
        _settingsWindow.Activate();
    }

    private void OnFullscreenChanged(bool fullscreen)
    {
        if (_fullscreen == fullscreen)
            return;
        _fullscreen = fullscreen;
        // Hra / video přes celou obrazovku: lišta (a dock) zmizí, aby nepřekážely.
        if (fullscreen)
            _topBar?.Hide();
        else
            _topBar?.Show();
    }

    // ================= Zprávy z UI =================

    private void RegisterMessageHandlers()
    {
        _hub.On<SettingsUpdatePayload>(MessageTypes.SettingsUpdate, (_, p) =>
        {
            if (p?.Settings is not null)
                _settings.Replace(p.Settings);
        });
        _hub.On<object>(MessageTypes.SettingsOpen, (_, _) => ShowSettings());
        _hub.On<SettingsActionPayload>(MessageTypes.SettingsAction, (_, p) =>
        {
            if (p is not null)
                HandleSettingsAction(p);
        });
        _hub.On<UiLogPayload>(MessageTypes.UiLog, (bridge, p) =>
        {
            if (p is null) return;
            var text = $"[UI {bridge.Page}] {p.Message}";
            if (p.Level == "error") Log.Error(text); else if (p.Level == "warn") Log.Warn(text); else Log.Info(text);
        });
    }

    private void HandleSettingsAction(SettingsActionPayload p)
    {
        switch (p.Action)
        {
            case "openLogs":
                Process.Start(new ProcessStartInfo(AppInfo.LogDir) { UseShellExecute = true });
                break;
            case "openDataDir":
                Process.Start(new ProcessStartInfo(AppInfo.DataDir) { UseShellExecute = true });
                break;
            case "restoreSystem":
                RestoreSystem();
                break;
            case "pickWallpaper":
                PickWallpaper();
                break;
            case "clearWallpaper":
                _settings.Update(s => s.Desktop.WallpaperPath = null);
                break;
            default:
                Log.Warn($"Neznámá akce nastavení: {p.Action}");
                break;
        }
    }

    private void PickWallpaper()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Vyber tapetu",
            Filter = "Obrázky|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.gif|Všechny soubory|*.*",
        };
        if (dialog.ShowDialog(_settingsWindow) == true)
            _settings.Update(s => s.Desktop.WallpaperPath = dialog.FileName);
    }

    /// <summary>Nouzové tlačítko: ukázat ikony plochy a hlavní panel a vypnout jejich skrývání.</summary>
    public void RestoreSystem()
    {
        _settings.Update(s =>
        {
            s.Desktop.HideIcons = false;
            s.Taskbar.Hide = false;
        });
        Safe.Run("Ruční obnova ikon", () => ExplorerWindows.SetDesktopIconsVisible(true));
        Safe.Run("Ruční obnova hlavního panelu", () =>
        {
            foreach (var t in ExplorerWindows.FindTaskbars())
                Windows.Win32.PInvoke.ShowWindowAsync(t, Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWNA);
        });
    }

    // ================= Reakce na změny =================

    private void ApplySettings(AppSettings old, AppSettings now)
    {
        PublishSettings();
        if (old.Appearance.Theme != now.Appearance.Theme)
            _theme.SetMode(now.Appearance.Theme);
        if (old.Appearance.TopBarBackdrop != now.Appearance.TopBarBackdrop)
            _topBar?.SetBackdrop(now.Appearance.TopBarBackdrop);
    }

    private void OnThemeChanged(bool dark)
    {
        WebViewHost.SetColorScheme(dark);
        _topBar?.SetDarkMode(dark);
    }

    private void OnMonitorsChanged()
    {
        _topBar?.RefreshMonitor();
        _desktop.OnMonitorsChanged();
    }

    private void OnExplorerRestarted()
    {
        _desktop.OnExplorerRestarted();
        // AppBar je potřeba u nového Průzkumníka zaregistrovat znovu -> lištu vytvoříme znovu.
        if (_topBar is not null)
        {
            _topBar.UnregisterAppBar();
            _topBar.Close();
            _topBar = null;
        }
        _dispatcher.InvokeAsync(() => Safe.Run("Horní lišta (po restartu Průzkumníka)", StartTopBar), DispatcherPriority.Background);
    }

    private void PublishSettings() =>
        _hub.Publish(MessageTypes.SettingsState, new SettingsStatePayload(_settings.Current));

    private void PublishDiagnostics(string? desktopMode = null, string? desktopDetail = null) =>
        _hub.Publish(MessageTypes.Diagnostics, new DiagnosticsPayload(
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?",
            Environment.OSVersion.Version.ToString(),
            AppInfo.LogDir,
            AppInfo.DataDir,
            desktopMode ?? _desktop?.Mode,
            desktopDetail ?? _desktop?.Detail));

    public void Dispose()
    {
        Safe.Run("Zavření nastavení", () => _settingsWindow?.Close());
        Safe.Run("Zavření lišty", () =>
        {
            _topBar?.UnregisterAppBar();
            _topBar?.Close();
        });
        Safe.Run("Zavření plochy", _desktop.Dispose);
        _wallpaper.Dispose();
        _shellMessages.Dispose();
        _theme.Dispose();
        _monitors.Dispose();
    }
}
