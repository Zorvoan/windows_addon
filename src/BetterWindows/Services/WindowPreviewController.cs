using System.Windows.Threading;
using BetterWindows.Core;
using BetterWindows.Logic.Apps;
using BetterWindows.Logic.Messaging;
using BetterWindows.ShellWindows;

namespace BetterWindows.Services;

/// <summary>
/// Kdy ukázat / schovat náhledy oken: po 450 ms nad ikonou spuštěné aplikace, schovat 350 ms
/// po odchodu (pokud myš nepřejela na samotné náhledy).
/// </summary>
internal sealed class WindowPreviewController : IDisposable
{
    private readonly ShellContext _context;
    private readonly WindowTracker _tracker;
    private readonly DispatcherTimer _showTimer;
    private readonly DispatcherTimer _hideTimer;
    private PreviewWindow? _window;
    private string? _pendingApp;
    private CssRect? _pendingRect;
    private string? _shownApp;
    private bool _overPreview;

    /// <summary>Převod CSS obdélníku ikony na obrazovku (zajišťuje DockWindow).</summary>
    public Func<CssRect, Monitors.PixelRect>? ToScreen { get; set; }

    public WindowPreviewController(ShellContext context, WindowTracker tracker)
    {
        _context = context;
        _tracker = tracker;
        _showTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(450), DispatcherPriority.Normal, (_, _) => { _showTimer!.Stop(); Safe.Run("Náhledy – zobrazení", ShowPending); }, context.Dispatcher);
        _hideTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(350), DispatcherPriority.Normal, (_, _) => { _hideTimer!.Stop(); if (!_overPreview) Safe.Run("Náhledy – skrytí", Hide); }, context.Dispatcher);
        _showTimer.Stop();
        _hideTimer.Stop();
        _tracker.WindowsChanged += () =>
        {
            if (_shownApp is not null)
                Safe.Run("Náhledy – obnova", () => ShowApp(_shownApp, _pendingRect));
        };
        context.Hub.On<DockHoverPayload>(MessageTypes.DockHover, (_, p) => Safe.Run("Náhledy", () => OnHover(p)));
    }

    private void OnHover(DockHoverPayload? p)
    {
        if (!_context.Settings.Current.Dock.Previews)
            return;
        if (p?.AppId is null || p.Rect is null)
        {
            _pendingApp = null;
            _showTimer.Stop();
            _hideTimer.Stop();
            _hideTimer.Start();
            return;
        }

        _pendingApp = p.AppId;
        _pendingRect = p.Rect;
        _hideTimer.Stop();
        if (_shownApp is not null)
        {
            ShowPending(); // náhledy už jsou vidět -> přepnout hned
            return;
        }
        _showTimer.Stop();
        _showTimer.Start();
    }

    private void ShowPending()
    {
        if (_pendingApp is not null)
            ShowApp(_pendingApp, _pendingRect);
    }

    private void ShowApp(string appId, CssRect? rect)
    {
        var windows = _tracker.Windows.Where(w => AppId.Equal(w.AppId, appId)).OrderBy(w => w.FirstSeen).ToList();
        if (windows.Count == 0 || rect is null || ToScreen is null)
        {
            Hide();
            return;
        }

        if (_window is null)
        {
            _window = new PreviewWindow(_context, _context.Monitors.GetPrimary());
            _window.SetBackdrop(_context.Settings.Current.Appearance.DockBackdrop);
            _window.HoverChanged += over =>
            {
                _overPreview = over;
                if (over) _hideTimer.Stop(); else { _hideTimer.Stop(); _hideTimer.Start(); }
            };
            _window.ActivateRequested += hwnd =>
            {
                Hide();
                AppLauncher.Activate(hwnd);
            };
            _window.CloseRequested += hwnd => AppLauncher.Close([hwnd]);
        }
        _window.SetDarkMode(_context.Theme.IsDark);
        _window.ShowFor(windows, ToScreen(rect));
        _shownApp = appId;
    }

    public void Hide()
    {
        _showTimer.Stop();
        _shownApp = null;
        _overPreview = false;
        _window?.HidePreview();
    }

    public void Dispose()
    {
        Hide();
        Safe.Run("Zavření náhledů", () => _window?.Close());
    }
}
