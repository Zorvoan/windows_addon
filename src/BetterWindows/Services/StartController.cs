using System.Windows.Threading;
using BetterWindows.Core;
using BetterWindows.Logic.Apps;
using BetterWindows.Logic.Messaging;
using BetterWindows.Logic.Settings;
using BetterWindows.Native;
using BetterWindows.ShellWindows;

namespace BetterWindows.Services;

/// <summary>
/// Start / Launchpad: okno, seznam aplikací, spouštění, připnutí a (volitelně) klávesa Windows.
/// Okno se vytvoří hned po startu neviditelně (načte se stránka), pak se už jen ukazuje / schovává.
/// </summary>
internal sealed class StartController : IDisposable
{
    private readonly ShellContext _context;
    private readonly StartAppsService _apps = new();
    private WinKeyHook? _hook;
    private StartWindow? _window;
    private bool _pageReady;
    private bool _open;
    private bool _fullscreen;
    private DateTime _openedAt;

    /// <summary>
    /// Zavírací animace běží ve stránce (obsah zmizí za ~140 ms); okno se schová až po ní.
    /// Časovač vlastní C# – na odpověď stránky se nečeká, takže se zavření nikdy nezasekne.
    /// </summary>
    private static readonly TimeSpan HideDelay = TimeSpan.FromMilliseconds(170);
    private DispatcherTimer? _hideTimer;

    public StartController(ShellContext context)
    {
        _context = context;
        _apps.Loaded += apps => _context.Hub.Publish(MessageTypes.StartApps,
            new StartAppsPayload(apps.Select(a => new StartAppItem(a.AppId, a.Name)).ToList()));

        var hub = context.Hub;
        hub.On<UiReadyPayload>(MessageTypes.UiReady, (bridge, _) =>
        {
            if (bridge.Page != "start" || _pageReady)
                return;
            _pageReady = true;
            // První načtení proběhlo v neviditelném okně – teď ho opravdu schovat.
            _window?.Hide();
            _window?.ClearRegion();
            UpdateHook();
        });
        hub.On<object>(MessageTypes.StartToggle, (_, _) => Toggle(fromWinKey: false));
        hub.On<object>(MessageTypes.StartClose, (_, _) => Close());
        hub.On<StartLaunchPayload>(MessageTypes.StartLaunch, (_, p) =>
        {
            if (p is null) return;
            // Nejdřív spustit (ještě máme popředí, nová aplikace ho tak smí převzít), pak zavřít.
            Safe.Run("Start – spuštění", () => AppLauncher.Launch(p.AppId));
            Close();
        });
        hub.On<StartPinPayload>(MessageTypes.StartPin, (_, p) =>
        {
            if (p is null) return;
            var name = _apps.Apps.FirstOrDefault(a => AppId.Equal(a.AppId, p.AppId))?.Name ?? p.AppId;
            _context.Settings.Update(s =>
            {
                if (!s.Dock.Pinned.Any(x => AppId.Equal(x.AppId, p.AppId)))
                    s.Dock.Pinned.Add(new PinnedApp { AppId = p.AppId, Name = name });
            });
        });
        _context.Settings.Changed += OnSettingsChanged;
    }

    public void Start()
    {
        if (!_context.Settings.Current.Start.Enabled)
            return;
        _apps.RefreshIfStale();
        CreateWindow();
        UpdateHook();
    }

    private void CreateWindow()
    {
        if (_window is not null)
            return;
        _pageReady = false;
        _window = new StartWindow(_context, _context.Monitors.GetPrimary());
        // Vlastní tón (AccentAcrylic): systémové rozmazání Windows je v tmavém režimu skoro neprůhledná šedá.
        _window.SetBackdrop(_context.Settings.Current.Appearance.DockBackdrop == BackdropMode.None ? BackdropMode.None : BackdropMode.AccentAcrylic);
        _window.Deactivated += (_, _) =>
        {
            // Klik mimo Start = zavřít (ale ne hned při otevírání, kdy se fokus teprve přenáší).
            if (_open && DateTime.UtcNow - _openedAt > TimeSpan.FromMilliseconds(400))
                Close();
        };
        _window.Show(); // neviditelné (prázdný tvar), jen kvůli načtení stránky
    }

    public void Toggle(bool fromWinKey)
    {
        if (_open)
        {
            Close();
            return;
        }
        if (_window is null || !_pageReady || _fullscreen)
        {
            if (fromWinKey)
                WinKeyHook.OpenWindowsStart(); // náš Start není připravený -> Start Windows
            return;
        }

        _apps.RefreshIfStale();
        _hideTimer?.Stop(); // otevření během zavírací animace: okno nechat, jen zrušit schování
        _open = true;
        _openedAt = DateTime.UtcNow;
        _window.RefreshMonitor();
        _window.ClearRegion();
        _window.Show();
        var focused = ForegroundHelper.BringToForeground(_window.Handle());
        _window.FocusContent();
        _context.Hub.Publish(MessageTypes.StartVisibility, new StartVisibilityPayload(true), sticky: false, page: "start");

        if (!focused)
        {
            // Bez fokusu by nešlo psát – raději zavřít a otevřít Start Windows.
            Log.Warn("Start: nepodařilo se získat fokus, otevírám Start Windows.");
            Close(immediate: true);
            if (fromWinKey)
                WinKeyHook.OpenWindowsStart();
        }
    }

    /// <param name="immediate">Schovat hned, bez animace (celá obrazovka, vypnutí, ukončení).</param>
    public void Close(bool immediate = false)
    {
        if (!_open)
            return;
        _open = false;
        _context.Hub.Publish(MessageTypes.StartVisibility, new StartVisibilityPayload(false), sticky: false, page: "start");
        if (immediate || _window is null)
        {
            HideNow();
            return;
        }
        if (_hideTimer is null)
        {
            _hideTimer = new DispatcherTimer(DispatcherPriority.Normal, _context.Dispatcher) { Interval = HideDelay };
            _hideTimer.Tick += (_, _) => Safe.Run("Start – schování", HideNow);
        }
        _hideTimer.Stop();
        _hideTimer.Start();
    }

    private void HideNow()
    {
        _hideTimer?.Stop();
        if (!_open) // mezitím znovu otevřený -> neschovávat
            _window?.Hide();
    }

    public void SetFullscreen(bool fullscreen)
    {
        _fullscreen = fullscreen;
        if (fullscreen) Close(immediate: true);
        UpdateHook();
    }

    public void SetDarkMode(bool dark) => _window?.SetDarkMode(dark);

    /// <summary>Háček klávesy Win jen když je to zapnuté; převezme klávesu jen když je Start připravený.</summary>
    private void UpdateHook()
    {
        var s = _context.Settings.Current.Start;
        var want = s.Enabled && s.ReplaceWinKey;
        if (want && _hook is null)
        {
            _hook = new WinKeyHook();
            _hook.Tapped += () => _context.Dispatcher.BeginInvoke(() => Safe.Run("Start – klávesa Win", () => Toggle(fromWinKey: true)));
            _hook.Start();
        }
        else if (!want && _hook is not null)
        {
            _hook.Dispose();
            _hook = null;
        }
        if (_hook is not null)
            _hook.Enabled = _pageReady && !_fullscreen && _window is not null;
    }

    private void OnSettingsChanged(AppSettings old, AppSettings now)
    {
        if (old.Start.Enabled != now.Start.Enabled)
        {
            if (now.Start.Enabled) Start();
            else
            {
                Close(immediate: true);
                Safe.Run("Zavření Startu", () => _window?.Close());
                _window = null;
                _pageReady = false;
            }
        }
        UpdateHook();
    }

    public void Dispose()
    {
        _context.Settings.Changed -= OnSettingsChanged;
        _hideTimer?.Stop();
        _hook?.Dispose();
        Safe.Run("Zavření Startu", () => _window?.Close());
    }
}

internal static class StartWindowExtensions
{
    public static Windows.Win32.Foundation.HWND Handle(this StartWindow w) =>
        new(new System.Windows.Interop.WindowInteropHelper(w).Handle);
}
