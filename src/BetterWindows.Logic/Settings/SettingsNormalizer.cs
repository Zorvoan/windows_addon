namespace BetterWindows.Logic.Settings;

/// <summary>Opraví nesmyslné hodnoty (ručně upravený soubor, chyba v UI) na rozumné.</summary>
public static class SettingsNormalizer
{
    public static AppSettings Normalize(AppSettings s)
    {
        s.Appearance ??= new();
        s.Weather ??= new();
        s.Dock ??= new();
        s.Desktop ??= new();
        s.Taskbar ??= new();
        s.Desktop.Visualizer ??= new();
        s.Start ??= new();
        s.Island ??= new();
        s.Performance ??= new();
        s.Lock ??= new();
        // Apple (dark-mode.md › Best practices): "Avoid offering an app-specific appearance setting."
        // Světlý/tmavý režim se řídí jen systémem; starší ruční volba se ruší.
        s.Appearance.Theme = ThemeMode.System;
        s.TopBar ??= new();
        s.Lock.IdleMinutes = Math.Clamp(s.Lock.IdleMinutes, 0, 240);
        s.Lock.WallpaperBlur = double.IsFinite(s.Lock.WallpaperBlur) ? Math.Clamp(s.Lock.WallpaperBlur, 0, 40) : 0;
        s.Lock.Dim = double.IsFinite(s.Lock.Dim) ? Math.Clamp(s.Lock.Dim, 0, 0.7) : 0.15;
        s.Appearance.Style = Themes.ThemeCodec.Sanitize(s.Appearance.Style);

        s.Weather.City = string.IsNullOrWhiteSpace(s.Weather.City) ? "Praha" : s.Weather.City.Trim();
        if (s.Weather.Resolved is { } r && !string.Equals(r.Query, s.Weather.City, StringComparison.OrdinalIgnoreCase))
            s.Weather.Resolved = null; // město se změnilo -> znovu vyhledat

        s.Dock.Pinned = (s.Dock.Pinned ?? [])
            .Where(p => p is not null && !string.IsNullOrWhiteSpace(p.AppId))
            .GroupBy(p => p.AppId, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
        s.Dock.IconSize = Math.Clamp(s.Dock.IconSize, 32, 96);
        s.Dock.MaxScale = double.IsFinite(s.Dock.MaxScale) ? Math.Clamp(s.Dock.MaxScale, 1.0, 2.5) : 1.7;

        s.Desktop.Widgets = (s.Desktop.Widgets ?? [])
            .Where(w => w is not null && WidgetTypes.All.Contains(w.Type))
            .GroupBy(w => w.Id)
            .Select(g => g.First())
            .Where(w => !string.IsNullOrWhiteSpace(w.Id))
            .Select(w =>
            {
                w.X = double.IsFinite(w.X) ? Math.Clamp(w.X, 0, 10000) : 24;
                w.Y = double.IsFinite(w.Y) ? Math.Clamp(w.Y, 0, 10000) : 64;
                return w;
            })
            .ToList();
        if (s.Desktop.WallpaperPath is { } wp && string.IsNullOrWhiteSpace(wp))
            s.Desktop.WallpaperPath = null;

        var v = s.Desktop.Visualizer;
        v.Sensitivity = double.IsFinite(v.Sensitivity) ? Math.Clamp(v.Sensitivity, 0.2, 3.0) : 1.0;

        return s;
    }
}
