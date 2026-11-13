using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using BetterWindows.Bridge;
using BetterWindows.Core;
using BetterWindows.Logic.Settings;
using BetterWindows.Monitors;
using BetterWindows.Native;
using BetterWindows.Web;
using Microsoft.Web.WebView2.Wpf;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BetterWindows.ShellWindows;

/// <summary>
/// Společný základ oken shellu (horní lišta, dock, pozadí docku, plocha):
///  - bez rámečku, není v Alt+Tab, (volitelně) vždy navrchu a kliknutím nebere fokus,
///  - celé okno je "sklo" (DWM) a pod ním volitelně systémové rozmazání,
///  - obsah je průhledné WebView2 se stránkou ui/&lt;page&gt;.html (page = null -> bez obsahu),
///  - pozici počítá potomek ve fyzických pixelech pro daný monitor.
///
/// Proč ne AllowsTransparency=true: v tom režimu WPF kreslí okno jako "vrstvené" a WebView2
/// (samostatné nativní okno uvnitř) se pak nevykreslí. Průhlednost proto děláme přes DWM.
/// </summary>
internal abstract class ShellWindowBase : Window
{
    private readonly string? _page;
    private HwndSourceHook? _hook; // odkaz držíme, aby delegáta neuklidil GC
    private BackdropMode _backdrop = BackdropMode.None;
    private List<(int Left, int Top, int Right, int Bottom)>? _lastRegion; // naposledy nastavený tvar (px)

    protected ShellContext Context { get; }
    protected WebView2? Web { get; }
    protected HWND Hwnd { get; private set; }
    protected bool IsDark { get; private set; }

    /// <summary>Spojení se stránkou (po načtení WebView2).</summary>
    public MessageBridge? Bridge { get; private set; }

    /// <summary>Monitor, na kterém okno leží.</summary>
    public MonitorInfo Monitor { get; private set; }

    protected ShellWindowBase(string? page, ShellContext context, MonitorInfo monitor)
    {
        _page = page;
        Context = context;
        Monitor = monitor;
        IsDark = context.Theme.IsDark;

        Title = $"{AppInfo.DisplayName} – {page ?? GetType().Name}";
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = IsTopmost;
        Background = Brushes.Transparent;

        if (page is not null)
        {
            Web = new WebView2 { DefaultBackgroundColor = System.Drawing.Color.Transparent };
            Content = Web;
        }

        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        // Okno se ocitlo na monitoru s jiným měřítkem (nebo se měřítko změnilo) -> přepočítat.
        DpiChanged += (_, _) => Safe.Run($"{Title}: DPI", RefreshMonitor);
        Closed += OnClosed;
    }

    // ---------- Chování, které potomci mění ----------

    /// <summary>Vždy navrchu (lišta, dock). Plocha ne.</summary>
    protected virtual bool IsTopmost => true;

    /// <summary>Kliknutí nebere fokus aktuální aplikaci.</summary>
    protected virtual bool NoActivate => true;

    /// <summary>Kde má okno na daném monitoru ležet (fyzické pixely).</summary>
    protected abstract PixelRect ComputeBounds(MonitorInfo monitor);

    /// <summary>Zaoblení rohů od Windows 11. Výchozí: hranaté.</summary>
    protected virtual DWM_WINDOW_CORNER_PREFERENCE CornerPreference => DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DONOTROUND;

    /// <summary>Tón pro AccentAcrylic: (průhlednost, R, G, B). Nízká alfa = hodně průhledné.</summary>
    protected virtual (byte A, byte R, byte G, byte B) AccentTint(bool dark) =>
        dark ? ((byte)0x38, (byte)0x0E, (byte)0x0E, (byte)0x12) : ((byte)0x38, (byte)0xF6, (byte)0xF6, (byte)0xF8);

    /// <summary>Okno (HWND) právě vzniklo, ještě není vidět.</summary>
    protected virtual void OnHandleCreated() { }

    /// <summary>Zprávy okna pro potomky (AppBar apod.).</summary>
    protected virtual nint OnWindowMessage(int msg, nint wParam, nint lParam, ref bool handled) => 0;

    /// <summary>Stránka je načtená a most zpráv připravený.</summary>
    protected virtual void OnBridgeReady(MessageBridge bridge) { }

    /// <summary>Umístění okna. Výchozí: ComputeBounds + SetWindowPos.</summary>
    protected virtual void Place()
    {
        var r = ComputeBounds(Monitor);
        var insertAfter = IsTopmost ? HWND.HWND_TOPMOST : HWND.Null;
        var flags = SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | (IsTopmost ? 0 : SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
        PInvoke.SetWindowPos(Hwnd, insertAfter, r.Left, r.Top, r.Width, r.Height, flags);
        Log.Info($"{Title}: {r.Width}×{r.Height} px na [{r.Left},{r.Top}], monitor {Monitor.DeviceName} ({Monitor.Scale:P0})");
    }

    // ---------- Tvar okna (dock, ostrůvek) ----------

    /// <summary>Převod CSS px (relativně k oknu) na fyzické pixely na obrazovce.</summary>
    public PixelRect ToScreen(Logic.Messaging.CssRect r)
    {
        var b = ComputeBounds(Monitor);
        var s = Monitor.Scale;
        return new PixelRect(
            b.Left + (int)Math.Floor(r.X * s), b.Top + (int)Math.Floor(r.Y * s),
            b.Left + (int)Math.Ceiling((r.X + r.Width) * s), b.Top + (int)Math.Ceiling((r.Y + r.Height) * s));
    }

    /// <summary>
    /// Tvar okna = sjednocení obdélníků (CSS px relativně k oknu). Mimo ně okno "neexistuje":
    /// není vidět a kliknutí jde do oken pod ním (SetWindowRgn).
    /// </summary>
    public unsafe void SetHitRects(IReadOnlyList<Logic.Messaging.CssRect> rects)
    {
        if (Hwnd.IsNull)
            return;
        var s = Monitor.Scale;
        var pixels = rects.Select(r => ((int)Math.Floor(r.X * s), (int)Math.Floor(r.Y * s),
            (int)Math.Ceiling((r.X + r.Width) * s), (int)Math.Ceiling((r.Y + r.Height) * s))).ToList();
        // Stejný tvar jako minule -> nic nedělat (SetWindowRgn překresluje okno, během animací by to bylo zbytečně často).
        if (_lastRegion is not null && _lastRegion.SequenceEqual(pixels))
            return;
        _lastRegion = pixels;

        var region = PInvoke.CreateRectRgn(0, 0, 0, 0);
        foreach (var (left, top, right, bottom) in pixels)
        {
            var part = PInvoke.CreateRectRgn(left, top, right, bottom);
            PInvoke.CombineRgn(region, region, part, RGN_COMBINE_MODE.RGN_OR);
            PInvoke.DeleteObject(new HGDIOBJ(part.Value));
        }
        // Po úspěchu region patří systému (nemazat). Při neúspěchu ho uklidíme.
        if (PInvoke.SetWindowRgn(Hwnd, region, true) == 0)
            PInvoke.DeleteObject(new HGDIOBJ(region.Value));
    }

    /// <summary>Zrušit tvar okna = celé okno zase viditelné a klikatelné.</summary>
    public void ClearRegion()
    {
        _lastRegion = null;
        if (!Hwnd.IsNull)
            PInvoke.SetWindowRgn(Hwnd, HRGN.Null, true);
    }

    // ---------- Veřejné ovládání ----------

    public void SetBackdrop(BackdropMode backdrop)
    {
        _backdrop = backdrop;
        ApplyBackdrop();
    }

    public void SetDarkMode(bool dark)
    {
        IsDark = dark;
        ApplyBackdrop();
    }

    /// <summary>Znovu načte údaje o svém monitoru (rozlišení, měřítko) a přesune se.</summary>
    public void RefreshMonitor()
    {
        Monitor = Context.Monitors.GetAll().FirstOrDefault(m => m.DeviceName == Monitor.DeviceName) ?? Context.Monitors.GetPrimary();
        if (!Hwnd.IsNull)
            Place();
    }

    // ---------- Interní ----------

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        Hwnd = new HWND(handle);

        // WPF jinak vyplní pozadí okna černou. Průhledná barva + "sklo" přes celé okno
        // (ExtendFrameIntoClientArea) = DWM ukáže pod průhlednými pixely to, co je za oknem.
        var source = HwndSource.FromHwnd(handle)!;
        source.CompositionTarget.BackgroundColor = Colors.Transparent;
        _hook = WndProc;
        source.AddHook(_hook);

        WindowStyles.MakeShellPanel(Hwnd, NoActivate);
        Dwm.ExtendFrameIntoClientArea(Hwnd);
        Dwm.SetCornerPreference(Hwnd, CornerPreference);
        Dwm.HideBorder(Hwnd);
        ApplyBackdrop();

        Safe.Run($"{Title}: inicializace", OnHandleCreated);
        Safe.Run($"{Title}: umístění", Place);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Web is null || _page is null)
            return;
        try
        {
            var core = await WebViewHost.InitializeAsync(Web, _page);
            Bridge = new MessageBridge(_page, core);
            Context.Hub.Register(Bridge);
            OnBridgeReady(Bridge);
        }
        catch (Exception ex)
        {
            Log.Error($"Načtení stránky '{_page}' selhalo", ex);
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (Bridge is not null)
            Context.Hub.Unregister(Bridge);
        Web?.Dispose();
    }

    private void ApplyBackdrop()
    {
        if (Hwnd.IsNull)
            return;

        // Každý režim nejdřív vypne ostatní, aby se efekty nesčítaly.
        Dwm.SetDarkMode(Hwnd, IsDark);
        switch (_backdrop)
        {
            case BackdropMode.SystemAcrylic:
                AccentPolicy.Disable(Hwnd);
                Dwm.SetSystemBackdrop(Hwnd, DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TRANSIENTWINDOW);
                break;
            case BackdropMode.AccentAcrylic:
                Dwm.SetSystemBackdrop(Hwnd, DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE);
                var (a, r, g, b) = AccentTint(IsDark);
                AccentPolicy.EnableAcrylic(Hwnd, a, r, g, b);
                break;
            default:
                Dwm.SetSystemBackdrop(Hwnd, DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE);
                AccentPolicy.Disable(Hwnd);
                break;
        }
        Log.Info($"{Title}: pozadí {_backdrop}, {(IsDark ? "tmavé" : "světlé")}");
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        // Kliknutí myší: "neaktivuj mě". Pojistka k WS_EX_NOACTIVATE, aby aplikace,
        // se kterou pracuješ, zůstala v popředí i po kliknutí na lištu nebo dock.
        if (NoActivate && msg == (int)PInvoke.WM_MOUSEACTIVATE)
        {
            handled = true;
            return (nint)PInvoke.MA_NOACTIVATE;
        }

        try
        {
            return OnWindowMessage(msg, wParam, lParam, ref handled);
        }
        catch (Exception ex)
        {
            Log.Error($"{Title}: obsluha zprávy 0x{msg:X4} selhala", ex);
            return 0;
        }
    }
}
