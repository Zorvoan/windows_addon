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

public sealed class AppSettings
{
    public int Version { get; set; } = 1;
    public AppearanceSettings Appearance { get; set; } = new();
    public WeatherSettings Weather { get; set; } = new();
    public DockSettings Dock { get; set; } = new();
    public DesktopSettings Desktop { get; set; } = new();
    public TaskbarSettings Taskbar { get; set; } = new();
}

public sealed class AppearanceSettings
{
    public ThemeMode Theme { get; set; } = ThemeMode.System;
    public BackdropMode TopBarBackdrop { get; set; } = BackdropMode.SystemAcrylic;
    public BackdropMode DockBackdrop { get; set; } = BackdropMode.SystemAcrylic;
    /// <summary>Ztlumit animace (připraveno pro režim úspory výkonu).</summary>
    public bool ReduceMotion { get; set; }
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
    /// <summary>Největší zvětšení ikony pod kurzorem (1.0 = žádné).</summary>
    public double MaxScale { get; set; } = 1.7;

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
    public bool ShowTestCard { get; set; }
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

public sealed class TaskbarSettings
{
    /// <summary>Skrýt hlavní panel Windows. Výchozí VYPNUTO.</summary>
    public bool Hide { get; set; }
}
