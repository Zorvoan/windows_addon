using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using BetterWindows.Bridge;
using BetterWindows.Core;
using BetterWindows.Logic.Apps;
using BetterWindows.Logic.Dock;
using BetterWindows.Logic.Messaging;
using BetterWindows.Logic.Settings;
using BetterWindows.ShellWindows;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace BetterWindows.Services;

/// <summary>
/// Dock: spojuje připnuté aplikace (nastavení) se spuštěnými okny (WindowTracker),
/// posílá stav do stránky, obsluhuje kliky, kontextovou nabídku, ikony a tvar okna.
/// </summary>
internal sealed class DockController : IDisposable
{
    private readonly ShellContext _context;
    private readonly WindowTracker _tracker;
    private readonly AppIdentityResolver _identity;
    private readonly IconService _icons;
    private DockWindow? _window;
    private DockBackdropWindow? _backdrop;
    private bool _hiddenForFullscreen;
    private readonly WindowPreviewController _previews;

    /// <summary>Speciální položky docku (jako v docku Familiar): Launchpad na začátku, Koš na konci.</summary>
    public const string LaunchpadId = "bw:launchpad";
    public const string TrashId = "bw:trash";

    private readonly TrashService _trash;
    private readonly Action _openLaunchpad;

    public DockController(ShellContext context, WindowTracker tracker, AppIdentityResolver identity, IconService icons,
        TrashService trash, Action openLaunchpad)
    {
        _context = context;
        _trash = trash;
        _openLaunchpad = openLaunchpad;
        _trash.Changed += () => Publish();
        _tracker = tracker;
        _identity = identity;
        _icons = icons;

        _tracker.WindowsChanged += Publish;
        _tracker.ForegroundChanged += Publish;
        _previews = new WindowPreviewController(context, tracker)
        {
            ToScreen = rect => _window?.ToScreen(rect) ?? default,
        };
        _context.Settings.Changed += OnSettingsChanged;
        _icons.IconReady += (appId, url) =>
        {
            if (url is not null)
                _context.Hub.Publish(MessageTypes.IconsResult, new IconsResultPayload(new Dictionary<string, string> { [appId] = url }), sticky: false);
        };

        var hub = _context.Hub;
        hub.On<DockActivatePayload>(MessageTypes.DockActivate, (_, p) => { if (p is not null) Activate(p.AppId); });
        hub.On<DockContextMenuPayload>(MessageTypes.DockContextMenu, (_, p) => { if (p is not null) ShowContextMenu(p.AppId); });
        hub.On<DockLayoutPayload>(MessageTypes.DockLayout, (bridge, p) => { if (p is not null && bridge.Page == "dock") ApplyLayout(p); });
        hub.On<IconsRequestPayload>(MessageTypes.IconsRequest, (bridge, p) => { if (p is not null) SendIcons(bridge, p.AppIds); });
    }

    public void Start()
    {
        if (!_context.Settings.Current.Dock.Enabled)
            return;
        var monitor = _context.Monitors.GetPrimary();
        _backdrop = new DockBackdropWindow(_context, monitor);
        _backdrop.SetBackdrop(_context.Theme.ReduceTransparency || _context.Theme.HighContrast
            ? BackdropMode.None : _context.Settings.Current.Appearance.DockBackdrop);
        _backdrop.Create(); // ukáže se až pod pásem docku (MoveUnder)
        _window = new DockWindow(_context, monitor);
        _window.Show();
        Publish();
    }

    public void Stop()
    {
        Safe.Run("Zavření docku", () => _window?.Close());
        Safe.Run("Zavření pozadí docku", () => _backdrop?.Close());
        _window = null;
        _backdrop = null;
    }

    public void SetFullscreen(bool fullscreen)
    {
        _hiddenForFullscreen = fullscreen;
        _previews.Hide();
        if (fullscreen)
            _window?.Hide();
        else
            _window?.Show();
        _backdrop?.SetVisible(!fullscreen);
    }

    public void SetBackdrop(BackdropMode mode) => _backdrop?.SetBackdrop(mode);

    public void SetDarkMode(bool dark)
    {
        _window?.SetDarkMode(dark);
        _backdrop?.SetDarkMode(dark);
    }

    public void OnMonitorsChanged()
    {
        _window?.RefreshMonitor();
        _backdrop?.RefreshMonitor();
    }

    // ---------- Stav ----------

    private void Publish()
    {
        var items = DockModel.BuildItems(_context.Settings.Current.Dock.Pinned, _tracker.Windows, _tracker.ForegroundAppId);
        _context.Hub.Publish(MessageTypes.DockState, new DockStatePayload(items, _trash.Count));
    }

    private void OnSettingsChanged(AppSettings old, AppSettings now)
    {
        var o = old.Dock;
        var n = now.Dock;
        if (o.Enabled != n.Enabled)
        {
            if (n.Enabled) Start(); else Stop();
            return;
        }
        if (o.IconSize != n.IconSize || o.MaxScale != n.MaxScale || o.Magnification != n.Magnification)
            _window?.RefreshMonitor(); // jiná výška okna

        if (!o.Pinned.Select(p => p.AppId).SequenceEqual(n.Pinned.Select(p => p.AppId)))
            Publish();
    }

    // ---------- Akce ----------

    private void Activate(string appId)
    {
        _previews.Hide();
        if (appId == LaunchpadId)
        {
            _openLaunchpad();
            return;
        }
        if (appId == TrashId)
        {
            Safe.Run("Otevření koše", TrashService.Open);
            return;
        }
        var action = DockModel.DecideClick(appId, _tracker.Windows, _tracker.ForegroundAppId);
        Log.Info($"Dock: {appId} -> {action}");
        switch (action)
        {
            case DockAction.Launch l:
                Safe.Run("Spuštění aplikace", () => AppLauncher.Launch(l.AppId));
                break;
            case DockAction.Activate a:
                AppLauncher.Activate(a.Hwnd);
                break;
            case DockAction.Minimize m:
                AppLauncher.Minimize(m.Hwnds);
                break;
        }
    }

    private void SetPinned(string appId, bool pinned)
    {
        _context.Settings.Update(s =>
        {
            s.Dock.Pinned.RemoveAll(p => AppId.Equal(p.AppId, appId));
            if (pinned)
            {
                var name = _tracker.Windows.FirstOrDefault(w => AppId.Equal(w.AppId, appId))?.AppName ?? _identity.GetName(appId);
                s.Dock.Pinned.Add(new PinnedApp { AppId = appId, Name = name });
            }
        });
    }

    /// <summary>
    /// Nativní WPF nabídka (může přesahovat okno docku). Dock nikdy nebere fokus, ale nabídka ho
    /// potřebuje, aby se zavřela kliknutím jinam – proto ho na chvíli aktivujeme a po zavření
    /// vrátíme fokus aplikaci, která ho měla (stejný trik používá oznamovací oblast Windows).
    /// </summary>
    private void ShowContextMenu(string appId)
    {
        _previews.Hide();
        if (_window is null)
            return;

        if (appId == LaunchpadId)
            return;
        if (appId == TrashId)
        {
            ShowTrashMenu();
            return;
        }

        var pinned = _context.Settings.Current.Dock.Pinned.Any(p => AppId.Equal(p.AppId, appId));
        var windows = _tracker.Windows.Where(w => AppId.Equal(w.AppId, appId)).ToList();
        var name = windows.FirstOrDefault()?.AppName
                   ?? _context.Settings.Current.Dock.Pinned.FirstOrDefault(p => AppId.Equal(p.AppId, appId))?.Name
                   ?? _identity.GetName(appId);
        var canLaunch = AppId.Parse(appId).Kind != AppIdKind.Unknown;

        var menu = new ContextMenu { Style = (Style)Application.Current.Resources["Bw.ContextMenu"] };
        var itemStyle = (Style)Application.Current.Resources["Bw.MenuItem"];
        var sepStyle = (Style)Application.Current.Resources["Bw.Separator"];
        MenuItem Item(string header, Action? click, bool enabled = true)
        {
            var mi = new MenuItem { Header = header, Style = itemStyle, IsEnabled = enabled };
            if (click is not null)
                mi.Click += (_, _) => Safe.Run($"Dock: {header}", click);
            return mi;
        }

        menu.Items.Add(Item(name, null, enabled: false));
        menu.Items.Add(new Separator { Style = sepStyle });
        // Apple: Dock menu může vypsat otevřená okna – rychlý skok na konkrétní okno.
        foreach (var w in windows.OrderBy(w => w.FirstSeen).Take(8))
        {
            var title = string.IsNullOrWhiteSpace(w.Title) ? w.AppName : w.Title;
            if (title.Length > 60) title = title[..59] + "…";
            var hwnd = w.Hwnd;
            menu.Items.Add(Item(w.IsMinimized ? $"{title} (minimalizováno)" : title, () => AppLauncher.Activate(hwnd)));
        }
        if (windows.Count > 0)
            menu.Items.Add(new Separator { Style = sepStyle });
        if (windows.Count > 0 && canLaunch)
            menu.Items.Add(Item("Nové okno", () => AppLauncher.Launch(appId)));
        menu.Items.Add(pinned
            ? Item("Odepnout z docku", () => SetPinned(appId, false))
            : Item("Připnout do docku", () => SetPinned(appId, true), canLaunch));
        if (windows.Count > 0)
        {
            menu.Items.Add(new Separator { Style = sepStyle });
            menu.Items.Add(Item(windows.Count > 1 ? $"Ukončit (zavřít {windows.Count} okna)" : "Ukončit",
                () => AppLauncher.Close(windows.Select(w => w.Hwnd))));
        }

        var previous = _tracker.ForegroundHwnd;
        menu.Placement = PlacementMode.MousePoint;
        menu.PlacementTarget = _window;
        menu.Closed += (_, _) =>
        {
            // Pokud fokus zůstal u docku, vrátit ho aplikaci.
            var fg = PInvoke.GetForegroundWindow();
            if (fg == _window?.Handle && previous != 0)
                PInvoke.SetForegroundWindow(new HWND(previous));
        };
        PInvoke.SetForegroundWindow(_window.Handle);
        menu.IsOpen = true;
    }

    private void ShowTrashMenu()
    {
        if (_window is null)
            return;
        var menu = new ContextMenu { Style = (Style)Application.Current.Resources["Bw.ContextMenu"] };
        var itemStyle = (Style)Application.Current.Resources["Bw.MenuItem"];
        var open = new MenuItem { Header = "Otevřít koš", Style = itemStyle };
        open.Click += (_, _) => Safe.Run("Otevření koše", TrashService.Open);
        var empty = new MenuItem { Header = "Vysypat koš…", Style = itemStyle, IsEnabled = _trash.Count != 0 };
        empty.Click += (_, _) => Safe.Run("Vysypání koše", _trash.Empty);
        menu.Items.Add(open);
        menu.Items.Add(empty);
        var previous = _tracker.ForegroundHwnd;
        menu.Placement = PlacementMode.MousePoint;
        menu.PlacementTarget = _window;
        menu.Closed += (_, _) =>
        {
            if (PInvoke.GetForegroundWindow() == _window?.Handle && previous != 0)
                PInvoke.SetForegroundWindow(new HWND(previous));
        };
        PInvoke.SetForegroundWindow(_window.Handle);
        menu.IsOpen = true;
    }

    private void SendIcons(MessageBridge bridge, IReadOnlyList<string> appIds)
    {
        var ready = new Dictionary<string, string>();
        foreach (var id in appIds.Distinct().Where(i => !i.StartsWith("bw:", StringComparison.Ordinal)))
        {
            if (_icons.TryGet(id, out var url) && url is not null)
                ready[id] = url;
        }
        if (ready.Count > 0)
            _context.Hub.Send(bridge, MessageTypes.IconsResult, new IconsResultPayload(ready));
    }

    private void ApplyLayout(DockLayoutPayload layout)
    {
        if (_window is null || _hiddenForFullscreen)
            return;
        _window.SetHitRects(layout.Hit);
        _backdrop?.MoveUnder(_window.Handle, layout.Band is null ? null : _window.ToScreen(layout.Band));
    }

    public void Dispose()
    {
        _tracker.WindowsChanged -= Publish;
        _tracker.ForegroundChanged -= Publish;
        _context.Settings.Changed -= OnSettingsChanged;
        _previews.Dispose();
        Stop();
    }
}
