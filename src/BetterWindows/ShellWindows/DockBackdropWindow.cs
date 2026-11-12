using BetterWindows.Monitors;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BetterWindows.ShellWindows;

/// <summary>
/// Systémové rozmazání pod viditelným pásem docku. Samostatné okno bez obsahu, protože
/// DWM rozmazává vždy celý obdélník okna – okno docku je kvůli zvětšování vyšší než pás.
/// Rohy: DWM "ROUND" (Windows 11 zaoblí s vyhlazením); CSS pásu má stejný poloměr.
/// </summary>
internal sealed class DockBackdropWindow : ShellWindowBase
{
    private PixelRect? _rect;

    public DockBackdropWindow(ShellContext context, MonitorInfo monitor)
        : base(null, context, monitor)
    {
    }

    protected override DWM_WINDOW_CORNER_PREFERENCE CornerPreference => DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;

    protected override PixelRect ComputeBounds(MonitorInfo monitor) => _rect ?? new PixelRect(0, 0, 1, 1);

    protected override void Place()
    {
        // Pozice určuje dock (MoveUnder).
    }

    /// <summary>Přesune pozadí pod pás docku a v pořadí oken těsně pod okno docku.</summary>
    public void MoveUnder(HWND dock, PixelRect? rect)
    {
        if (Hwnd.IsNull)
            return;
        if (rect is null || rect.Value.Width <= 0 || rect.Value.Height <= 0)
        {
            PInvoke.ShowWindowAsync(Hwnd, SHOW_WINDOW_CMD.SW_HIDE);
            _rect = null;
            return;
        }
        _rect = rect;
        var r = rect.Value;
        // hWndInsertAfter = dock -> naše okno bude v pořadí hned ZA (pod) dockem.
        PInvoke.SetWindowPos(Hwnd, dock, r.Left, r.Top, r.Width, r.Height,
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
    }
}
