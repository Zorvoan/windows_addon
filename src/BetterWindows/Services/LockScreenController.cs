using System.Runtime.InteropServices;
using System.Windows.Threading;
using BetterWindows.Core;
using BetterWindows.Logic.Lock;
using BetterWindows.Logic.Messaging;
using BetterWindows.Logic.Settings;
using BetterWindows.Native;
using BetterWindows.ShellWindows;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.System.Power;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace BetterWindows.Services;

/// <summary>
/// Zamykací obrazovka Better Windows – ukáže se PŘED zámkem Windows:
///  - otevření: tlačítko Zamknout ve Startu, zkratka Win+Shift+L, volitelně po nečinnosti,
///  - "odemknutí" (klik / klávesa): pokud je zapnuto RequireWindowsLock, zavolá se LockWorkStation
///    = skutečný zámek Windows (heslo, PIN, Windows Hello),
///  - editor: stejné okno v režimu úprav (Nastavení › Zamykací obrazovka › Upravit).
/// Win+L si Windows vyhrazuje (zamkne hned), proto vlastní zkratka Win+Shift+L.
/// </summary>
internal sealed class LockScreenController : IDisposable
{
    private const int HotKeyId = 0x4C4B; // "LK"

    private readonly ShellContext _context;
    private readonly ShellMessageWindow _messages;
    private readonly Func<bool> _mediaPlaying;
    private readonly Func<bool> _fullscreen;
    private readonly DispatcherTimer _idle;
    private LockScreenWindow? _window;
    private bool _ready;
    private bool _open;
    private bool _editing;
    private bool _hotKey;

    public LockScreenController(ShellContext context, ShellMessageWindow messages, Func<bool> mediaPlaying, Func<bool> fullscreen)
    {
        _context = context;
        _messages = messages;
        _mediaPlaying = mediaPlaying;
        _fullscreen = fullscreen;
        _idle = new DispatcherTimer(TimeSpan.FromSeconds(15), DispatcherPriority.Background, (_, _) => Safe.Run("Zámek – nečinnost", CheckIdle), context.Dispatcher);

        var hub = context.Hub;
        hub.On<UiReadyPayload>(MessageTypes.UiReady, (bridge, _) =>
        {
            if (bridge.Page != "lock" || _ready)
                return;
            _ready = true;
            _window?.Hide();
            _window?.ClearRegion();
        });
        hub.On<LockOpenPayload>(MessageTypes.LockOpen, (_, p) => Open(p?.Editing ?? false));
        hub.On<object>(MessageTypes.LockDismiss, (_, _) => Dismiss());
        hub.On<object>(MessageTypes.LockEditDone, (_, _) => Close());
        _messages.HotKey += id =>
        {
            if (id == HotKeyId)
                Safe.Run("Zámek – zkratka", () => Open(editing: false));
        };
        SystemEvents.SessionSwitch += OnSessionSwitch;
        _context.Settings.Changed += OnSettingsChanged;
    }

    public void Start()
    {
        Apply(_context.Settings.Current.Lock);
        _idle.Start();
    }

    private void Apply(LockSettings s)
    {
        if (s.Enabled && _window is null)
        {
            _ready = false;
            _window = new LockScreenWindow(_context, _context.Monitors.GetPrimary());
            _window.SetBackdrop(BackdropMode.None);
            _window.Show(); // neviditelně (prázdný tvar), jen kvůli načtení stránky
        }
        else if (!s.Enabled && _window is not null)
        {
            Close();
            Safe.Run("Zavření zámku", _window.Close);
            _window = null;
            _ready = false;
        }

        if (s.Enabled && !_hotKey)
        {
            _hotKey = _messages.RegisterHotKey(HotKeyId, HOT_KEY_MODIFIERS.MOD_WIN | HOT_KEY_MODIFIERS.MOD_SHIFT, 0x4C /* L */);
            Log.Info(_hotKey ? "Zámek: zkratka Win+Shift+L zaregistrována." : "Zámek: zkratku Win+Shift+L už používá jiný program.");
        }
        else if (!s.Enabled && _hotKey)
        {
            _messages.UnregisterHotKey(HotKeyId);
            _hotKey = false;
        }
    }

    public void Open(bool editing)
    {
        if (_window is null || !_ready || (_open && _editing == editing))
            return;
        _open = true;
        _editing = editing;
        _window.RefreshMonitor();
        _window.ClearRegion();
        _window.Show();
        _window.BringToTop();
        ForegroundHelper.BringToForeground(_window.Handle);
        _window.FocusContent();
        _context.Hub.Publish(MessageTypes.LockVisibility, new LockVisibilityPayload(true, editing), sticky: false, page: "lock");
        Log.Info(editing ? "Zámek: editor otevřen." : "Zámek: zamykací obrazovka zobrazena.");
    }

    /// <summary>Uživatel "odemkl" naši obrazovku -> případně zámek Windows.</summary>
    private void Dismiss()
    {
        var lockWindows = !_editing && _context.Settings.Current.Lock.RequireWindowsLock;
        if (lockWindows && !PInvoke.LockWorkStation())
            Log.Warn($"LockWorkStation selhalo (chyba {Marshal.GetLastWin32Error()}).");
        Close();
    }

    public void Close()
    {
        if (!_open)
            return;
        _open = false;
        _editing = false;
        _window?.Hide();
        _context.Hub.Publish(MessageTypes.LockVisibility, new LockVisibilityPayload(false, false), sticky: false, page: "lock");
    }

    private void CheckIdle()
    {
        var s = _context.Settings.Current.Lock;
        if (!s.Enabled || s.IdleMinutes <= 0)
            return;
        if (IdlePolicy.ShouldShow(s.IdleMinutes, IdleTime(), _open, _mediaPlaying(), _fullscreen(), DisplayRequired()))
            Open(editing: false);
    }

    /// <summary>Jak dlouho nikdo nesáhl na myš ani klávesnici.</summary>
    private static TimeSpan IdleTime()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (!PInvoke.GetLastInputInfo(ref info))
            return TimeSpan.Zero;
        return TimeSpan.FromMilliseconds(unchecked((uint)Environment.TickCount - info.dwTime));
    }

    /// <summary>Žádá nějaká aplikace, ať obrazovka nezhasne? (Přehrávač videa, prezentace…)</summary>
    private static unsafe bool DisplayRequired()
    {
        uint state = 0;
        var status = PInvoke.CallNtPowerInformation(POWER_INFORMATION_LEVEL.SystemExecutionState, null, 0, &state, sizeof(uint));
        const uint ES_DISPLAY_REQUIRED = 0x00000002;
        return status == 0 && (state & ES_DISPLAY_REQUIRED) != 0;
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        // Po odemknutí Windows už naše obrazovka nemá co dělat.
        if (e.Reason == SessionSwitchReason.SessionUnlock)
            _context.Dispatcher.InvokeAsync(() => { if (!_editing) Close(); });
    }

    private void OnSettingsChanged(AppSettings old, AppSettings now)
    {
        if (old.Lock.Enabled != now.Lock.Enabled)
            Apply(now.Lock);
    }

    public void SetDarkMode(bool dark) => _window?.SetDarkMode(dark);

    public void Dispose()
    {
        _idle.Stop();
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        _context.Settings.Changed -= OnSettingsChanged;
        if (_hotKey)
            _messages.UnregisterHotKey(HotKeyId);
        Safe.Run("Zavření zámku", () => _window?.Close());
    }
}
