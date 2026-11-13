using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BetterWindows.Logic.Settings;

namespace BetterWindows.Logic.Themes;

/// <summary>
/// Sdílení motivu jako textu: "BW1." + base64url(JSON s krátkými klíči).
/// Importovaný kód je NEDŮVĚRYHODNÝ text, který skončí v CSS – proto:
/// jen známé klíče, barvy jen ve tvaru #rrggbb, čísla omezená na rozsah, délka max. 1024 znaků.
/// </summary>
public static partial class ThemeCodec
{
    public const string Prefix = "BW1.";
    public const int MaxLength = 1024;

    public sealed record Decoded(ThemeStyle Style, int? DockIconSize);

    public static string Encode(ThemeStyle style, int? dockIconSize)
    {
        var s = Sanitize(style);
        var map = new Dictionary<string, object>
        {
            ["n"] = s.Name,
            ["a"] = s.Accent,
            ["r"] = Math.Round(s.RadiusCard, 1),
            ["b"] = Math.Round(s.GlassBlur, 1),
            ["g"] = Math.Round(s.GlassOpacity, 3),
            ["t"] = Math.Round(s.BarTint, 3),
            ["d"] = Math.Round(s.DockTint, 3),
        };
        if (dockIconSize is { } size)
            map["i"] = Math.Clamp(size, 32, 96);
        var json = JsonSerializer.Serialize(map);
        return Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static bool TryDecode(string? code, out Decoded? result, out string? error)
    {
        result = null;
        error = null;
        code = code?.Trim();
        if (string.IsNullOrEmpty(code) || code.Length > MaxLength)
        {
            error = "Kód je prázdný nebo moc dlouhý.";
            return false;
        }
        if (!code.StartsWith(Prefix, StringComparison.Ordinal))
        {
            error = "Tohle není kód motivu Better Windows (má začínat „BW1.“).";
            return false;
        }

        try
        {
            var b64 = code[Prefix.Length..].Replace('-', '+').Replace('_', '/');
            b64 = b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '=');
            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(b64)));
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                throw new FormatException();

            // Sdílený motiv nese vlastní barvu zvýraznění -> barvu z Windows nepoužívat.
            var style = new ThemeStyle { UseSystemAccent = false };
            int? icon = null;
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var v = prop.Value;
                switch (prop.Name)
                {
                    case "n" when v.ValueKind == JsonValueKind.String: style.Name = v.GetString()!; break;
                    case "a" when v.ValueKind == JsonValueKind.String: style.Accent = v.GetString()!; break;
                    case "r" when v.ValueKind == JsonValueKind.Number: style.RadiusCard = v.GetDouble(); break;
                    case "b" when v.ValueKind == JsonValueKind.Number: style.GlassBlur = v.GetDouble(); break;
                    case "g" when v.ValueKind == JsonValueKind.Number: style.GlassOpacity = v.GetDouble(); break;
                    case "t" when v.ValueKind == JsonValueKind.Number: style.BarTint = v.GetDouble(); break;
                    case "d" when v.ValueKind == JsonValueKind.Number: style.DockTint = v.GetDouble(); break;
                    case "i" when v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i): icon = Math.Clamp(i, 32, 96); break;
                    // Neznámé klíče ignorujeme – nikdy se nedostanou dál.
                }
            }
            result = new Decoded(Sanitize(style), icon);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or JsonException or DecoderFallbackException)
        {
            error = "Kód motivu je poškozený.";
            return false;
        }
    }

    /// <summary>Opraví hodnoty na bezpečné (i pro ručně upravený settings.json).</summary>
    public static ThemeStyle Sanitize(ThemeStyle? style)
    {
        style ??= new ThemeStyle();
        var def = new ThemeStyle();
        return new ThemeStyle
        {
            Name = CleanName(style.Name) is { Length: > 0 } n ? n : def.Name,
            UseSystemAccent = style.UseSystemAccent,
            Accent = HexColor().IsMatch(style.Accent ?? "") ? style.Accent!.ToLowerInvariant() : def.Accent,
            RadiusCard = Clamp(style.RadiusCard, 0, 40, def.RadiusCard),
            GlassBlur = Clamp(style.GlassBlur, 0, 60, def.GlassBlur),
            GlassOpacity = Clamp(style.GlassOpacity, 0, 1, def.GlassOpacity),
            BarTint = Clamp(style.BarTint, 0, 0.8, def.BarTint),
            DockTint = Clamp(style.DockTint, 0, 0.8, def.DockTint),
        };
    }

    private static double Clamp(double v, double min, double max, double fallback) =>
        double.IsFinite(v) ? Math.Clamp(v, min, max) : fallback;

    /// <summary>Jen písmena, číslice, mezery a běžná interpunkce; max. 40 znaků.</summary>
    private static string CleanName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "";
        var sb = new StringBuilder();
        foreach (var ch in name.Trim())
        {
            var cat = char.GetUnicodeCategory(ch);
            if (char.IsLetterOrDigit(ch) || ch == ' ' || "-_.,!?&()'+".Contains(ch) ||
                cat == UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
            if (sb.Length >= 40)
                break;
        }
        return sb.ToString().Trim();
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColor();
}
