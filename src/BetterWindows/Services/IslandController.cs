using BetterWindows.Core;
using BetterWindows.Logic.Apps;
using BetterWindows.Logic.Messaging;
using BetterWindows.Monitors;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;
using BetterWindows.Logic.Settings;
using BetterWindows.ShellWindows;

namespace BetterWindows.Services;

/// <summary>Dynamic Island: okno, média, hlasitost a zprávy ze stránky.</summary>
internal sealed class IslandController : IDisposable
{
    private readonly ShellContext _context;
    private readonly MediaService _media;
    private readonly VolumeService _volume;
    private IslandWindow? _window;
    private IslandGhostWindow? _ghost;
    private bool _flying;
    private bool _fullscreen;
    private readonly AppIdentityResolver _identity;
    private readonly Func<bool> _reducedMotion;

    public IslandController(ShellContext context, AppIdentityResolver identity, WindowTracker tracker, Func<bool> reducedMotion)
    {
        _context = context;
        _identity = identity;
        _reducedMotion = reducedMotion;
        tracker.WindowMinimizing += OnWindowMinimizing;
        _media = new MediaService(context.Dispatcher, identity);
        // Hudbu ukazuje ostrůvek, mini přehrávač v liště i zamykací obrazovka (obal je zmenšený na 160 px).
        _media.Changed += m => _context.Hub.Publish(MessageTypes.MediaState, m);
        _volume = new VolumeService(context.Dispatcher);
        _volume.Changed += v =>
        {
            // Změnu poslat jako "teď" (ukazatel), ale pamatovat si klidový stav (po znovunačtení nic neblikne).
            _context.Hub.Publish(MessageTypes.VolumeState, v, sticky: false);
            _context.Hub.Remember(MessageTypes.VolumeState, v with { Changed = false });
        };

        var hub = context.Hub;
        hub.On<DockLayoutPayload>(MessageTypes.IslandLayout, (bridge, p) =>
        {
            if (p is not null && bridge.Page == "island" && !_fullscreen)
                _window?.SetHitRects(p.Hit);
        });
        // Rozbalený ostrůvek -> lišta rozmaže svůj obsah (jako stavový řádek kolem ostrůvku na telefonu).
        hub.On<IslandExpandedPayload>(MessageTypes.IslandExpanded, (bridge, p) =>
        {
            if (p is not null && bridge.Page == "island")
                _context.Hub.Publish(MessageTypes.IslandExpanded, p, sticky: false, page: "topbar");
        });
        hub.On<MediaCommandPayload>(MessageTypes.MediaCommand, (_, p) => { if (p is not null) _media.Command(p.Command); });
        hub.On<VolumeSetPayload>(MessageTypes.VolumeSet, (_, p) => { if (p is not null) _volume.Set(p.Level, p.Muted); });
        _context.Settings.Changed += OnSettingsChanged;
    }

    public void Start()
    {
        Safe.Run("Média", _media.Start);
        Safe.Run("Hlasitost", _volume.Start);
        if (_context.Settings.Current.Island.Enabled)
            OpenWindow();
    }

    private void OpenWindow()
    {
        if (_window is not null)
            return;
        _window = new IslandWindow(_context, _context.Monitors.GetPrimary());
        _window.SetBackdrop(BackdropMode.None);
        _window.Show();
        // Duch pro vlétnutí aplikace: vytvořit hned, ať první let necuká na vytváření okna.
        _ghost ??= CreateGhost();
        _ghost.Prepare();
    }

    private void CloseWindow()
    {
        Safe.Run("Zavření ostrůvku", () => _window?.Close());
        _window = null;
    }

    public void BringToTop() => _window?.BringToTop();

    // ================= Okno hrající aplikace vletí do ostrůvku =================

    /// <summary>Kompaktní pilulka: šířka = CSS token --bw-island-width, výška 28, 4 px od horního okraje.</summary>
    private const double CompactWidthDip = 260, CompactTopDip = 4;

    private void OnWindowMinimizing(HWND hwnd)
    {
        if (_window is null || _flying || _fullscreen || _reducedMotion())
            return;
        var media = _media.Current;
        if (!media.Available || !media.Playing || !_context.Settings.Current.Island.Enabled)
            return;
        var app = _identity.Resolve(hwnd);
        if (app is null || !MediaSourceMatch.Matches(_media.SourceAumid, app.AppId))
            return;
        if (!TryGetWindowRect(hwnd, out var from))
            return;

        _ghost ??= CreateGhost();
        if (!_ghost.Attach(hwnd))
        {
            Log.Warn("Ostrůvek: náhled okna pro animaci nejde vytvořit (DWM).");
            return;
        }

        // Cíl: šířka kompaktní pilulky, výška podle poměru stran okna, horní hrana v pilulce.
        var island = _window.Monitor;
        var s = island.Scale;
        var bounds = _window.Bounds;
        var size = _ghost.SourceSize;
        var w = (int)Math.Round(CompactWidthDip * s);
        var h = (int)Math.Round(w * (double)size.cy / Math.Max(1, size.cx));
        var left = bounds.Left + (bounds.Width - w) / 2;
        var top = bounds.Top + (int)Math.Round(CompactTopDip * s);
        var to = new PixelRect(left, top, left + w, top + h);

        _flying = true;
        Log.Info($"Ostrůvek: {app.Name} vlétá do ostrůvku.");
        _ghost.Fly(from, to, _window.Handle,
            onAbsorb: () => _context.Hub.Publish(MessageTypes.IslandAbsorb, new { }, sticky: false, page: "island"),
            onDone: () => _flying = false);
    }

    private IslandGhostWindow CreateGhost()
    {
        var ghost = new IslandGhostWindow(_context, _context.Monitors.GetPrimary());
        ghost.SetBackdrop(BackdropMode.None);
        return ghost;
    }

    /// <summary>
    /// Obdélník okna na začátku minimalizace. Okno může být už na cestě do „minimalizované“ polohy
    /// (-32000), pak vezmeme uloženou polohu (u maximalizovaného okna pracovní plochu monitoru).
    /// </summary>
    private unsafe bool TryGetWindowRect(HWND hwnd, out PixelRect rect)
    {
        RECT r;
        if (PInvoke.DwmGetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, &r, (uint)sizeof(RECT)).Succeeded
            && r.left > -30000 && r.right - r.left > 50 && r.bottom - r.top > 50)
        {
            rect = new PixelRect(r.left, r.top, r.right, r.bottom);
            return true;
        }

        var placement = new WINDOWPLACEMENT { length = (uint)sizeof(WINDOWPLACEMENT) };
        if (!PInvoke.GetWindowPlacement(hwnd, ref placement))
        {
            rect = default;
            return false;
        }
        // rcNormalPosition je v souřadnicích pracovní plochy (bez lišt) – posunout na obrazovku.
        var n = placement.rcNormalPosition;
        var mon = MonitorFromRect(n);
        var dx = mon is null ? 0 : mon.WorkArea.Left - mon.Bounds.Left;
        var dy = mon is null ? 0 : mon.WorkArea.Top - mon.Bounds.Top;
        const uint WPF_RESTORETOMAXIMIZED = 0x0002;
        if ((placement.flags & (WINDOWPLACEMENT_FLAGS)WPF_RESTORETOMAXIMIZED) != 0 && mon is not null)
            rect = mon.WorkArea;
        else
            rect = new PixelRect(n.left + dx, n.top + dy, n.right + dx, n.bottom + dy);
        return rect.Width > 50 && rect.Height > 50;
    }

    private MonitorInfo? MonitorFromRect(RECT r)
    {
        var cx = (r.left + r.right) / 2;
        var cy = (r.top + r.bottom) / 2;
        return _context.Monitors.GetAll().FirstOrDefault(m => cx >= m.Bounds.Left && cx < m.Bounds.Right && cy >= m.Bounds.Top && cy < m.Bounds.Bottom);
    }

    /// <summary>Hraje teď hudba/video? (Zámek po nečinnosti se pak neukáže.)</summary>
    public bool MediaPlaying => _media.Current.Playing;

    public void SetFullscreen(bool fullscreen)
    {
        _fullscreen = fullscreen;
        if (fullscreen) _window?.Hide();
        else
        {
            _window?.Show();
            _window?.BringToTop();
        }
    }

    public void SetDarkMode(bool dark) => _window?.SetDarkMode(dark);
    public void OnMonitorsChanged() => _window?.RefreshMonitor();

    private void OnSettingsChanged(AppSettings old, AppSettings now)
    {
        if (old.Island.Enabled == now.Island.Enabled)
            return;
        if (now.Island.Enabled) OpenWindow(); else CloseWindow();
    }

    public void Dispose()
    {
        _context.Settings.Changed -= OnSettingsChanged;
        Safe.Run("Zavření ducha ostrůvku", () => _ghost?.Close());
        CloseWindow();
        _media.Dispose();
        _volume.Dispose();
    }
}
