using System.IO;
using BetterWindows.Core;
using BetterWindows.Logic.Settings;

namespace BetterWindows.Services;

/// <summary>Nastavení v %AppData%\BetterWindows\settings.json. Změny hlásí událostí Changed (v UI vlákně).</summary>
internal sealed class SettingsService
{
    private readonly SettingsStore _store = new(Path.Combine(AppInfo.DataDir, "settings.json"));

    public AppSettings Current { get; private set; }

    /// <summary>(staré, nové) nastavení.</summary>
    public event Action<AppSettings, AppSettings>? Changed;

    public SettingsService()
    {
        Current = _store.Load(out var warning);
        if (warning is not null)
            Log.Warn(warning);
        Log.Info($"Nastavení načteno z {_store.Path}");
    }

    /// <summary>Upraví kopii nastavení a uloží ji.</summary>
    public void Update(Action<AppSettings> mutate)
    {
        var copy = SettingsStore.Clone(Current);
        mutate(copy);
        Replace(copy);
    }

    public void Replace(AppSettings next)
    {
        SettingsNormalizer.Normalize(next);
        var old = Current;
        Current = next;
        Safe.Run("Uložení nastavení", () => _store.Save(next));
        Changed?.Invoke(old, next);
    }
}
