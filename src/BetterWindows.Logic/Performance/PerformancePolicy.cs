using BetterWindows.Logic.Settings;
using BetterWindows.Logic.Localization;

namespace BetterWindows.Logic.Performance;

/// <summary>Jediný zdroj pravdy: mají se ztlumit animace? (Čtou to dock, vizualizér, widgety, plocha.)</summary>
public static class PerformancePolicy
{
    /// <returns>(ztlumit, důvod pro uživatele)</returns>
    public static (bool Reduced, string Reason) Evaluate(AppSettings settings, bool onBattery, bool batterySaver, bool fullscreen)
    {
        if (settings.Appearance.ReduceMotion)
            return (true, Loc.T("Omezit animace je zapnuto ve Vzhledu"));

        var p = settings.Performance;
        switch (p.Mode)
        {
            case PerformanceMode.AlwaysReduced:
                return (true, Loc.T("Režim výkonu: vždy úsporně"));
            case PerformanceMode.NeverReduced:
                return (false, Loc.T("Režim výkonu: vždy plné animace"));
        }

        if (p.ReduceOnFullscreen && fullscreen)
            return (true, Loc.T("Aplikace přes celou obrazovku"));
        if (batterySaver)
            return (true, Loc.T("Úspora energie Windows"));
        if (p.ReduceOnBattery && onBattery)
            return (true, Loc.T("Napájení z baterie"));
        return (false, Loc.T("Plné animace"));
    }
}
