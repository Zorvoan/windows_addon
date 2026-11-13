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
/// „Duch“ okna hrající aplikace, který při minimalizaci vletí do ostrůvku (jako aplikace na telefonu
/// při odchodu na plochu). Obsah kreslí DWM (živý náhled okna, DwmRegisterThumbnail); okno se každý
/// snímek přesune a zmenší po dráze z původní polohy okna do ostrůvku a na konci zprůhlední.
///
/// Pozn.: skutečnou animaci minimalizace kreslí Windows a nejde ji nahradit – se zapnutými animacemi
/// Windows proto současně poletí i okno samo dolů. Duch je animace navíc.
/// </summary>
internal sealed class IslandGhostWindow : ShellWindowBase
{
    private const double DurationMs = 380;
    private const double FadeFrom = 0.6;   // od 60 % dráhy průhlední
    private const double AbsorbAt = 0.75;  // v 75 % dráhy ostrůvek „polkne“ (nafoukne se)

    private nint _thumb;
    private PixelRect _from, _to, _rect = new(0, 0, 1, 1);
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
    protected override PixelRect ComputeBounds(MonitorInfo monitor) => _rect;

    protected override void Place()
    {
        // Poloha se řídí animací (Step).
    }

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
        Step(0);
        PInvoke.ShowWindow(Hwnd, SHOW_WINDOW_CMD.SW_SHOWNOACTIVATE);
        _clock.Restart();
        CompositionTarget.Rendering += OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var p = Math.Min(1, _clock.Elapsed.TotalMilliseconds / DurationMs);
        Safe.Run("Ostrůvek – let okna", () => Step(p));
        if (!_absorbed && p >= AbsorbAt)
        {
            _absorbed = true;
            Safe.Run("Ostrůvek – pohlcení", () => _onAbsorb?.Invoke());
        }
        if (p >= 1)
            Finish();
    }

    private void Step(double p)
    {
        // Dráha: zrychlí a před ostrůvkem zpomalí (ease-in-out, kubická).
        var e = p < 0.5 ? 4 * p * p * p : 1 - Math.Pow(-2 * p + 2, 3) / 2;
        static int Lerp(int a, int b, double t) => (int)Math.Round(a + (b - a) * t);
        _rect = new PixelRect(Lerp(_from.Left, _to.Left, e), Lerp(_from.Top, _to.Top, e),
            Lerp(_from.Right, _to.Right, e), Lerp(_from.Bottom, _to.Bottom, e));
        var opacity = p <= FadeFrom ? 255 : (byte)Math.Round(255 * (1 - (p - FadeFrom) / (1 - FadeFrom)));

        PInvoke.SetWindowPos(Hwnd, _insertAfter, _rect.Left, _rect.Top, Math.Max(1, _rect.Width), Math.Max(1, _rect.Height),
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
        if (_thumb != 0)
        {
            var props = new DWM_THUMBNAIL_PROPERTIES
            {
                dwFlags = PInvoke.DWM_TNP_RECTDESTINATION | PInvoke.DWM_TNP_VISIBLE | PInvoke.DWM_TNP_OPACITY | PInvoke.DWM_TNP_SOURCECLIENTAREAONLY,
                rcDestination = new RECT { left = 0, top = 0, right = _rect.Width, bottom = _rect.Height },
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
