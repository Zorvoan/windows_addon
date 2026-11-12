using BetterWindows.Core;
using BetterWindows.Monitors;
using BetterWindows.Native;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BetterWindows.ShellWindows;

/// <summary>
/// Horní lišta přes celou šířku monitoru (ui/topbar.html), registrovaná jako AppBar:
/// Windows jí rezervuje horní okraj, takže maximalizovaná okna pod ni nezajedou.
/// </summary>
internal sealed class TopBarWindow : ShellWindowBase
{
    /// <summary>Výška lišty ve WPF jednotkách (při měřítku 100 % = pixely).</summary>
    public const double HeightDip = 36;

    private uint _appBarMessage;
    private bool _registered;

    /// <summary>Na monitoru se otevřela / zavřela aplikace přes celou obrazovku (hra, video).</summary>
    public event Action<bool>? FullscreenChanged;

    public TopBarWindow(ShellContext context, MonitorInfo monitor)
        : base("topbar", context, monitor)
    {
    }

    protected override PixelRect ComputeBounds(MonitorInfo m) =>
        new(m.Bounds.Left, m.Bounds.Top, m.Bounds.Right, m.Bounds.Top + m.ToPixels(HeightDip));

    protected override void OnHandleCreated()
    {
        // Vlastní zpráva, kterou nám Windows posílá změny (jiný AppBar, celá obrazovka).
        _appBarMessage = PInvoke.RegisterWindowMessage("BetterWindows.AppBar");
        // Nejdřív zapsat do pojistky (kdybychom spadli, watchdog AppBar odregistruje).
        SystemGuard.Record(s => s.AppBarHwnd = (long)(nint)Hwnd);
        AppBarNative.Register(Hwnd, _appBarMessage);
        _registered = true;
        Log.Info("Horní lišta zaregistrována jako AppBar.");
    }

    protected override void Place()
    {
        if (!_registered)
        {
            base.Place();
            return;
        }

        var b = Monitor.Bounds;
        var monitorRect = new RECT { left = b.Left, top = b.Top, right = b.Right, bottom = b.Bottom };
        var rc = AppBarNative.RequestTop(Hwnd, monitorRect, Monitor.ToPixels(HeightDip));
        PInvoke.SetWindowPos(Hwnd, HWND.HWND_TOPMOST, rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top,
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
        Log.Info($"Horní lišta (AppBar): {rc.right - rc.left}×{rc.bottom - rc.top} px na [{rc.left},{rc.top}] ({Monitor.Scale:P0})");
    }

    protected override nint OnWindowMessage(int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (_appBarMessage == 0 || msg != (int)_appBarMessage)
            return 0;

        handled = true;
        switch ((uint)wParam)
        {
            case PInvoke.ABN_POSCHANGED:
                // Změnil se jiný AppBar nebo hlavní panel -> požádat o místo znovu.
                Place();
                break;
            case PInvoke.ABN_FULLSCREENAPP:
                Log.Info($"Aplikace přes celou obrazovku: {(lParam != 0 ? "otevřena" : "zavřena")}");
                FullscreenChanged?.Invoke(lParam != 0);
                break;
        }
        return 0;
    }

    /// <summary>Uvolní rezervovaný okraj. Volat před zavřením okna.</summary>
    public void UnregisterAppBar()
    {
        if (!_registered)
            return;
        AppBarNative.Remove(Hwnd);
        _registered = false;
        SystemGuard.Record(s => s.AppBarHwnd = null);
        Log.Info("AppBar odregistrován.");
    }
}
