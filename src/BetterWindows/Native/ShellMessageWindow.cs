using System.Windows.Interop;
using BetterWindows.Core;
using Windows.Win32;

namespace BetterWindows.Native;

/// <summary>
/// Skryté okno pro systémové zprávy, které chodí jen oknům nejvyšší úrovně.
/// Hlavně "TaskbarCreated": Průzkumník se restartoval (spadl nebo ho někdo restartoval)
/// -> je potřeba znovu vložit vrstvu plochy, znovu skrýt hlavní panel apod.
/// </summary>
internal sealed class ShellMessageWindow : IDisposable
{
    private readonly HwndSource _source;
    private readonly HwndSourceHook _hook; // držíme odkaz, aby ho GC neuklidil
    private readonly uint _taskbarCreated;

    /// <summary>Průzkumník byl znovu spuštěn. Volá se v UI vlákně.</summary>
    public event Action? ExplorerRestarted;

    public ShellMessageWindow()
    {
        _taskbarCreated = PInvoke.RegisterWindowMessage("TaskbarCreated");
        var parameters = new HwndSourceParameters("BetterWindows.ShellMessages")
        {
            Width = 0,
            Height = 0,
            WindowStyle = unchecked((int)0x80000000), // WS_POPUP, nikdy se neukáže
            ExtendedWindowStyle = 0x00000080,         // WS_EX_TOOLWINDOW
        };
        _source = new HwndSource(parameters);
        _hook = WndProc;
        _source.AddHook(_hook);
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == _taskbarCreated)
        {
            Log.Warn("Průzkumník byl restartován (TaskbarCreated).");
            ExplorerRestarted?.Invoke();
        }
        return 0;
    }

    public void Dispose()
    {
        _source.RemoveHook(_hook);
        _source.Dispose();
    }
}
