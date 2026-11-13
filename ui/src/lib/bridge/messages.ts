/**
 * Typy zpráv mezi C# a JS. Zrcadlo C#: src/BetterWindows.Logic/Messaging/Messages.cs
 * a Settings/AppSettings.cs. Každá zpráva má tvar { type, payload }.
 * Při změně uprav obě strany; test ContractTests + UI harness rozdíl odhalí.
 */

// ---------- Nastavení ----------
export type ThemeMode = 'system' | 'light' | 'dark';
export type BackdropMode = 'systemAcrylic' | 'accentAcrylic' | 'none';
export type DesktopMode = 'auto' | 'embed' | 'bottom';
export type VisualizerScene = 'dots' | 'wave' | 'ring';
export type WidgetType = 'clock' | 'calendar' | 'weather' | 'reminders' | 'battery';

export interface ResolvedLocation {
  query: string;
  name: string;
  country: string | null;
  latitude: number;
  longitude: number;
}

export interface PinnedApp {
  appId: string;
  name: string;
}

export interface WidgetPlacement {
  id: string;
  type: WidgetType;
  x: number;
  y: number;
}

export type PerformanceMode = 'auto' | 'alwaysReduced' | 'neverReduced';
export type LockClockStyle = 'large' | 'thin' | 'bold' | 'analog';
export type LockClockPosition = 'top' | 'center';

export interface LockSettings {
  enabled: boolean;
  idleMinutes: number;
  requireWindowsLock: boolean;
  clockStyle: LockClockStyle;
  clockPosition: LockClockPosition;
  showSeconds: boolean;
  showMedia: boolean;
  showWeather: boolean;
  showBattery: boolean;
  showUser: boolean;
  wallpaperBlur: number;
  dim: number;
}

export interface ThemeStyle {
  name: string;
  useSystemAccent: boolean;
  accent: string;
  radiusCard: number;
  glassBlur: number;
  glassOpacity: number;
  barTint: number;
  dockTint: number;
}

export interface AppSettings {
  version: number;
  appearance: {
    theme: ThemeMode;
    topBarBackdrop: BackdropMode;
    dockBackdrop: BackdropMode;
    reduceMotion: boolean;
    style: ThemeStyle;
  };
  weather: {
    enabled: boolean;
    city: string;
    resolved: ResolvedLocation | null;
  };
  dock: {
    enabled: boolean;
    pinned: PinnedApp[];
    iconSize: number;
    magnification: boolean;
    previews: boolean;
    showLaunchpad: boolean;
    showTrash: boolean;
    maxScale: number;
  };
  desktop: {
    enabled: boolean;
    mode: DesktopMode;
    hideIcons: boolean;
    wallpaperPath: string | null;
    showWidgets: boolean;
    showTestCard: boolean;
    widgets: WidgetPlacement[];
    visualizer: {
      enabled: boolean;
      scene: VisualizerScene;
      sensitivity: number;
    };
  };
  taskbar: {
    hide: boolean;
  };
  start: {
    enabled: boolean;
    replaceWinKey: boolean;
  };
  island: {
    enabled: boolean;
    showVolume: boolean;
  };
  performance: {
    mode: PerformanceMode;
    reduceOnBattery: boolean;
    reduceOnFullscreen: boolean;
  };
  lock: LockSettings;
  topBar: {
    showNowPlaying: boolean;
    showLanguage: boolean;
    showUser: boolean;
    showExit: boolean;
  };
}

export interface MediaState {
  available: boolean;
  title: string | null;
  artist: string | null;
  source: string | null;
  thumbnail: string | null;
  playing: boolean;
  position: number | null;
  duration: number | null;
  canPrevious: boolean;
  canNext: boolean;
}

export interface VolumeState {
  level: number;
  muted: boolean;
  changed: boolean;
}

export interface StartApp {
  appId: string;
  name: string;
}

// ---------- Stavy z C# ----------
export interface DockItem {
  appId: string;
  name: string;
  pinned: boolean;
  running: boolean;
  active: boolean;
  windowCount: number;
}

export interface BatteryState {
  present: boolean;
  percent: number | null;
  charging: boolean;
  onAc: boolean;
}

export interface WeatherState {
  status: 'ok' | 'loading' | 'error' | 'disabled';
  city: string | null;
  temperature: number | null;
  weatherCode: number | null;
  isDay: boolean;
  high: number | null;
  low: number | null;
  updatedAt: string | null;
  error: string | null;
}

export interface PrivacyState {
  micInUse: boolean;
  camInUse: boolean;
  micApps: string[];
  camApps: string[];
}

export interface Diagnostics {
  version: string;
  windowsVersion: string;
  logDir: string;
  dataDir: string;
  desktopMode: string | null;
  desktopDetail: string | null;
}

export interface CssRect {
  x: number;
  y: number;
  width: number;
  height: number;
}

// ---------- C# -> UI ----------
export type HostMessage =
  | { type: 'settings.state'; payload: { settings: AppSettings } }
  | { type: 'foreground.changed'; payload: { appId: string | null; appName: string; title: string } }
  | { type: 'dock.state'; payload: { items: DockItem[]; trashCount: number } }
  | { type: 'icons.result'; payload: { icons: Record<string, string> } }
  | { type: 'battery.state'; payload: BatteryState }
  | { type: 'weather.state'; payload: WeatherState }
  | { type: 'privacy.state'; payload: PrivacyState }
  | { type: 'wallpaper.state'; payload: { url: string | null; color: string } }
  | { type: 'desktop.status'; payload: { mode: string; iconsHidden: boolean; detail: string | null } }
  | { type: 'visualizer.frame'; payload: { bins: number[] } }
  | { type: 'store.value'; payload: { key: string; value: unknown } }
  | { type: 'diagnostics.state'; payload: Diagnostics }
  | { type: 'performance.state'; payload: { reduced: boolean; reason: string } }
  | { type: 'media.state'; payload: MediaState }
  | { type: 'volume.state'; payload: VolumeState }
  | { type: 'start.apps'; payload: { apps: StartApp[] } }
  | { type: 'start.visibility'; payload: { open: boolean } }
  | { type: 'island.absorb'; payload: Record<string, never> }
  | { type: 'dock.absorb'; payload: { appId: string } }
  | { type: 'desktop.textResult'; payload: { key: string; value: string; submit: boolean } }
  | { type: 'island.expanded'; payload: { expanded: boolean } }
  | { type: 'theme.code'; payload: { code: string } }
  | { type: 'theme.importResult'; payload: { ok: boolean; error: string | null } }
  | { type: 'lock.visibility'; payload: { open: boolean; editing: boolean } }
  | { type: 'user.state'; payload: { name: string; avatar: string | null } }
  | { type: 'input.language'; payload: { code: string; name: string } }
  | { type: 'system.appearance'; payload: { reduceTransparency: boolean; highContrast: boolean; accent: string } };

// ---------- UI -> C# ----------
export type SettingsAction =
  | 'pickWallpaper'
  | 'clearWallpaper'
  | 'openLogs'
  | 'openDataDir'
  | 'restoreSystem'
  | 'unpin'
  | 'refreshWeather';

export type UiMessage =
  | { type: 'ui.ready'; payload: { page: string } }
  | { type: 'ui.log'; payload: { level: 'info' | 'warn' | 'error'; message: string } }
  | { type: 'icons.request'; payload: { appIds: string[] } }
  | { type: 'dock.activate'; payload: { appId: string } }
  | { type: 'dock.contextMenu'; payload: { appId: string } }
  | { type: 'dock.layout'; payload: { hit: CssRect[]; band: CssRect | null; icons?: { appId: string; rect: CssRect }[] } }
  | { type: 'settings.update'; payload: { settings: AppSettings } }
  | { type: 'settings.open'; payload: Record<string, never> }
  | { type: 'settings.action'; payload: { action: SettingsAction; arg: string | null } }
  | { type: 'store.get'; payload: { key: string } }
  | { type: 'store.set'; payload: { key: string; value: unknown } }
  | { type: 'dock.hover'; payload: { appId: string | null; rect: CssRect | null } }
  | { type: 'island.layout'; payload: { hit: CssRect[]; band: CssRect | null } }
  | { type: 'island.expanded'; payload: { expanded: boolean } }
  | { type: 'desktop.textInput'; payload: { key: string; rect: CssRect; value: string; placeholder: string | null; fontSize: number } }
  | { type: 'media.command'; payload: { command: 'playPause' | 'next' | 'previous' } }
  | { type: 'volume.set'; payload: { level: number | null; muted: boolean | null } }
  | { type: 'start.launch'; payload: { appId: string } }
  | { type: 'start.close'; payload: Record<string, never> }
  | { type: 'start.toggle'; payload: Record<string, never> }
  | { type: 'start.pin'; payload: { appId: string } }
  | { type: 'theme.export'; payload: Record<string, never> }
  | { type: 'theme.import'; payload: { code: string } }
  | { type: 'lock.open'; payload: { editing: boolean; preview?: boolean } }
  | { type: 'lock.dismiss'; payload: Record<string, never> }
  | { type: 'lock.editDone'; payload: Record<string, never> }
  | { type: 'app.exit'; payload: Record<string, never> }
  | { type: 'trash.empty'; payload: Record<string, never> };

export type HostType = HostMessage['type'];
export type HostPayload<T extends HostType> = Extract<HostMessage, { type: T }>['payload'];
export type UiType = UiMessage['type'];
export type UiPayload<T extends UiType> = Extract<UiMessage, { type: T }>['payload'];
