using System.Windows.Threading;
using BetterWindows.Core;
using BetterWindows.Logic.Dock;
using BetterWindows.Native;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BetterWindows.Services;

/// <summary>
/// Seznam "skutečných" oken aplikací a okno v popředí.
///  - Na začátku EnumWindows (všechna okna nejvyšší úrovně, seřazená odshora dolů).
///  - Pak SetWinEventHook: Windows nám hlásí vznik/zánik/skrytí oken, změnu titulku,
///    minimalizaci a změnu okna v popředí. Po změně se seznam přepočítá (s malým zpožděním,
///    ať se dávka událostí zpracuje najednou).
/// Okna vlastního procesu (lišta, dock) se ignorují – kliknutí na dock tak nemění "aktivní aplikaci".
/// </summary>
internal sealed class WindowTracker : IDisposable
{
    private readonly AppIdentityResolver _identity;
    private readonly WINEVENTPROC _callback; // odkaz držíme, jinak by ho GC uklidil a Windows by volal neexistující funkci
    private readonly List<HWINEVENTHOOK> _hooks = [];
    private readonly DispatcherTimer _debounce;
    private readonly Dictionary<nint, (long FirstSeen, AppIdentity Identity)> _known = [];
    private readonly uint _ownPid = (uint)Environment.ProcessId;
    private long _sequence;

    public IReadOnlyList<AppWindowInfo> Windows { get; private set; } = [];
    public string? ForegroundAppId { get; private set; }
    /// <summary>Poslední cizí okno v popředí (pro vrácení fokusu po kontextové nabídce).</summary>
    public nint ForegroundHwnd { get; private set; }
    public string ForegroundName { get; private set; } = "Plocha";
    public string ForegroundTitle { get; private set; } = "";

    public event Action? WindowsChanged;
    public event Action? ForegroundChanged;

    public WindowTracker(AppIdentityResolver identity, Dispatcher dispatcher)
    {
        _identity = identity;
        _callback = OnWinEvent;
        _debounce = new DispatcherTimer(TimeSpan.FromMilliseconds(80), DispatcherPriority.Background, (_, _) =>
        {
            _debounce!.Stop();
            Safe.Run("Seznam oken", Rescan);
        }, dispatcher);
        _debounce.Stop();
    }

    public void Start()
    {
        // WINEVENT_OUTOFCONTEXT: události chodí přes frontu zpráv do našeho (UI) vlákna, nic se nevkládá do cizích procesů.
        Hook(PInvoke.EVENT_SYSTEM_FOREGROUND, PInvoke.EVENT_SYSTEM_FOREGROUND);
        Hook(PInvoke.EVENT_SYSTEM_MINIMIZESTART, PInvoke.EVENT_SYSTEM_MINIMIZEEND);
        Hook(PInvoke.EVENT_OBJECT_CREATE, PInvoke.EVENT_OBJECT_HIDE); // CREATE, DESTROY, SHOW, HIDE
        Hook(PInvoke.EVENT_OBJECT_NAMECHANGE, PInvoke.EVENT_OBJECT_NAMECHANGE);
        Hook(PInvoke.EVENT_OBJECT_CLOAKED, PInvoke.EVENT_OBJECT_UNCLOAKED);
        Rescan();
        UpdateForeground(PInvoke.GetForegroundWindow());
    }

    private void Hook(uint min, uint max)
    {
        var hook = PInvoke.SetWinEventHook(min, max, HMODULE.Null, _callback, 0, 0,
            PInvoke.WINEVENT_OUTOFCONTEXT | PInvoke.WINEVENT_SKIPOWNPROCESS);
        if (hook.IsNull)
            Log.Warn($"SetWinEventHook 0x{min:X4}–0x{max:X4} selhal.");
        else
            _hooks.Add(hook);
    }

    private void OnWinEvent(HWINEVENTHOOK hook, uint @event, HWND hwnd, int idObject, int idChild, uint thread, uint time)
    {
        try
        {
            if (@event == PInvoke.EVENT_SYSTEM_FOREGROUND)
            {
                UpdateForeground(hwnd);
                ScheduleRescan(); // změnilo se pořadí oken
                return;
            }

            // Zajímají nás jen samotná okna (ne tlačítka, kurzor apod. uvnitř nich).
            const int OBJID_WINDOW = 0, CHILDID_SELF = 0;
            if (idObject != OBJID_WINDOW || idChild != CHILDID_SELF || hwnd.IsNull)
                return;

            if (@event == PInvoke.EVENT_OBJECT_NAMECHANGE)
            {
                if (_known.ContainsKey((nint)hwnd))
                    ScheduleRescan();
                if (hwnd == PInvoke.GetForegroundWindow())
                    UpdateForeground(hwnd);
                return;
            }
            ScheduleRescan();
        }
        catch (Exception ex)
        {
            Log.Error("WindowTracker: událost selhala", ex);
        }
    }

    private void ScheduleRescan()
    {
        if (!_debounce.IsEnabled)
            _debounce.Start();
    }

    private void Rescan()
    {
        var found = new List<HWND>();
        PInvoke.EnumWindows((hwnd, _) =>
        {
            if (WindowInfo.IsAppWindow(hwnd) && WindowInfo.GetProcessId(hwnd) != _ownPid)
                found.Add(hwnd);
            return true;
        }, 0);

        var list = new List<AppWindowInfo>(found.Count);
        var alive = new HashSet<nint>();
        for (var z = 0; z < found.Count; z++)
        {
            var hwnd = found[z];
            var key = (nint)hwnd;
            alive.Add(key);
            if (!_known.TryGetValue(key, out var entry))
            {
                var identity = Safe.Run("Identita okna", () => _identity.Resolve(hwnd));
                if (identity is null)
                    continue;
                entry = (++_sequence, identity);
                _known[key] = entry;
            }
            else if (entry.Identity.AppId.EndsWith("applicationframehost.exe", StringComparison.OrdinalIgnoreCase))
            {
                // UWP okno zachycené dřív, než se aplikace v rámečku "usadila" -> zkusit znovu.
                var again = Safe.Run("Identita okna (znovu)", () => _identity.Resolve(hwnd));
                if (again is not null && again.AppId != entry.Identity.AppId)
                {
                    entry = (entry.FirstSeen, again);
                    _known[key] = entry;
                }
            }
            list.Add(new AppWindowInfo((long)key, entry.Identity.AppId, entry.Identity.Name,
                WindowInfo.GetTitle(hwnd), PInvoke.IsIconic(hwnd), z, entry.FirstSeen));
        }

        foreach (var gone in _known.Keys.Where(k => !alive.Contains(k)).ToList())
        {
            _known.Remove(gone);
            _identity.Forget(gone);
        }

        if (!SameContent(list, Windows))
        {
            Windows = list;
            WindowsChanged?.Invoke();
        }
    }

    private void UpdateForeground(HWND hwnd)
    {
        if (hwnd.IsNull || WindowInfo.GetProcessId(hwnd) == _ownPid)
            return; // klik do lišty / docku – "aktivní aplikace" zůstává
        ForegroundHwnd = (nint)hwnd;

        string? appId;
        string name, title;
        if (WindowInfo.IsDesktopOrTaskbar(hwnd))
        {
            appId = null;
            name = "Plocha";
            title = "";
        }
        else
        {
            // Dialog (např. "Uložit jako") patří aplikaci, která ho otevřela.
            var root = PInvoke.GetAncestor(hwnd, GET_ANCESTOR_FLAGS.GA_ROOTOWNER);
            var identity = Safe.Run("Identita okna v popředí", () => _identity.Resolve(root.IsNull ? hwnd : root));
            appId = identity?.AppId;
            name = identity?.Name ?? WindowInfo.GetTitle(hwnd);
            title = WindowInfo.GetTitle(hwnd);
        }

        if (appId == ForegroundAppId && name == ForegroundName && title == ForegroundTitle)
            return;
        ForegroundAppId = appId;
        ForegroundName = name;
        ForegroundTitle = title;
        ForegroundChanged?.Invoke();
    }

    private static bool SameContent(IReadOnlyList<AppWindowInfo> a, IReadOnlyList<AppWindowInfo> b) =>
        a.Count == b.Count && a.Zip(b).All(p => p.First == p.Second);

    public void Dispose()
    {
        _debounce.Stop();
        foreach (var hook in _hooks)
            PInvoke.UnhookWinEvent(hook);
        _hooks.Clear();
    }
}
