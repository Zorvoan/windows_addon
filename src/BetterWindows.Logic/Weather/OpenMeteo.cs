using System.Globalization;
using System.Text.Json;
using BetterWindows.Logic.Messaging;
using BetterWindows.Logic.Settings;

namespace BetterWindows.Logic.Weather;

/// <summary>
/// Open-Meteo (open-meteo.com): počasí zdarma a bez API klíče.
/// 1. geokódování města -> souřadnice, 2. předpověď pro souřadnice.
/// Čísla vždy v invariantní kultuře (česká by napsala desetinnou čárku a URL by nefungovala).
/// </summary>
public static class OpenMeteo
{
    public static string GeocodingUrl(string city, string language = "cs") =>
        $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language={language}&format=json";

    public static string ForecastUrl(double latitude, double longitude) =>
        string.Format(CultureInfo.InvariantCulture,
            "https://api.open-meteo.com/v1/forecast?latitude={0:0.####}&longitude={1:0.####}" +
            "&current=temperature_2m,weather_code,is_day&daily=temperature_2m_max,temperature_2m_min&timezone=auto&forecast_days=1",
            latitude, longitude);

    /// <summary>Vrátí první nalezené místo, nebo null když město neexistuje.</summary>
    public static ResolvedLocation? ParseGeocoding(string json, string query)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0)
            return null;

        var first = results[0];
        return new ResolvedLocation
        {
            Query = query,
            Name = first.TryGetProperty("name", out var n) ? n.GetString() ?? query : query,
            Country = first.TryGetProperty("country", out var c) ? c.GetString() : null,
            Latitude = first.GetProperty("latitude").GetDouble(),
            Longitude = first.GetProperty("longitude").GetDouble(),
        };
    }

    public static WeatherStatePayload ParseForecast(string json, string cityName, DateTimeOffset now)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var current = root.GetProperty("current");

        double? high = null, low = null;
        if (root.TryGetProperty("daily", out var daily))
        {
            high = FirstNumber(daily, "temperature_2m_max");
            low = FirstNumber(daily, "temperature_2m_min");
        }

        return new WeatherStatePayload(
            Status: "ok",
            City: cityName,
            Temperature: current.GetProperty("temperature_2m").GetDouble(),
            WeatherCode: current.TryGetProperty("weather_code", out var wc) ? wc.GetInt32() : null,
            IsDay: !current.TryGetProperty("is_day", out var d) || d.GetInt32() == 1,
            High: high,
            Low: low,
            UpdatedAt: now,
            Error: null);
    }

    private static double? FirstNumber(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var arr) && arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0 &&
        arr[0].ValueKind == JsonValueKind.Number
            ? arr[0].GetDouble()
            : null;
}
