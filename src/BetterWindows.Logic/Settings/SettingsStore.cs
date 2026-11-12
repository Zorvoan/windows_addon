using System.Text.Json;

namespace BetterWindows.Logic.Settings;

/// <summary>Načítání a ukládání settings.json.</summary>
public sealed class SettingsStore(string path)
{
    public string Path { get; } = path;

    /// <summary>
    /// Načte nastavení. Chybí-li soubor, vrátí výchozí. Je-li poškozený, odloží ho
    /// vedle jako settings.corrupt-*.json (ať o data nepřijdeš) a vrátí výchozí.
    /// </summary>
    public AppSettings Load(out string? warning)
    {
        warning = null;
        if (!File.Exists(Path))
            return SettingsNormalizer.Normalize(new AppSettings());

        try
        {
            var json = File.ReadAllText(Path);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonDefaults.FileOptions)
                           ?? throw new JsonException("Soubor je prázdný (null).");
            return SettingsNormalizer.Normalize(settings);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            var backup = System.IO.Path.ChangeExtension(Path, null) + $".corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.json";
            try { File.Move(Path, backup, overwrite: true); } catch (IOException) { /* nevadí */ }
            warning = $"Nastavení bylo poškozené ({ex.Message}). Odloženo do {backup}, použity výchozí hodnoty.";
            return SettingsNormalizer.Normalize(new AppSettings());
        }
    }

    public void Save(AppSettings settings) =>
        AtomicFile.WriteAllText(Path, JsonSerializer.Serialize(settings, JsonDefaults.FileOptions));

    /// <summary>Hluboká kopie (přes JSON) – ať se upravuje kopie, ne sdílený objekt.</summary>
    public static AppSettings Clone(AppSettings settings) =>
        JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings, JsonDefaults.Options), JsonDefaults.Options)!;
}
