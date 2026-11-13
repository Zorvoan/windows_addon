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
    private readonly AppIdentityResolver _identity = new();
    private readonly WindowTracker _tracker;
    private readonly BatteryService _battery;
    private readonly WeatherService _weather;
    private readonly PrivacyIndicatorService _privacy;
    private readonly IconService _icons = new();
    private readonly DockController _dock;
    private readonly AudioVisualizerService _visualizer;
    private readonly TaskbarService _taskbar;
    private readonly PerformanceService _performance;
    private readonly IslandController _island;
    private readonly Logic.Storage.KeyValueStore _store = new(System.IO.Path.Combine(AppInfo.DataDir, "widgets-data.json"));

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

        _tracker = new WindowTracker(_identity, dispatcher);
        _tracker.ForegroundChanged += () => _hub.Publish(MessageTypes.ForegroundChanged,
            new ForegroundChangedPayload(_tracker.ForegroundAppId, _tracker.ForegroundName, _tracker.ForegroundTitle));
        _battery = new BatteryService(dispatcher);
        _battery.Changed += b => _hub.Publish(MessageTypes.BatteryState, b);
        _weather = new WeatherService(_settings, dispatcher);
        _weather.Changed += w => _hub.Publish(MessageTypes.WeatherState, w);
        _privacy = new PrivacyIndicatorService(dispatcher);
        _privacy.Changed += p => _hub.Publish(MessageTypes.PrivacyState, p);
        _dock = new DockController(_context, _tracker, _identity, _icons);
        _taskbar = new TaskbarService(_settings, dispatcher);
        // Po skrytí/ukázání panelu Windows chvíli přepočítává pracovní plochu -> dock posunout s malým zpožděním.
        _taskbar.WorkAreaChanged += () => DelayOnUi(TimeSpan.FromMilliseconds(600), OnMonitorsChanged);
        _island = new IslandController(_context, _identity);
        _performance = new PerformanceService(_settings, _battery);
        _performance.Changed += OnPerformanceChanged;
        _hub.Publish(MessageTypes.PerformanceState, _performance.State);
        _visualizer = new AudioVisualizerService(_settings);
        _visualizer.Frame += bins => _hub.Publish(MessageTypes.VisualizerFrame, new VisualizerFramePayload(bins), sticky: false, page: "desktop");
        Themes.ThemeResources.Apply(_theme.IsDark);

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
        Safe.Run("Sledování oken", _tracker.Start);
        Safe.Run("Dock", _dock.Start);
        Safe.Run("Dynamic Island", _island.Start);
        Safe.Run("Baterie", _battery.Start);
        Safe.Run("Počasí", _weather.Start);
        Safe.Run("Mikrofon a kamera", _privacy.Start);
        Safe.Run("Vizualizér", () =>
        {
            _visualizer.Start();
            _visualizer.SetPaused(_performance.Reduced);
        });
        Safe.Run("Hlavní panel", _taskbar.Start);
    }

    private void DelayOnUi(TimeSpan delay, Action action)
    {
        var timer = new DispatcherTimer(delay, DispatcherPriority.Background, null, _dispatcher);
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Safe.Run("Odložená akce", action);
        };
        timer.Start();
    }

    // ================= Okna =================

    private void StartTopBar()
    {
        _topBar = new TopBarWindow(_context, _monitors.GetPrimary());
        _topBar.SetBackdrop(_settings.Current.Appearance.TopBarBackdrop);
        _topBar.FullscreenChanged += OnFullscreenChanged;
        _topBar.Placed += () => _island?.BringToTop();
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
        // Hra / video přes celou obrazovku: lišta a dock zmizí, aby nepřekážely.
        if (fullscreen)
            _topBar?.Hide();
        else
            _topBar?.Show();
        _dock.SetFullscreen(fullscreen);
        _island.SetFullscreen(fullscreen);
        _performance.SetFullscreen(fullscreen);
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
        // Data widgetů (např. připomínky).
        _hub.On<StoreGetPayload>(MessageTypes.StoreGet, (bridge, p) =>
        {
            if (p is not null)
                _hub.Send(bridge, MessageTypes.StoreValue, new StoreValuePayload(p.Key, _store.Get(p.Key)));
        });
        _hub.On<StoreSetPayload>(MessageTypes.StoreSet, (_, p) =>
        {
            if (p is null) return;
            _store.Set(p.Key, p.Value);
            // Ostatním stránkám (např. druhý monitor později) poslat novou hodnotu.
            _hub.Publish(MessageTypes.StoreValue, new StoreValuePayload(p.Key, p.Value), sticky: false);
        });
        // Motivy: sdílení přes kód (dekodér v Logic hlídá nedůvěryhodný vstup).
        _hub.On<object>(MessageTypes.ThemeExport, (bridge, _) =>
            _hub.Send(bridge, MessageTypes.ThemeCode, new ThemeCodePayload(
                Logic.Themes.ThemeCodec.Encode(_settings.Current.Appearance.Style, _settings.Current.Dock.IconSize))));
        _hub.On<ThemeImportPayload>(MessageTypes.ThemeImport, (bridge, p) =>
        {
            if (Logic.Themes.ThemeCodec.TryDecode(p?.Code, out var decoded, out var error) && decoded is not null)
            {
                _settings.Update(s =>
                {
                    s.Appearance.Style = decoded.Style;
                    if (decoded.DockIconSize is { } size)
                        s.Dock.IconSize = size;
                });
                Log.Info($"Motiv importován: {decoded.Style.Name}");
                _hub.Send(bridge, MessageTypes.ThemeImportResult, new ThemeImportResultPayload(true, null));
            }
            else
            {
                _hub.Send(bridge, MessageTypes.ThemeImportResult, new ThemeImportResultPayload(false, error));
            }
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
            case "refreshWeather":
                _weather.Refresh();
                break;
            case "clearWallpaper":
                _settings.Update(s => s.Desktop.WallpaperPath = null);
                break;
            case "unpin" when p.Arg is not null:
                _settings.Update(s => s.Dock.Pinned.RemoveAll(x => Logic.Apps.AppId.Equal(x.AppId, p.Arg)));
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
        Safe.Run("Ruční obnova hlavního panelu", _taskbar.ForceShow);
        Safe.Run("Ruční zobrazení oken panelu", () =>
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

    private void OnPerformanceChanged()
    {
        _hub.Publish(MessageTypes.PerformanceState, _performance.State);
        // Vizualizér je nejdražší animace: při ztlumení (baterie, hra) se pozastaví úplně.
        Safe.Run("Vizualizér – pauza", () => _visualizer.SetPaused(_performance.Reduced));
    }

    private void OnThemeChanged(bool dark)
    {
        WebViewHost.SetColorScheme(dark);
        _topBar?.SetDarkMode(dark);
        _dock.SetDarkMode(dark);
        _island.SetDarkMode(dark);
        Themes.ThemeResources.Apply(dark);
    }

    private void OnMonitorsChanged()
    {
        _topBar?.RefreshMonitor();
        _desktop.OnMonitorsChanged();
        _dock.OnMonitorsChanged();
        _island.OnMonitorsChanged();
    }

    private void OnExplorerRestarted()
    {
        _desktop.OnExplorerRestarted();
        DelayOnUi(TimeSpan.FromSeconds(2), _taskbar.OnExplorerRestarted);
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
        Safe.Run("Hlavní panel", _taskbar.Dispose);
        Safe.Run("Dynamic Island", _island.Dispose);
        Safe.Run("Vizualizér", _visualizer.Dispose);
        Safe.Run("Dock", _dock.Dispose);
        Safe.Run("Ikony", _icons.Dispose);
        Safe.Run("Sledování oken", _tracker.Dispose);
        Safe.Run("Baterie", _battery.Dispose);
        Safe.Run("Počasí", _weather.Dispose);
        Safe.Run("Mikrofon a kamera", _privacy.Dispose);
        _wallpaper.Dispose();
        _shellMessages.Dispose();
        _theme.Dispose();
        _monitors.Dispose();
    }
}
