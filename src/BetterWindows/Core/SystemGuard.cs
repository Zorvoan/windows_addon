using System.IO;
using BetterWindows.Logic.SystemState;
using BetterWindows.Native;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BetterWindows.Core;

/// <summary>
/// Pojistka změn systému (ikony plochy, hlavní panel, AppBar).
///
/// Pravidlo: PŘED každou změnou se zapíše do system-state.json, co se mění.
/// Obnova pak funguje ve všech případech:
///  - běžné ukončení: App.OnExit -> RestoreCurrent()
///  - pád: CrashGuard -> RestoreCurrent()
///  - zabití procesu (Správce úloh): watchdog (druhý proces) -> RestoreFromFile()
///  - zabití obou: při dalším spuštění -> RestoreFromFile()
///  - restart počítače: ikony i hlavní panel skrýváme jen dočasně, Windows je ukáže samo
///    (výjimka: "automaticky skrývat" hlavní panel – viz TaskbarService).
/// Všechno tady musí fungovat bez UI (volá to i watchdog).
/// </summary>
internal static class SystemGuard
{
    private static readonly SystemStateFile File = new(Path.Combine(AppInfo.DataDir, "system-state.json"));
    private static readonly object Gate = new();
    private static SystemStateData _state = new();

    public static SystemStateData Current
    {
        get { lock (Gate) return _state; }
    }

    /// <summary>Zapíše plánovanou změnu. Volat PŘED zásahem do systému.</summary>
    public static void Record(Action<SystemStateData> change)
    {
        lock (Gate)
        {
            change(_state);
            _state.OwnerPid = Environment.ProcessId;
            File.Save(_state);
        }
    }

    /// <summary>Obnoví vše podle stavu v paměti (běžné ukončení, pád).</summary>
    public static void RestoreCurrent(string reason)
    {
        SystemStateData snapshot;
        lock (Gate)
            snapshot = _state;
        Execute(RestorePlanner.Plan(snapshot, corrupt: false), reason);
        lock (Gate)
        {
            _state = new SystemStateData();
            File.Delete();
        }
    }

    /// <summary>Obnoví vše podle souboru (watchdog, start po pádu). Vrací true, když bylo co obnovovat.</summary>
    public static bool RestoreFromFile(string reason)
    {
        var state = File.Load(out var corrupt);
        var plan = RestorePlanner.Plan(state, corrupt);
        if (plan.IsEmpty)
        {
            File.Delete();
            return false;
        }
        Execute(plan, reason);
        File.Delete();
        return true;
    }

    private static void Execute(RestorePlan plan, string reason)
    {
        if (plan.IsEmpty)
            return;
        Log.Info($"Obnova systému ({reason}): ikony={plan.ShowDesktopIcons}, hlavní panel={plan.ShowTaskbar}, AppBar={plan.AppBarToRemove}");

        // Každý krok zvlášť: když jeden selže, ostatní musí proběhnout.
        if (plan.ShowTaskbar)
        {
            Safe.Run("Obnova hlavního panelu", () =>
            {
                foreach (var taskbar in ExplorerWindows.FindTaskbars())
                    PInvoke.ShowWindowAsync(taskbar, SHOW_WINDOW_CMD.SW_SHOWNA);
                if (plan.TaskbarStateToRestore is { } original)
                {
                    var primary = PInvoke.FindWindow("Shell_TrayWnd", null);
                    AppBarNative.SetTaskbarState(primary, original);
                }
            });
        }
        if (plan.ShowDesktopIcons)
            Safe.Run("Obnova ikon plochy", () => ExplorerWindows.SetDesktopIconsVisible(true));
        if (plan.AppBarToRemove is { } appBar)
            Safe.Run("Odregistrování AppBaru", () => AppBarNative.Remove(new HWND((nint)appBar)));
    }
}
