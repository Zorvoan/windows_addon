using BetterWindows.Core;
using BetterWindows.Logic.Settings;
using BetterWindows.Monitors;
using BetterWindows.Native;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BetterWindows.ShellWindows;

/// <summary>
/// Vrstva plochy (ui/desktop.html): tapeta, widgety, vizualizér. Leží POD všemi aplikacemi.
///  - "embed": vložená do plochy Windows (viz DesktopEmbedder) – nejlepší varianta,
///    reaguje na myš i klávesnici (když jsou skryté ikony plochy),
///  - "bottom": samostatné okno držené úplně vespod (HWND_BOTTOM). Nepřebírá fokus
///    (jinak by si ho Průzkumník spletl s hrou přes celou obrazovku), takže psaní do widgetů nejde.
/// </summary>
internal sealed class DesktopWindow : ShellWindowBase
{
    private readonly DesktopMode _requestedMode;
    private readonly DesktopSnapshot? _snapshot;
    private bool _noActivate = true;

    public EmbedResult? Embedded { get; private set; }

    /// <summary>"embed-raised" | "embed-classic" | "bottom"</summary>
    public string ModeName => Embedded?.Success == true ? Embedded.Mode : "bottom";

    public string ModeDetail { get; private set; } = "";

    public DesktopWindow(ShellContext context, MonitorInfo monitor, DesktopMode mode, DesktopSnapshot? snapshot)
        : base("desktop", context, monitor)
    {
        _requestedMode = mode;
        _snapshot = snapshot;
    }

    public HWND Handle => Hwnd;

    protected override bool IsTopmost => false;
    protected override bool NoActivate => _noActivate;

    protected override PixelRect ComputeBounds(MonitorInfo m) => m.Bounds;

    protected override void OnHandleCreated()
    {
        if (_requestedMode != DesktopMode.Bottom && _snapshot is not null)
        {
            var result = DesktopEmbedder.Embed(Hwnd, _snapshot, Monitor.Bounds);
            if (result.Success)
            {
                Embedded = result;
                _noActivate = false; // jako podokno plochy smí dostat fokus (psaní do widgetů)
                ModeDetail = result.Detail;
                Log.Info($"Plocha vložena: {result.Mode} – {result.Detail}; {_snapshot}");
                return;
            }
            Log.Warn($"Vložení plochy selhalo: {result.Detail}");
            ModeDetail = $"Vložení selhalo ({result.Detail}), záloha HWND_BOTTOM";
        }
        else
        {
            ModeDetail = _requestedMode == DesktopMode.Bottom ? "HWND_BOTTOM (zvoleno v nastavení)" : "HWND_BOTTOM (plocha nenalezena)";
        }
        Log.Info($"Plocha: režim HWND_BOTTOM – {ModeDetail}");
    }

    protected override void Place()
    {
        if (Embedded?.Success == true)
        {
            DesktopEmbedder.PlaceInParent(Hwnd, Embedded.Parent, Embedded.InsertAfter, Monitor.Bounds);
            return;
        }
        var r = Monitor.Bounds;
        PInvoke.SetWindowPos(Hwnd, HWND.HWND_BOTTOM, r.Left, r.Top, r.Width, r.Height, SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
    }

    /// <summary>Je vrstva pořád tam, kde má být? (Průzkumník mohl plochu přestavět.)</summary>
    public bool CheckHealthy() =>
        Embedded?.Success == true ? DesktopEmbedder.Verify(Hwnd, Embedded) : PInvoke.IsWindowVisible(Hwnd);

    protected override unsafe nint OnWindowMessage(int msg, nint wParam, nint lParam, ref bool handled)
    {
        // Režim HWND_BOTTOM: kdokoli (i Windows po kliknutí) chce okno posunout nahoru -> necháme ho vespod.
        if (Embedded?.Success != true && msg == (int)PInvoke.WM_WINDOWPOSCHANGING && lParam != 0)
        {
            var pos = (WINDOWPOS*)lParam;
            if ((pos->flags & SET_WINDOW_POS_FLAGS.SWP_NOZORDER) == 0)
                pos->hwndInsertAfter = HWND.HWND_BOTTOM;
        }
        return 0;
    }
}
