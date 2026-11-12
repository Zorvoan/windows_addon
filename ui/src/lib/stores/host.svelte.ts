/**
 * Reaktivní stav ze C#, sdílený všemi komponentami stránky.
 * Stačí import { host } a číst host.battery, host.weather atd.
 */
import { on } from '$lib/bridge/bridge';
import type {
  AppSettings,
  BatteryState,
  Diagnostics,
  DockItem,
  MediaState,
  PrivacyState,
  StartApp,
  VolumeState,
  WeatherState,
} from '$lib/bridge/messages';

export const host = $state({
  settings: null as AppSettings | null,
  foreground: null as { appId: string | null; appName: string; title: string } | null,
  dock: [] as DockItem[],
  icons: {} as Record<string, string>,
  battery: null as BatteryState | null,
  weather: null as WeatherState | null,
  privacy: { micInUse: false, camInUse: false, micApps: [], camApps: [] } as PrivacyState,
  wallpaper: null as { url: string | null; color: string } | null,
  desktop: null as { mode: string; iconsHidden: boolean; detail: string | null } | null,
  diagnostics: null as Diagnostics | null,
  /** Data widgetů z C# úložiště (klíč -> hodnota). */
  store: {} as Record<string, unknown>,
  performance: { reduced: false, reason: '' },
  media: null as MediaState | null,
  volume: null as VolumeState | null,
  startApps: [] as StartApp[],
  startOpen: false,
});

on('settings.state', (p) => (host.settings = p.settings));
on('foreground.changed', (p) => (host.foreground = p));
on('dock.state', (p) => (host.dock = p.items));
on('icons.result', (p) => Object.assign(host.icons, p.icons));
on('battery.state', (p) => (host.battery = p));
on('weather.state', (p) => (host.weather = p));
on('privacy.state', (p) => (host.privacy = p));
on('wallpaper.state', (p) => (host.wallpaper = p));
on('desktop.status', (p) => (host.desktop = p));
on('diagnostics.state', (p) => (host.diagnostics = p));
on('store.value', (p) => (host.store[p.key] = p.value));
on('performance.state', (p) => (host.performance = p));
on('media.state', (p) => (host.media = p));
on('volume.state', (p) => (host.volume = p));
on('start.apps', (p) => (host.startApps = p.apps));
on('start.visibility', (p) => (host.startOpen = p.open));
