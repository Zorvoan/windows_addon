using BetterWindows.Core;
using BetterWindows.Logic.Messaging;
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
    private bool _fullscreen;

    public IslandController(ShellContext context, AppIdentityResolver identity)
    {
        _context = context;
        _media = new MediaService(context.Dispatcher, identity);
        // Obal alba (data URL) potřebuje jen ostrůvek – neposílat ho všem stránkám.
        _media.Changed += m => _context.Hub.Publish(MessageTypes.MediaState, m, page: "island");
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
    }

    private void CloseWindow()
    {
        Safe.Run("Zavření ostrůvku", () => _window?.Close());
        _window = null;
    }

    public void BringToTop() => _window?.BringToTop();

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
        CloseWindow();
        _media.Dispose();
        _volume.Dispose();
    }
}
