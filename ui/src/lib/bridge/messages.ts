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

export interface AppSettings {
  version: number;
  appearance: {
    theme: ThemeMode;
    topBarBackdrop: BackdropMode;
    dockBackdrop: BackdropMode;
    reduceMotion: boolean;
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
  | { type: 'dock.state'; payload: { items: DockItem[] } }
  | { type: 'icons.result'; payload: { icons: Record<string, string> } }
  | { type: 'battery.state'; payload: BatteryState }
  | { type: 'weather.state'; payload: WeatherState }
  | { type: 'privacy.state'; payload: PrivacyState }
  | { type: 'wallpaper.state'; payload: { url: string | null; color: string } }
  | { type: 'desktop.status'; payload: { mode: string; iconsHidden: boolean; detail: string | null } }
  | { type: 'visualizer.frame'; payload: { bins: number[] } }
  | { type: 'store.value'; payload: { key: string; value: unknown } }
  | { type: 'diagnostics.state'; payload: Diagnostics };

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
  | { type: 'dock.layout'; payload: { hit: CssRect[]; band: CssRect | null } }
  | { type: 'settings.update'; payload: { settings: AppSettings } }
  | { type: 'settings.open'; payload: Record<string, never> }
  | { type: 'settings.action'; payload: { action: SettingsAction; arg: string | null } }
  | { type: 'store.get'; payload: { key: string } }
  | { type: 'store.set'; payload: { key: string; value: unknown } };

export type HostType = HostMessage['type'];
export type HostPayload<T extends HostType> = Extract<HostMessage, { type: T }>['payload'];
export type UiType = UiMessage['type'];
export type UiPayload<T extends UiType> = Extract<UiMessage, { type: T }>['payload'];
