using BetterWindows.Core;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;

namespace BetterWindows.Monitors;

/// <summary>Seznam monitorů a upozornění na změnu (připojení monitoru, změna rozlišení/měřítka).</summary>
public sealed class MonitorService : IDisposable
{
    private readonly System.Windows.Threading.Dispatcher _dispatcher;

    /// <summary>Změnilo se nastavení obrazovek. Volá se v UI vlákně.</summary>
    public event Action? Changed;

    public MonitorService(System.Windows.Threading.Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        // Událost přichází z vlákna SystemEvents -> přesun do UI vlákna.
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    public MonitorInfo GetPrimary() =>
        GetAll().FirstOrDefault(m => m.IsPrimary) ?? throw new InvalidOperationException("Nenalezen hlavní monitor.");

    public unsafe IReadOnlyList<MonitorInfo> GetAll()
    {
        var result = new List<MonitorInfo>();

        // EnumDisplayMonitors zavolá náš callback pro každý monitor.
        PInvoke.EnumDisplayMonitors(HDC.Null, (RECT?)null, (hMonitor, _, _, _) =>
        {
            var info = new MONITORINFOEXW();
            info.monitorInfo.cbSize = (uint)sizeof(MONITORINFOEXW);
            if (!PInvoke.GetMonitorInfo(hMonitor, (MONITORINFO*)&info))
                return true; // pokračuj dalším monitorem

            // Efektivní DPI = to, co uživatel nastavil jako "Měřítko" v Nastavení obrazovky.
            if (PInvoke.GetDpiForMonitor(hMonitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out _).Failed)
                dpiX = 96;

            var m = info.monitorInfo;
            result.Add(new MonitorInfo(
                Handle: (nint)hMonitor.Value,
                DeviceName: info.szDevice.ToString(),
                Bounds: new PixelRect(m.rcMonitor.left, m.rcMonitor.top, m.rcMonitor.right, m.rcMonitor.bottom),
                WorkArea: new PixelRect(m.rcWork.left, m.rcWork.top, m.rcWork.right, m.rcWork.bottom),
                Dpi: dpiX,
                IsPrimary: (m.dwFlags & PInvoke.MONITORINFOF_PRIMARY) != 0));
            return true;
        }, 0);

        return result;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Log.Info("Změna nastavení obrazovek.");
        _dispatcher.InvokeAsync(() => Changed?.Invoke());
    }

    public void Dispose() => SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
}
