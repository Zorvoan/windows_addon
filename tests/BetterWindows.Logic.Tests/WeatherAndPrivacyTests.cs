using System.Globalization;
using BetterWindows.Logic.Privacy;
using BetterWindows.Logic.Weather;

namespace BetterWindows.Logic.Tests;

public class WeatherAndPrivacyTests
{
    [Fact]
    public void Forecast_url_uses_dot_even_in_czech_culture()
    {
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("cs-CZ");
            var url = OpenMeteo.ForecastUrl(50.0755, 14.4378);
            Assert.Contains("latitude=50.0755&longitude=14.4378", url);
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    [Fact]
    public void Geocoding_parse()
    {
        const string json = """{"results":[{"id":1,"name":"Praha","latitude":50.08804,"longitude":14.42076,"country":"Česko"}]}""";
        var loc = OpenMeteo.ParseGeocoding(json, "praha")!;
        Assert.Equal("Praha", loc.Name);
        Assert.Equal("praha", loc.Query);
        Assert.Equal(50.08804, loc.Latitude, 5);
        Assert.Null(OpenMeteo.ParseGeocoding("""{"generationtime_ms":0.5}""", "xyz"));
    }

    [Fact]
    public void Forecast_parse()
    {
        const string json = """
        {"current":{"time":"2026-11-12T22:00","temperature_2m":4.3,"weather_code":3,"is_day":0},
         "daily":{"time":["2026-11-12"],"temperature_2m_max":[8.1],"temperature_2m_min":[1.2]}}
        """;
        var w = OpenMeteo.ParseForecast(json, "Praha", DateTimeOffset.UnixEpoch);
        Assert.Equal("ok", w.Status);
        Assert.Equal(4.3, w.Temperature);
        Assert.Equal(3, w.WeatherCode);
        Assert.False(w.IsDay);
        Assert.Equal(8.1, w.High);
        Assert.Equal(1.2, w.Low);
    }

    [Fact]
    public void Consent_store_in_use_and_names()
    {
        var entries = new[]
        {
            new ConsentEntry("C:#Program Files#Zoom#bin#Zoom.exe", true, 133000000000000000, 0),
            new ConsentEntry("Microsoft.WindowsCamera_8wekyb3d8bbwe", false, 133000000000000000, 133000000000000999),
            new ConsentEntry("Microsoft.SkypeApp_kzf8qxf38zg5c", false, 133000000000000000, 0),
            new ConsentEntry("C:#never#used.exe", true, null, null),
            new ConsentEntry("C:#zero#start.exe", true, 0, 0),
        };
        Assert.Equal(["SkypeApp", "Zoom"], ConsentStore.InUseApps(entries));
    }
}
