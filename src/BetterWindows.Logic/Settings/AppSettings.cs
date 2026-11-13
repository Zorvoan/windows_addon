namespace BetterWindows.Logic.Settings;

// Model nastavení uloženého v %AppData%\BetterWindows\settings.json.
// Stejný tvar (camelCase) dostává UI ve zprávě "settings.state" – viz ui/src/lib/bridge/messages.ts.

public enum ThemeMode { System, Light, Dark }

/// <summary>Jak se kreslí rozmazané pozadí horní lišty a docku.</summary>
public enum BackdropMode
{
    /// <summary>Oficiální Windows 11 API (DWMWA_SYSTEMBACKDROP_TYPE = Acrylic). Tón určuje systém.</summary>
    SystemAcrylic,
    /// <summary>Nedokumentované API z Windows 10. Vlastní barva a průhlednost tónu.</summary>
    AccentAcrylic,
    /// <summary>Bez rozmazání.</summary>
    None,
}

/// <summary>Jak se okno plochy (DesktopLayer) dostane pod aplikace.</summary>
public enum DesktopMode
{
    /// <summary>Zkusí vložení do plochy Windows, při neúspěchu HWND_BOTTOM.</summary>
    Auto,
    /// <summary>Jen vložení do plochy (WorkerW / Progman).</summary>
    Embed,
    /// <summary>Jen samostatné okno držené úplně vespod (HWND_BOTTOM).</summary>
    Bottom,
}

public enum VisualizerScene { Dots, Wave, Ring }

public enum LockClockStyle { Large, Thin, Bold, Analog }

public enum LockClockPosition { Top, Center }

/// <summary>Režim výkonu: Auto = ztlumit animace podle situace (baterie, celá obrazovka).</summary>
public enum PerformanceMode { Auto, AlwaysReduced, NeverReduced }

public sealed class AppSettings
{
    public int Version { get; set; } = SettingsNormalizer.CurrentVersion;
    public AppearanceSettings Appearance { get; set; } = new();
    public WeatherSettings Weather { get; set; } = new();
    public DockSettings Dock { get; set; } = new();
    public DesktopSettings Desktop { get; set; } = new();
    public TaskbarSettings Taskbar { get; set; } = new();
    public StartSettings Start { get; set; } = new();
    public IslandSettings Island { get; set; } = new();
    public PerformanceSettings Performance { get; set; } = new();
    public LockSettings Lock { get; set; } = new();
    public TopBarSettings TopBar { get; set; } = new();
}

public sealed class AppearanceSettings
{
    public ThemeMode Theme { get; set; } = ThemeMode.System;
    public BackdropMode TopBarBackdrop { get; set; } = BackdropMode.SystemAcrylic;
    public BackdropMode DockBackdrop { get; set; } = BackdropMode.SystemAcrylic;
    /// <summary>Ztlumit animace vždy (ruční volba).</summary>
    public bool ReduceMotion { get; set; }
    /// <summary>Motiv: barvy, průhlednost, rozmazání, zaoblení (sdílí se kódem, viz ThemeCodec).</summary>
    public ThemeStyle Style { get; set; } = new();
}

/// <summary>
/// Vizuální motiv – přepisuje designové tokeny v UI. Nesahá na systémové rozmazání (tón určuje Windows)
/// ani na nativní kontextovou nabídku.
/// </summary>
public sealed class ThemeStyle
{
    public string Name { get; set; } = "Výchozí";
    /// <summary>
    /// Barva zvýraznění podle Windows (Nastavení › Přizpůsobení › Barvy). Apple: když si člověk zvolí
    /// barvu zvýraznění v systému, aplikace ji má použít (color.md › App accent colors).
    /// </summary>
    public bool UseSystemAccent { get; set; } = true;
    /// <summary>Vlastní barva zvýraznění #rrggbb (když UseSystemAccent = false).</summary>
    public string Accent { get; set; } = "#3a6df0";
    /// <summary>Zaoblení karet a widgetů (px).</summary>
    public double RadiusCard { get; set; } = 22;
    /// <summary>Rozmazání skla widgetů (px).</summary>
    public double GlassBlur { get; set; } = 28;
    /// <summary>Neprůhlednost skla widgetů 0..1.</summary>
    public double GlassOpacity { get; set; } = 0.35;
    /// <summary>Tón přes rozmazání horní lišty 0..0.8.</summary>
    public double BarTint { get; set; } = 0.12;
    /// <summary>Tón přes rozmazání docku 0..0.8.</summary>
    public double DockTint { get; set; } = 0.16;
}

public sealed class WeatherSettings
{
    public bool Enabled { get; set; } = true;
    public string City { get; set; } = "Praha";
    /// <summary>Výsledek geokódování (ať se město nehledá při každém startu). Smaže se při změně města.</summary>
    public ResolvedLocation? Resolved { get; set; }
}

public sealed class ResolvedLocation
{
    public string Query { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Country { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public sealed class DockSettings
{
    public bool Enabled { get; set; } = true;
    public List<PinnedApp> Pinned { get; set; } = DefaultPinned();
    /// <summary>Velikost ikony v klidu (WPF jednotky / CSS px).</summary>
    public int IconSize { get; set; } = 48;
    public bool Magnification { get; set; } = true;
    /// <summary>Náhledy oken při najetí na ikonu.</summary>
    public bool Previews { get; set; } = true;
    /// <summary>Ikona Launchpadu (Start) na začátku docku.</summary>
    public bool ShowLaunchpad { get; set; } = true;
    /// <summary>Koš na konci docku.</summary>
    public bool ShowTrash { get; set; } = true;
    /// <summary>Největší zvětšení ikony pod kurzorem (1.0 = žádné).</summary>
    public double MaxScale { get; set; } = 1.1;

    public static List<PinnedApp> DefaultPinned()
    {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var list = new List<PinnedApp>();
        if (!string.IsNullOrEmpty(windows))
            list.Add(new PinnedApp { AppId = Apps.AppId.FromExePath(Path.Combine(windows, "explorer.exe")), Name = "Průzkumník souborů" });
        list.Add(new PinnedApp
        {
            AppId = Apps.AppId.FromAumid("windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel"),
            Name = "Nastavení",
        });
        return list;
    }
}

public sealed class PinnedApp
{
    public string AppId { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class DesktopSettings
{
    public bool Enabled { get; set; } = true;
    public DesktopMode Mode { get; set; } = DesktopMode.Auto;
    /// <summary>Skrýt ikony plochy Windows (jen když je vrstva plochy úspěšně vložená).</summary>
    public bool HideIcons { get; set; } = true;
    /// <summary>Vlastní tapeta. null = aktuální tapeta Windows.</summary>
    public string? WallpaperPath { get; set; }
    public bool ShowWidgets { get; set; } = true;
    /// <summary>Testovací skleněná karta z kroku 2 (diagnostika vrstvy plochy).</summary>
    public bool ShowTestCard { get; set; } = true;
    public List<WidgetPlacement> Widgets { get; set; } = DefaultWidgets();
    public VisualizerSettings Visualizer { get; set; } = new();

    public static List<WidgetPlacement> DefaultWidgets() =>
    [
        new() { Id = "clock-1", Type = WidgetTypes.Clock, X = 24, Y = 64 },
        new() { Id = "calendar-1", Type = WidgetTypes.Calendar, X = 196, Y = 64 },
        new() { Id = "weather-1", Type = WidgetTypes.Weather, X = 24, Y = 236 },
        new() { Id = "reminders-1", Type = WidgetTypes.Reminders, X = 196, Y = 236 },
        new() { Id = "battery-1", Type = WidgetTypes.Battery, X = 24, Y = 408 },
    ];
}

public static class WidgetTypes
{
    public const string Clock = "clock";
    public const string Calendar = "calendar";
    public const string Weather = "weather";
    public const string Reminders = "reminders";
    public const string Battery = "battery";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Clock, Calendar, Weather, Reminders, Battery };
}

public sealed class WidgetPlacement
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    /// <summary>Pozice levého horního rohu v CSS px od levého horního rohu monitoru.</summary>
    public double X { get; set; }
    public double Y { get; set; }
}

public sealed class VisualizerSettings
{
    public bool Enabled { get; set; } = true;
    public VisualizerScene Scene { get; set; } = VisualizerScene.Dots;
    public double Sensitivity { get; set; } = 1.0;
}

public sealed class StartSettings
{
    public bool Enabled { get; set; } = true;
    /// <summary>Otevírat Start Better Windows klávesou Windows. Výchozí VYPNUTO.</summary>
    public bool ReplaceWinKey { get; set; }
}

public sealed class IslandSettings
{
    public bool Enabled { get; set; } = true;
    /// <summary>Při změně hlasitosti ukázat ukazatel v ostrůvku.</summary>
    public bool ShowVolume { get; set; } = true;
}

public sealed class PerformanceSettings
{
    public PerformanceMode Mode { get; set; } = PerformanceMode.Auto;
    public bool ReduceOnBattery { get; set; } = true;
    public bool ReduceOnFullscreen { get; set; } = true;
}

/// <summary>
/// Vlastní zamykací obrazovka – ukáže se PŘED zámkem Windows (není to zabezpečení, to dělá až zámek Windows).
/// </summary>
public sealed class LockSettings
{
    public bool Enabled { get; set; } = true;
    /// <summary>Ukázat po N minutách nečinnosti (0 = nikdy). Ne při videu / celé obrazovce.</summary>
    public int IdleMinutes { get; set; }
    /// <summary>Po odemknutí naší obrazovky zamknout Windows (heslo / PIN / Hello).</summary>
    public bool RequireWindowsLock { get; set; } = true;
    public LockClockStyle ClockStyle { get; set; } = LockClockStyle.Large;
    public LockClockPosition ClockPosition { get; set; } = LockClockPosition.Top;
    public bool ShowSeconds { get; set; }
    public bool ShowMedia { get; set; } = true;
    public bool ShowWeather { get; set; } = true;
    public bool ShowBattery { get; set; } = true;
    public bool ShowUser { get; set; } = true;
    /// <summary>Rozmazání tapety (px).</summary>
    public double WallpaperBlur { get; set; } = 0;
    /// <summary>Ztmavení tapety 0..0.7 (kvůli čitelnosti bílého textu na libovolné tapetě).</summary>
    public double Dim { get; set; } = 0.25;
}

public sealed class TopBarSettings
{
    public bool ShowNowPlaying { get; set; } = true;
    public bool ShowLanguage { get; set; } = true;
    public bool ShowUser { get; set; } = true;
    public bool ShowExit { get; set; } = true;
}

public sealed class TaskbarSettings
{
    /// <summary>Skrýt hlavní panel Windows. Výchozí VYPNUTO.</summary>
    public bool Hide { get; set; }
}
