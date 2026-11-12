using BetterWindows.Logic.Settings;

namespace BetterWindows.Logic.Performance;

/// <summary>Jediný zdroj pravdy: mají se ztlumit animace? (Čtou to dock, vizualizér, widgety, plocha.)</summary>
public static class PerformancePolicy
{
    /// <returns>(ztlumit, důvod pro uživatele)</returns>
    public static (bool Reduced, string Reason) Evaluate(AppSettings settings, bool onBattery, bool batterySaver, bool fullscreen)
    {
        if (settings.Appearance.ReduceMotion)
            return (true, "Omezit animace je zapnuto ve Vzhledu");

        var p = settings.Performance;
        switch (p.Mode)
        {
            case PerformanceMode.AlwaysReduced:
                return (true, "Režim výkonu: vždy úsporně");
            case PerformanceMode.NeverReduced:
                return (false, "Režim výkonu: vždy plné animace");
        }

        if (p.ReduceOnFullscreen && fullscreen)
            return (true, "Aplikace přes celou obrazovku");
        if (batterySaver)
            return (true, "Úspora energie Windows");
        if (p.ReduceOnBattery && onBattery)
            return (true, "Napájení z baterie");
        return (false, "Plné animace");
    }
}
