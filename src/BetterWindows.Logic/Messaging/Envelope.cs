using System.Text.Json;

namespace BetterWindows.Logic.Messaging;

/// <summary>Zabalení / rozbalení zprávy { type, payload }.</summary>
public static class Envelope
{
    public static string Serialize(string type, object? payload) =>
        JsonSerializer.Serialize(new { type, payload }, JsonDefaults.Options);

    /// <summary>Rozbalí JSON přijatý z JS. Vrací false, když to není platná zpráva.</summary>
    public static bool TryParse(string json, out string type, out JsonElement payload)
    {
        type = "";
        payload = default;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object ||
                !doc.RootElement.TryGetProperty("type", out var typeEl) ||
                typeEl.ValueKind != JsonValueKind.String)
                return false;

            type = typeEl.GetString()!;
            payload = doc.RootElement.TryGetProperty("payload", out var p) ? p.Clone() : default;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>Převede payload na konkrétní typ. Prázdný payload -> null.</summary>
    public static T? Read<T>(JsonElement payload) where T : class =>
        payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? null
            : payload.Deserialize<T>(JsonDefaults.Options);
}
