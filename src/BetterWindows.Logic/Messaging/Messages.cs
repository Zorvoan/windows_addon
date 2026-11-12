using System.Text.Json;
using BetterWindows.Logic.Settings;

namespace BetterWindows.Logic.Messaging;

// ===== C# -> UI =====

public sealed record SettingsStatePayload(AppSettings Settings);

public sealed record ForegroundChangedPayload(string? AppId, string AppName, string Title);

public sealed record DockItem(string AppId, string Name, bool Pinned, bool Running, bool Active, int WindowCount);

public sealed record DockStatePayload(IReadOnlyList<DockItem> Items);

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
