using BetterWindows.Monitors;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BetterWindows.ShellWindows;

/// <summary>
/// Vlastní zamykací obrazovka (ui/lock.html) přes celý monitor, nad vším ostatním.
/// Není to zabezpečení – chrání až zámek Windows, který se zavolá po "odemknutí".
/// Při startu se načte neviditelně (prázdný tvar okna), pak se jen ukazuje.
/// </summary>
internal sealed class LockScreenWindow : ShellWindowBase
{
    public LockScreenWindow(ShellContext context, MonitorInfo monitor)
        : base("lock", context, monitor)
    {
    }

    protected override bool NoActivate => false;
    protected override PixelRect ComputeBounds(MonitorInfo m) => m.Bounds;
    protected override void OnHandleCreated() => SetHitRects([]);

    public HWND Handle => Hwnd;

    public void FocusContent() => Web?.Focus();

    public void BringToTop()
    {
        if (!Hwnd.IsNull)
            PInvoke.SetWindowPos(Hwnd, HWND.HWND_TOPMOST, 0, 0, 0, 0,
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
    }
}
