using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using BetterWindows.Core;
using BetterWindows.Monitors;
using BetterWindows.Native;
using BetterWindows.Web;
using Microsoft.Web.WebView2.Wpf;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BetterWindows.ShellWindows;

/// <summary>
/// Společný základ oken shellu, která leží NAD aplikacemi (horní lišta, dock, později Dynamic Island):
///  - bez rámečku, vždy navrchu, není v Alt+Tab, kliknutím nebere fokus,
///  - celé okno je "sklo" a pod ním systémové rozmazání (viz BackdropKind),
///  - obsah je průhledné WebView2 se stránkou ui/&lt;page&gt;.html,
///  - pozici počítá potomek ve fyzických pixelech pro daný monitor.
///
/// Proč ne AllowsTransparency=true: v tom režimu WPF kreslí okno jako "vrstvené" a WebView2
/// (které je samostatné nativní okno uvnitř) se pak nevykreslí. Proto průhlednost děláme přes DWM.
/// </summary>
internal abstract class ShellWindowBase : Window
{
    private readonly string _page;
    private readonly MonitorService _monitors;
    private HWND _hwnd;
    private BackdropKind _backdrop = BackdropKind.SystemAcrylic;
    private bool _dark;

    protected WebView2 Web { get; } = new();

    /// <summary>Monitor, na kterém okno leží.</summary>
    public MonitorInfo Monitor { get; private set; }

    protected ShellWindowBase(string page, MonitorService monitors, MonitorInfo monitor, bool dark)
    {
        _page = page;
        _monitors = monitors;
        _dark = dark;
        Monitor = monitor;

        Title = $"{AppInfo.DisplayName} – {page}";
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        Background = Brushes.Transparent;

        Web.DefaultBackgroundColor = System.Drawing.Color.Transparent;
        Content = Web;

        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        // Okno se ocitlo na monitoru s jiným měřítkem (nebo se měřítko změnilo) -> přepočítat.
        DpiChanged += (_, _) => RefreshMonitor();
        Closed += (_, _) => Web.Dispose();
    }

    /// <summary>Kde má okno na daném monitoru ležet (fyzické pixely).</summary>
    protected abstract PixelRect ComputeBounds(MonitorInfo monitor);

    /// <summary>Zaoblení rohů od Windows 11. Výchozí: hranaté.</summary>
    protected virtual DWM_WINDOW_CORNER_PREFERENCE CornerPreference => DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DONOTROUND;

    /// <summary>Tón pro BackdropKind.AccentAcrylic: (průhlednost, R, G, B). Nízká alfa = hodně průhledné.</summary>
    protected virtual (byte A, byte R, byte G, byte B) AccentTint(bool dark) =>
        dark ? ((byte)0x38, (byte)0x0E, (byte)0x0E, (byte)0x12) : ((byte)0x38, (byte)0xF6, (byte)0xF6, (byte)0xF8);

    public void SetBackdrop(BackdropKind backdrop)
    {
        _backdrop = backdrop;
        ApplyBackdrop();
    }

    public void SetDarkMode(bool dark)
    {
        _dark = dark;
        ApplyBackdrop();
    }

    /// <summary>Znovu načte údaje o svém monitoru (rozlišení, měřítko) a přesune se.</summary>
    public void RefreshMonitor()
    {
        Monitor = _monitors.GetAll().FirstOrDefault(m => m.DeviceName == Monitor.DeviceName) ?? _monitors.GetPrimary();
        PlaceOnMonitor();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        _hwnd = new HWND(handle);

        // WPF jinak vyplní pozadí okna černou. Průhledná barva + "sklo" přes celé okno
        // (ExtendFrameIntoClientArea) = DWM ukáže pod průhlednými pixely rozmazání.
        var source = HwndSource.FromHwnd(handle)!;
        source.CompositionTarget.BackgroundColor = Colors.Transparent;
        source.AddHook(WndProc);

        WindowStyles.MakeShellPanel(_hwnd);
        Dwm.ExtendFrameIntoClientArea(_hwnd);
        Dwm.SetCornerPreference(_hwnd, CornerPreference);
        Dwm.HideBorder(_hwnd);
        ApplyBackdrop();
        PlaceOnMonitor();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await WebViewHost.InitializeAsync(Web, _page);
        }
        catch (Exception ex)
        {
            Log.Error($"Načtení stránky '{_page}' selhalo", ex);
        }
    }

    private void ApplyBackdrop()
    {
        if (_hwnd.IsNull)
            return;

        // Každý režim nejdřív vypne ostatní, aby se efekty nesčítaly.
        Dwm.SetDarkMode(_hwnd, _dark);
        switch (_backdrop)
        {
            case BackdropKind.SystemAcrylic:
                AccentPolicy.Disable(_hwnd);
                Dwm.SetSystemBackdrop(_hwnd, DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TRANSIENTWINDOW);
                break;
            case BackdropKind.AccentAcrylic:
                Dwm.SetSystemBackdrop(_hwnd, DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE);
                var (a, r, g, b) = AccentTint(_dark);
                AccentPolicy.EnableAcrylic(_hwnd, a, r, g, b);
                break;
            case BackdropKind.None:
                Dwm.SetSystemBackdrop(_hwnd, DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE);
                AccentPolicy.Disable(_hwnd);
                break;
        }
        Log.Info($"Okno '{_page}': pozadí {_backdrop}, {(_dark ? "tmavé" : "světlé")}");
    }

    private void PlaceOnMonitor()
    {
        if (_hwnd.IsNull)
            return;

        var r = ComputeBounds(Monitor);
        // Pozice ve fyzických pixelech, HWND_TOPMOST = nad běžnými okny, SWP_NOACTIVATE = nebrat fokus.
        PInvoke.SetWindowPos(_hwnd, HWND.HWND_TOPMOST, r.Left, r.Top, r.Width, r.Height,
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
        Log.Info($"Okno '{_page}': {r.Width}×{r.Height} px na [{r.Left},{r.Top}], monitor {Monitor.DeviceName} ({Monitor.Scale:P0})");
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        // Kliknutí myší: "neaktivuj mě". Pojistka k WS_EX_NOACTIVATE, aby aplikace,
        // se kterou pracuješ, zůstala v popředí i po kliknutí na lištu.
        if (msg == (int)PInvoke.WM_MOUSEACTIVATE)
        {
            handled = true;
            return (nint)PInvoke.MA_NOACTIVATE;
        }
        return 0;
    }
}
