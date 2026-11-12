namespace BetterWindows.Logic.Messaging;

/// <summary>
/// Názvy zpráv mezi C# a JS. Zrcadlo v TS: ui/src/lib/bridge/messages.ts.
/// Každá zpráva má tvar { "type": "...", "payload": { ... } }.
/// </summary>
public static class MessageTypes
{
    // ---- C# -> UI ----
    public const string SettingsState = "settings.state";
    public const string ForegroundChanged = "foreground.changed";
    public const string DockState = "dock.state";
    public const string IconsResult = "icons.result";
    public const string BatteryState = "battery.state";
    public const string WeatherState = "weather.state";
    public const string PrivacyState = "privacy.state";
    public const string WallpaperState = "wallpaper.state";
    public const string DesktopStatus = "desktop.status";
    public const string VisualizerFrame = "visualizer.frame";
    public const string StoreValue = "store.value";
    public const string Diagnostics = "diagnostics.state";
    public const string PerformanceState = "performance.state";
    public const string MediaState = "media.state";
    public const string VolumeState = "volume.state";
    public const string StartApps = "start.apps";
    public const string StartVisibility = "start.visibility";
    public const string ThemeCode = "theme.code";
    public const string ThemeImportResult = "theme.importResult";

    // ---- UI -> C# ----
    public const string UiReady = "ui.ready";
    public const string IconsRequest = "icons.request";
    public const string DockActivate = "dock.activate";
    public const string DockContextMenu = "dock.contextMenu";
    public const string DockLayout = "dock.layout";
    public const string SettingsUpdate = "settings.update";
    public const string SettingsOpen = "settings.open";
    public const string SettingsAction = "settings.action";
    public const string StoreGet = "store.get";
    public const string StoreSet = "store.set";
    public const string UiLog = "ui.log";
    public const string DockHover = "dock.hover";
    public const string IslandLayout = "island.layout";
    public const string MediaCommand = "media.command";
    public const string VolumeSet = "volume.set";
    public const string StartLaunch = "start.launch";
    public const string StartClose = "start.close";
    public const string StartToggle = "start.toggle";
    public const string StartPin = "start.pin";
    public const string ThemeExport = "theme.export";
    public const string ThemeImport = "theme.import";
}
