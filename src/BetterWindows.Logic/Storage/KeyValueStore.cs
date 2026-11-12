using System.Text.Json;

namespace BetterWindows.Logic.Storage;

/// <summary>
/// Jednoduché úložiště "klíč -> JSON hodnota" pro data widgetů (např. připomínky).
/// Soubor %AppData%\BetterWindows\widgets-data.json.
/// </summary>
public sealed class KeyValueStore
{
    private readonly string _path;
    private readonly Dictionary<string, JsonElement> _values;
    private readonly object _gate = new();

    public KeyValueStore(string path)
    {
        _path = path;
        _values = LoadFile(path);
    }

    public JsonElement? Get(string key)
    {
        lock (_gate)
            return _values.TryGetValue(key, out var v) ? v : null;
    }

    public void Set(string key, JsonElement? value)
    {
        lock (_gate)
        {
            if (value is null || value.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                _values.Remove(key);
            else
                _values[key] = value.Value.Clone();
            AtomicFile.WriteAllText(_path, JsonSerializer.Serialize(_values, JsonDefaults.FileOptions));
        }
    }

    private static Dictionary<string, JsonElement> LoadFile(string path)
    {
        try
        {
            if (File.Exists(path))
                return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(path), JsonDefaults.FileOptions) ?? [];
        }
        catch (JsonException)
        {
            try { File.Move(path, path + $".corrupt-{DateTime.Now:yyyyMMdd-HHmmss}", overwrite: true); } catch (IOException) { }
        }
        return [];
    }
}
