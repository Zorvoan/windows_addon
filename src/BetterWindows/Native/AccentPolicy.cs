using System.Runtime.InteropServices;
using BetterWindows.Core;

namespace BetterWindows.Native;

/// <summary>
/// Rozmazání přes NEDOKUMENTOVANÉ API SetWindowCompositionAttribute (z doby Windows 10).
/// Používá ho např. TranslucentTB. Výhoda oproti DWM Acrylicu: můžeme si zvolit barvu
/// a průhlednost tónu (DWM má tón pevně daný systémem). Nevýhoda: Microsoft ho může
/// v nějaké aktualizaci změnit nebo rozbít.
/// </summary>
internal static class AccentPolicy
{
    private enum AccentState
    {
        Disabled = 0,
        EnableAcrylicBlurBehind = 4,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ACCENT_POLICY
    {
        public AccentState AccentState;
        public int AccentFlags;
        public uint GradientColor; // barva tónu ve formátu 0xAABBGGRR
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWCOMPOSITIONATTRIBDATA
    {
        public int Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    private const int WCA_ACCENT_POLICY = 19;

    [DllImport("user32.dll")]
    private static extern bool SetWindowCompositionAttribute(IntPtr hwnd, ref WINDOWCOMPOSITIONATTRIBDATA data);

    /// <summary>Zapne Acrylic s vlastním tónem. Barva: A = průhlednost (0–255), R, G, B.</summary>
    public static void EnableAcrylic(IntPtr hwnd, byte a, byte r, byte g, byte b)
    {
        // Alfa 0 způsobuje v některých verzích Windows blikání, proto minimum 1.
        uint abgr = ((uint)Math.Max(a, (byte)1) << 24) | ((uint)b << 16) | ((uint)g << 8) | r;
        Apply(hwnd, new ACCENT_POLICY { AccentState = AccentState.EnableAcrylicBlurBehind, GradientColor = abgr });
    }

    public static void Disable(IntPtr hwnd) =>
        Apply(hwnd, new ACCENT_POLICY { AccentState = AccentState.Disabled });

    private static void Apply(IntPtr hwnd, ACCENT_POLICY policy)
    {
        var size = Marshal.SizeOf<ACCENT_POLICY>();
        var ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(policy, ptr, false);
            var data = new WINDOWCOMPOSITIONATTRIBDATA { Attribute = WCA_ACCENT_POLICY, Data = ptr, SizeOfData = size };
            if (!SetWindowCompositionAttribute(hwnd, ref data))
                Log.Warn($"SetWindowCompositionAttribute selhalo (stav {policy.AccentState})");
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
}
