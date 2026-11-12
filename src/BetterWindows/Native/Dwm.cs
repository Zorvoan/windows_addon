using BetterWindows.Core;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.Controls;

namespace BetterWindows.Native;

/// <summary>
/// Desktop Window Manager = část Windows, která skládá okna na obrazovku.
/// Přes něj se zapíná systémové rozmazání (Acrylic), tmavý režim rámu a zaoblení rohů.
/// Selhání jen zalogujeme: okno bez efektu je lepší než pád.
/// </summary>
internal static class Dwm
{
    /// <summary>
    /// "Sklo přes celé okno": DWM pak bere celou plochu okna jako průhlednou oblast
    /// a pod průhlednými pixely (z WPF/WebView2) ukáže pozadí nebo rozmazání.
    /// </summary>
    public static void ExtendFrameIntoClientArea(HWND hwnd)
    {
        var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
        Check(PInvoke.DwmExtendFrameIntoClientArea(hwnd, margins), "DwmExtendFrameIntoClientArea");
    }

    /// <summary>Systémové pozadí okna (Windows 11 22H2+). TRANSIENTWINDOW = Acrylic (rozmaže vše pod oknem).</summary>
    public static void SetSystemBackdrop(HWND hwnd, DWM_SYSTEMBACKDROP_TYPE type) =>
        Set(hwnd, DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, (int)type);

    /// <summary>Tmavá varianta systémových efektů (tón Acrylicu).</summary>
    public static void SetDarkMode(HWND hwnd, bool dark) =>
        Set(hwnd, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, dark ? 1 : 0);

    /// <summary>Windows 11 okna automaticky zaobluje; liště to vypneme, docku zapneme.</summary>
    public static void SetCornerPreference(HWND hwnd, DWM_WINDOW_CORNER_PREFERENCE preference) =>
        Set(hwnd, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, (int)preference);

    /// <summary>Vypne tenký barevný okraj, který Windows 11 kreslí kolem oken.</summary>
    public static void HideBorder(HWND hwnd) =>
        Set(hwnd, DWMWINDOWATTRIBUTE.DWMWA_BORDER_COLOR, PInvoke.DWMWA_COLOR_NONE);

    private static unsafe void Set<T>(HWND hwnd, DWMWINDOWATTRIBUTE attribute, T value) where T : unmanaged =>
        Check(PInvoke.DwmSetWindowAttribute(hwnd, attribute, &value, (uint)sizeof(T)), attribute.ToString());

    private static void Check(HRESULT result, string what)
    {
        if (result.Failed)
            Log.Warn($"DWM: {what} selhalo (HRESULT 0x{result.Value:X8})");
    }
}
