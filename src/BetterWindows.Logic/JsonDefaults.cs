using System.Text.Json;
using System.Text.Json.Serialization;

namespace BetterWindows.Logic;

/// <summary>Jednotné nastavení JSON pro soubory i zprávy do UI (camelCase, enumy jako text).</summary>
public static class JsonDefaults
{
    /// <summary>Pro zprávy C# ↔ JS (kompaktní).</summary>
    public static JsonSerializerOptions Options { get; } = Create(indented: false);

    /// <summary>Pro soubory v %AppData% (čitelné pro člověka).</summary>
    public static JsonSerializerOptions FileOptions { get; } = Create(indented: true);

    private static JsonSerializerOptions Create(bool indented)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = indented,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
