using BetterWindows.Core;
using BetterWindows.Logic.Messaging;
using BetterWindows.Logic.Performance;

namespace BetterWindows.Services;

/// <summary>
/// Režim výkonu – JEDINÝ zdroj pravdy, jestli ztlumit animace (PerformancePolicy v Logic):
/// ruční volba, aplikace přes celou obrazovku, baterie, úspora energie Windows.
/// UI dostane performance.state (data-motion="reduced"), vizualizér se pozastaví.
/// </summary>
internal sealed class PerformanceService
{
    private readonly SettingsService _settings;
    private readonly BatteryService _battery;
    private bool _fullscreen;

    public bool Reduced { get; private set; }
    public string Reason { get; private set; } = "";
    public bool Fullscreen => _fullscreen;

    /// <summary>Změnil se výsledek. Volá se v UI vlákně.</summary>
    public event Action? Changed;

    public PerformanceService(SettingsService settings, BatteryService battery)
    {
        _settings = settings;
        _battery = battery;
        _settings.Changed += (_, _) => Recompute();
        _battery.Changed += _ => Recompute();
        Recompute(force: true);
    }

    public void SetFullscreen(bool fullscreen)
    {
        _fullscreen = fullscreen;
        Recompute();
    }

    private void Recompute(bool force = false)
    {
        var b = _battery.Current;
        var (reduced, reason) = PerformancePolicy.Evaluate(_settings.Current, b.Present && !b.OnAc, _battery.BatterySaver, _fullscreen);
        if (!force && reduced == Reduced && reason == Reason)
            return;
        Reduced = reduced;
        Reason = reason;
        Log.Info($"Výkon: {(reduced ? "ztlumené animace" : "plné animace")} ({reason})");
        Changed?.Invoke();
    }

    public PerformanceStatePayload State => new(Reduced, Reason);
}
