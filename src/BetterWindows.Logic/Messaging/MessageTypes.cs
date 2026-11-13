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
    public const string LockVisibility = "lock.visibility";
    public const string UserState = "user.state";
    public const string InputLanguage = "input.language";
    public const string SystemAppearance = "system.appearance";

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
    /// <summary>Ostrůvek se rozbaluje / sbaluje. Posílá ostrůvek, C# přepošle liště (ta rozmaže svůj obsah).</summary>
    public const string IslandExpanded = "island.expanded";
    /// <summary>
    /// Plocha -> C#: uživatel klikl do textového pole widgetu. Vrstva plochy klávesnici nedostane
    /// (fokus má plocha Windows), proto C# přes pole ukáže skutečné vstupní okno.
    /// </summary>
    public const string DesktopTextInput = "desktop.textInput";
    /// <summary>C# -> ostrůvek: okno hrající aplikace právě vletělo do ostrůvku (krátké nafouknutí).</summary>
    public const string IslandAbsorb = "island.absorb";
    /// <summary>C# -> dock: minimalizované okno zajelo do ikony aplikace (zhoupnutí ikony).</summary>
    public const string DockAbsorb = "dock.absorb";
    /// <summary>C# -> plocha: text z plovoucího vstupního pole (psaní do widgetů, viz desktop.textInput).</summary>
    public const string DesktopTextResult = "desktop.textResult";
    public const string MediaCommand = "media.command";
    public const string VolumeSet = "volume.set";
    public const string StartLaunch = "start.launch";
    public const string StartClose = "start.close";
    public const string StartToggle = "start.toggle";
    public const string StartPin = "start.pin";
    public const string ThemeExport = "theme.export";
    public const string ThemeImport = "theme.import";
    public const string LockOpen = "lock.open";
    public const string LockDismiss = "lock.dismiss";
    public const string LockEditDone = "lock.editDone";
    public const string AppExit = "app.exit";
    public const string TrashEmpty = "trash.empty";
}
