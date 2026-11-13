using System.Net.Http;
using System.Windows.Threading;
using BetterWindows.Core;
using BetterWindows.Logic.Messaging;
using BetterWindows.Logic.Settings;
using BetterWindows.Logic.Weather;
using Microsoft.Win32;
using BetterWindows.Logic.Localization;

namespace BetterWindows.Services;

/// <summary>
/// Počasí z Open-Meteo (bez API klíče). Město z nastavení se jednou převede na souřadnice
/// (uloží se do nastavení), předpověď se obnovuje každých 15 minut, při chybě za 2 minuty
/// a po probuzení počítače.
/// </summary>
internal sealed class WeatherService : IDisposable
{
    private static readonly HttpClient Http = CreateClient();

    private readonly SettingsService _settings;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _timer;
    private bool _busy;

    public WeatherStatePayload Current { get; private set; } = new("loading", null, null, null, true, null, null, null, null);
    public event Action<WeatherStatePayload>? Changed;

    public WeatherService(SettingsService settings, Dispatcher dispatcher)
    {
        _settings = settings;
        _dispatcher = dispatcher;
        _timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher);
        _timer.Tick += (_, _) => Refresh();
        _settings.Changed += OnSettingsChanged;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    public void Start() => Refresh();

    public void Refresh() => Safe.Fire("Počasí", RefreshAsync);

    private async Task RefreshAsync()
    {
        if (_busy)
            return;
        var weather = _settings.Current.Weather;
        if (!weather.Enabled)
        {
            _timer.Stop();
            Publish(new WeatherStatePayload("disabled", null, null, null, true, null, null, null, null));
            return;
        }

        _busy = true;
        try
        {
            var location = weather.Resolved;
            if (location is null)
            {
                var json = await Http.GetStringAsync(OpenMeteo.GeocodingUrl(weather.City));
                location = OpenMeteo.ParseGeocoding(json, weather.City);
                if (location is null)
                {
                    PublishError(Loc.T("Město „{city}“ nenalezeno.", ("city", weather.City)));
                    Schedule(TimeSpan.FromMinutes(30));
                    return;
                }
                Log.Info($"Počasí: {weather.City} -> {location.Name}, {location.Country} ({location.Latitude}, {location.Longitude})");
                var resolved = location;
                _settings.Update(s => s.Weather.Resolved = resolved);
            }

            var forecast = await Http.GetStringAsync(OpenMeteo.ForecastUrl(location.Latitude, location.Longitude));
            Publish(OpenMeteo.ParseForecast(forecast, location.Name, DateTimeOffset.Now));
            Schedule(TimeSpan.FromMinutes(15));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or KeyNotFoundException or InvalidOperationException)
        {
            Log.Warn($"Počasí: {ex.Message}");
            PublishError(Loc.T("Počasí se nepodařilo načíst (bez připojení?)."));
            Schedule(TimeSpan.FromMinutes(2));
        }
        finally
        {
            _busy = false;
        }
    }

    private void Schedule(TimeSpan after)
    {
        _timer.Stop();
        _timer.Interval = after;
        _timer.Start();
    }

    private void PublishError(string message) =>
        Publish(Current with { Status = Current.Temperature is null ? "error" : Current.Status, Error = message });

    private void Publish(WeatherStatePayload state)
    {
        Current = state;
        Changed?.Invoke(state);
    }

    private void OnSettingsChanged(AppSettings old, AppSettings now)
    {
        if (old.Weather.City != now.Weather.City || old.Weather.Enabled != now.Weather.Enabled)
            Refresh();
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
            _dispatcher.InvokeAsync(() => Schedule(TimeSpan.FromSeconds(10))); // síť po probuzení chvíli naskakuje
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("BetterWindows/0.1");
        return client;
    }

    public void Dispose()
    {
        _timer.Stop();
        _settings.Changed -= OnSettingsChanged;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
    }
}
