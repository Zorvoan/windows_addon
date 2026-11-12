using System.Text.Json;

namespace BetterWindows.Logic.SystemState;

/// <summary>
/// Co Better Windows právě změnilo na systému. Ukládá se do system-state.json PŘED každou změnou,
/// takže i po pádu nebo zabití procesu ví watchdog / další spuštění, co vrátit.
/// </summary>
public sealed class SystemStateData
{
    public bool DesktopIconsHidden { get; set; }
    public bool TaskbarHidden { get; set; }
    /// <summary>Původní stav hlavního panelu (ABM_GETSTATE: 1 = automaticky skrývat).</summary>
    public int? TaskbarOriginalState { get; set; }
    /// <summary>HWND horní lišty registrované jako AppBar (pro odregistrování po pádu).</summary>
    public long? AppBarHwnd { get; set; }
    public int? OwnerPid { get; set; }

    public bool IsClean => !DesktopIconsHidden && !TaskbarHidden && AppBarHwnd is null;
}

/// <summary>Co je potřeba obnovit.</summary>
public sealed record RestorePlan(bool ShowDesktopIcons, bool ShowTaskbar, int? TaskbarStateToRestore, long? AppBarToRemove)
{
    public bool IsEmpty => !ShowDesktopIcons && !ShowTaskbar && AppBarToRemove is null;
}

public static class RestorePlanner
{
    /// <param name="corrupt">Soubor existuje, ale nejde přečíst -> nevíme, co se změnilo, obnovíme vše bezpečné.</param>
    public static RestorePlan Plan(SystemStateData? state, bool corrupt)
    {
        if (corrupt)
            return new RestorePlan(ShowDesktopIcons: true, ShowTaskbar: true, TaskbarStateToRestore: null, AppBarToRemove: null);
        if (state is null)
            return new RestorePlan(false, false, null, null);
        return new RestorePlan(
            ShowDesktopIcons: state.DesktopIconsHidden,
            ShowTaskbar: state.TaskbarHidden,
            TaskbarStateToRestore: state.TaskbarHidden ? state.TaskbarOriginalState : null,
            AppBarToRemove: state.AppBarHwnd);
    }
}

/// <summary>Soubor system-state.json. Bezpečné pro volání z watchdogu (bez UI).</summary>
public sealed class SystemStateFile(string path)
{
    public string Path { get; } = path;

    public SystemStateData? Load(out bool corrupt)
    {
        corrupt = false;
        if (!File.Exists(Path))
            return null;
        try
        {
            return JsonSerializer.Deserialize<SystemStateData>(File.ReadAllText(Path), Logic.JsonDefaults.FileOptions);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            corrupt = true;
            return null;
        }
    }

    public void Save(SystemStateData state)
    {
        if (state.IsClean)
            Delete();
        else
            AtomicFile.WriteAllText(Path, JsonSerializer.Serialize(state, Logic.JsonDefaults.FileOptions));
    }

    public void Delete()
    {
        try { File.Delete(Path); } catch (IOException) { /* nevadí */ }
    }
}
