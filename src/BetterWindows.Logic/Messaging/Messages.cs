using System.Text.Json;
using BetterWindows.Logic.Settings;

namespace BetterWindows.Logic.Messaging;

// ===== C# -> UI =====

public sealed record SettingsStatePayload(AppSettings Settings);

public sealed record ForegroundChangedPayload(string? AppId, string AppName, string Title);

public sealed record DockItem(string AppId, string Name, bool Pinned, bool Running, bool Active, int WindowCount);

/// <param name="TrashCount">Počet položek v koši (-1 = nevíme).</param>
public sealed record DockStatePayload(IReadOnlyList<DockItem> Items, long TrashCount = -1);

/// <summary>Ikony jako data URL (data:image/png;base64,...), klíč = appId.</summary>
public sealed record IconsResultPayload(IReadOnlyDictionary<string, string> Icons);

public sealed record BatteryStatePayload(bool Present, int? Percent, bool Charging, bool OnAc);

/// <param name="Status">"ok" | "loading" | "error" | "disabled"</param>
public sealed record WeatherStatePayload(
    string Status,
    string? City,
    double? Temperature,
    int? WeatherCode,
    bool IsDay,
    double? High,
    double? Low,
    DateTimeOffset? UpdatedAt,
    string? Error);

public sealed record PrivacyStatePayload(bool MicInUse, bool CamInUse, IReadOnlyList<string> MicApps, IReadOnlyList<string> CamApps);

/// <param name="Url">Adresa obrázku tapety (virtuální host), nebo null = jen barva.</param>
/// <param name="Color">Barva pozadí plochy (#rrggbb) jako záloha.</param>
public sealed record WallpaperStatePayload(string? Url, string Color);

/// <param name="Mode">Jak je vrstva plochy umístěná: "embed-raised" | "embed-classic" | "bottom" | "off" | "failed".</param>
public sealed record DesktopStatusPayload(string Mode, bool IconsHidden, string? Detail);

/// <summary>Hodnoty pásem 0..1, od nízkých frekvencí k vysokým.</summary>
public sealed record VisualizerFramePayload(IReadOnlyList<float> Bins);

public sealed record StoreValuePayload(string Key, JsonElement? Value);

public sealed record DiagnosticsPayload(string Version, string WindowsVersion, string LogDir, string DataDir, string? DesktopMode, string? DesktopDetail);

public sealed record PerformanceStatePayload(bool Reduced, string Reason);

/// <param name="Thumbnail">Obal alba jako data URL, nebo null.</param>
/// <param name="Position">Pozice v sekundách (null = neznámá).</param>
public sealed record MediaStatePayload(
    bool Available,
    string? Title,
    string? Artist,
    string? Source,
    string? Thumbnail,
    bool Playing,
    double? Position,
    double? Duration,
    bool CanPrevious,
    bool CanNext);

/// <param name="Level">Hlasitost 0..1.</param>
/// <param name="Changed">true = změna právě teď (ukázat ukazatel v ostrůvku).</param>
public sealed record VolumeStatePayload(double Level, bool Muted, bool Changed);

public sealed record StartAppItem(string AppId, string Name);

public sealed record StartAppsPayload(IReadOnlyList<StartAppItem> Apps);

public sealed record StartVisibilityPayload(bool Open);

public sealed record IslandExpandedPayload(bool Expanded);

/// <summary>Textové pole widgetu na ploše: kde je (CSS px v okně plochy), co v něm je, jak vypadá.</summary>
public sealed record DesktopTextInputPayload(string Key, CssRect Rect, string Value, string? Placeholder, double FontSize);

/// <summary>Výsledek psaní: Submit = Enter (widget má text rovnou použít), jinak jen rozepsaný text.</summary>
public sealed record DesktopTextResultPayload(string Key, string Value, bool Submit);

public sealed record ThemeCodePayload(string Code);

public sealed record ThemeImportResultPayload(bool Ok, string? Error);

public sealed record LockVisibilityPayload(bool Open, bool Editing);

/// <param name="Avatar">Obrázek účtu Windows jako data URL, nebo null.</param>
public sealed record UserStatePayload(string Name, string? Avatar);

/// <param name="Code">Krátký kód jazyka klávesnice (CES, ENG, …).</param>
public sealed record InputLanguagePayload(string Code, string Name);

/// <summary>Nastavení vzhledu z Windows, na která má rozhraní reagovat.</summary>
/// <param name="ReduceTransparency">Efekty průhlednosti vypnuté -> neprůhledné plochy místo skla.</param>
/// <param name="HighContrast">Režim vysokého kontrastu.</param>
/// <param name="Accent">Barva zvýraznění Windows #rrggbb.</param>
public sealed record SystemAppearancePayload(bool ReduceTransparency, bool HighContrast, string Accent);

// ===== UI -> C# =====

public sealed record UiReadyPayload(string? Page);

public sealed record IconsRequestPayload(IReadOnlyList<string> AppIds);

public sealed record DockActivatePayload(string AppId);

public sealed record DockContextMenuPayload(string AppId);

/// <summary>Obdélník v CSS px relativně k levému hornímu rohu stránky (okna).</summary>
public sealed record CssRect(double X, double Y, double Width, double Height);

/// <param name="Hit">Oblasti, které mají být vidět a reagovat na myš (zbytek okna kliknutí propouští).</param>
/// <param name="Band">Viditelný pás docku (pod ním se kreslí systémové rozmazání).</param>
public sealed record DockLayoutPayload(IReadOnlyList<CssRect> Hit, CssRect? Band);

public sealed record SettingsUpdatePayload(AppSettings Settings);

/// <param name="Action">"pickWallpaper" | "clearWallpaper" | "openLogs" | "openDataDir" | "restoreSystem" | "unpin" | "refreshWeather"</param>
public sealed record SettingsActionPayload(string Action, string? Arg);

public sealed record StoreGetPayload(string Key);

public sealed record StoreSetPayload(string Key, JsonElement? Value);

public sealed record UiLogPayload(string Level, string Message);

/// <param name="AppId">Ikona pod kurzorem (null = kurzor odešel).</param>
/// <param name="Rect">Ikona v CSS px relativně k oknu docku.</param>
public sealed record DockHoverPayload(string? AppId, CssRect? Rect);

/// <param name="Command">"playPause" | "next" | "previous"</param>
public sealed record MediaCommandPayload(string Command);

public sealed record VolumeSetPayload(double? Level, bool? Muted);

public sealed record StartLaunchPayload(string AppId);

public sealed record StartPinPayload(string AppId);

public sealed record ThemeImportPayload(string Code);

/// <param name="Preview">Jen náhled (z nastavení) – po odemknutí NEzamykat Windows.</param>
public sealed record LockOpenPayload(bool Editing, bool Preview = false);
