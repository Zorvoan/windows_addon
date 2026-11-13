using System.Diagnostics;
using System.Windows.Media;
using BetterWindows.Core;
using BetterWindows.Monitors;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BetterWindows.ShellWindows;

/// <summary>
/// „Duch“ minimalizovaného okna: hrající aplikace vletí do ostrůvku (jako aplikace na telefonu
/// při odchodu na plochu), ostatní zajedou do své ikony v docku (jako na Macu). Obsah kreslí DWM (živý náhled okna, DwmRegisterThumbnail).
///
/// Okno samo se NEHÝBE: je průhledné přes celý monitor a každý snímek se mění jen obdélník náhledu
/// (DwmUpdateThumbnailProperties – skládá ho přímo DWM). Přesouvat a zvětšovat celé okno každý snímek
/// (první verze) se trhalo. Myš okno nebere a ukazuje se jen po dobu letu (~0,4 s).
///
/// Pozn.: skutečnou animaci minimalizace kreslí Windows a nejde ji nahradit – se zapnutými animacemi
/// Windows proto současně poletí i okno samo dolů. Duch je animace navíc.
/// </summary>
internal sealed class IslandGhostWindow : ShellWindowBase
{
    /*
     * Pohyb = kriticky tlumená pružina (response 0,42 s): plynulý rozjezd, dlouhé dojetí do ostrůvku.
     * (První verze: 380 ms se zrychlením i zpomalením – uprostřed příliš rychlá, na videu skoky.)
     * Průhlednost a pohlcení se řídí postupem DRÁHY, ne času: duch zmizí až těsně u ostrůvku.
     */
    private const double DurationMs = 560;
    private const double Response = 0.42;  // s
    private const double FadeFrom = 0.86;  // od 86 % dráhy průhlední
    private const double AbsorbAt = 0.8;   // v 80 % dráhy ostrůvek „polkne“ (nafoukne se)

    private nint _thumb;
    private PixelRect _from, _to;
    private HWND _insertAfter;
    private readonly Stopwatch _clock = new();
    private bool _absorbed;
    private Action? _onAbsorb;
    private Action? _onDone;

    public IslandGhostWindow(ShellContext context, MonitorInfo monitor)
        : base(null, context, monitor)
    {
    }

    protected override DWM_WINDOW_CORNER_PREFERENCE CornerPreference => DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
    protected override PixelRect ComputeBounds(MonitorInfo monitor) => monitor.Bounds;

    protected override void Place()
    {
        // Celý monitor, v pořadí oken těsně pod ostrůvkem (letí „do něj“, ale nad vším ostatním).
        var b = Monitor.Bounds;
        PInvoke.SetWindowPos(Hwnd, _insertAfter.IsNull ? HWND.HWND_TOPMOST : _insertAfter, b.Left, b.Top, b.Width, b.Height,
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
    }

    /// <summary>Připravit okno předem (první let pak nečeká na vytvoření okna).</summary>
    public void Prepare() => new System.Windows.Interop.WindowInteropHelper(this).EnsureHandle();

    /// <summary>Duch nebere myš – kliknutí jde do okna pod ním.</summary>
    protected override nint OnWindowMessage(int msg, nint wParam, nint lParam, ref bool handled)
    {
        const int WM_NCHITTEST = 0x0084, HTTRANSPARENT = -1;
        if (msg != WM_NCHITTEST)
            return 0;
        handled = true;
        return HTTRANSPARENT;
    }

    /// <summary>Velikost zdrojového okna (poměr stran pro cílový obdélník).</summary>
    // (DWM_TNP_RECTDESTINATION je v souřadnicích klientské oblasti tohoto okna = obrazovka − levý horní roh monitoru.)
    public SIZE SourceSize { get; private set; } = new(16, 10);

    /// <summary>Připojí živý náhled okna. False = DWM náhled nejde (animace se neukáže).</summary>
    public bool Attach(HWND source)
    {
        new System.Windows.Interop.WindowInteropHelper(this).EnsureHandle();
        Detach();
        if (!PInvoke.DwmRegisterThumbnail(Hwnd, source, out var t).Succeeded)
            return false;
        _thumb = (nint)t;
        if (PInvoke.DwmQueryThumbnailSourceSize(t, out var size).Succeeded && size.cx > 0 && size.cy > 0)
            SourceSize = size;
        return true;
    }

    /// <summary>
    /// Spustí let z <paramref name="from"/> do <paramref name="to"/> (fyzické px obrazovky).
    /// insertAfter = okno ostrůvku: duch letí pod ním (do něj), ale nad vším ostatním.
    /// </summary>
    public void Fly(PixelRect from, PixelRect to, HWND insertAfter, Action onAbsorb, Action onDone)
    {
        _from = from;
        _to = to;
        _insertAfter = insertAfter;
        _onAbsorb = onAbsorb;
        _onDone = onDone;
        _absorbed = false;
        Place();
        Step(0);
        PInvoke.ShowWindow(Hwnd, SHOW_WINDOW_CMD.SW_SHOWNOACTIVATE);
        _clock.Restart();
        CompositionTarget.Rendering += OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var t = Math.Min(1, _clock.Elapsed.TotalMilliseconds / DurationMs);
        var x = Path(t);
        Safe.Run("Ostrůvek – let okna", () => Step(x));
        if (!_absorbed && x >= AbsorbAt)
        {
            _absorbed = true;
            Safe.Run("Ostrůvek – pohlcení", () => _onAbsorb?.Invoke());
        }
        if (t >= 1)
            Finish();
    }

    /// <summary>Postup dráhy 0..1 v čase t (0..1 trvání): kriticky tlumená pružina, na konci přesně 1.</summary>
    private static double Path(double t)
    {
        var omega = 2 * Math.PI / Response;
        double Spring(double seconds) => 1 - (1 + omega * seconds) * Math.Exp(-omega * seconds);
        return Math.Min(1, Spring(t * DurationMs / 1000) / Spring(DurationMs / 1000)); // v t = 1 přesně v cíli
    }

    /// <param name="p">Postup dráhy 0..1 (už po křivce pružiny).</param>
    private void Step(double p)
    {
        var e = p;
        var ox = Monitor.Bounds.Left;
        var oy = Monitor.Bounds.Top;
        int Lerp(int a, int b) => (int)Math.Round(a + (b - a) * e);
        var opacity = p <= FadeFrom ? 255 : (byte)Math.Round(255 * (1 - (p - FadeFrom) / (1 - FadeFrom)));

        if (_thumb != 0)
        {
            var props = new DWM_THUMBNAIL_PROPERTIES
            {
                dwFlags = PInvoke.DWM_TNP_RECTDESTINATION | PInvoke.DWM_TNP_VISIBLE | PInvoke.DWM_TNP_OPACITY | PInvoke.DWM_TNP_SOURCECLIENTAREAONLY,
                rcDestination = new RECT
                {
                    left = Lerp(_from.Left, _to.Left) - ox,
                    top = Lerp(_from.Top, _to.Top) - oy,
                    right = Lerp(_from.Right, _to.Right) - ox,
                    bottom = Lerp(_from.Bottom, _to.Bottom) - oy,
                },
                fVisible = true,
                opacity = (byte)opacity,
                fSourceClientAreaOnly = false,
            };
            PInvoke.DwmUpdateThumbnailProperties(_thumb, props);
        }
    }

    private void Finish()
    {
        CompositionTarget.Rendering -= OnRendering;
        _clock.Stop();
        PInvoke.ShowWindow(Hwnd, SHOW_WINDOW_CMD.SW_HIDE);
        Detach();
        var done = _onDone;
        _onDone = null;
        done?.Invoke();
    }

    public void Detach()
    {
        if (_thumb != 0)
            PInvoke.DwmUnregisterThumbnail(_thumb);
        _thumb = 0;
    }

    protected override void OnClosed(EventArgs e)
    {
        CompositionTarget.Rendering -= OnRendering;
        Detach();
        base.OnClosed(e);
    }
}
