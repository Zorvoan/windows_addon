using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using BetterWindows.Core;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace BetterWindows.Services;

/// <summary>Koš v docku: počet položek, otevření, vysypání (Windows se zeptá).</summary>
internal sealed class TrashService : IDisposable
{
    private readonly DispatcherTimer _timer;

    public long Count { get; private set; } = -1;
    public event Action? Changed;

    public TrashService(Dispatcher dispatcher) =>
        _timer = new DispatcherTimer(TimeSpan.FromSeconds(10), DispatcherPriority.Background, (_, _) => Safe.Run("Koš", Refresh), dispatcher);

    public void Start()
    {
        Refresh();
        _timer.Start();
    }

    public void Refresh()
    {
        var info = new SHQUERYRBINFO { cbSize = (uint)Marshal.SizeOf<SHQUERYRBINFO>() };
        // null = koše všech disků dohromady
        var count = PInvoke.SHQueryRecycleBin(null, ref info).Succeeded ? info.i64NumItems : -1;
        if (count == Count)
            return;
        Count = count;
        Changed?.Invoke();
    }

    public static void Open() =>
        Process.Start(new ProcessStartInfo("explorer.exe", "shell:RecycleBinFolder") { UseShellExecute = true })?.Dispose();

    public void Empty()
    {
        // Bez SHERB_NOCONFIRMATION -> Windows se zeptá, jestli opravdu vysypat.
        var hr = PInvoke.SHEmptyRecycleBin(HWND.Null, (string?)null, 0);
        if (hr.Failed && hr.Value != unchecked((int)0x8000FFFF)) // E_UNEXPECTED = koš už je prázdný
            Log.Warn($"Vysypání koše: HRESULT 0x{hr.Value:X8}");
        Refresh();
    }

    public void Dispose() => _timer.Stop();
}
