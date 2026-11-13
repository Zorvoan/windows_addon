import type { ThemeStyle } from './bridge/messages';

/** Předpřipravené motivy. Výchozí přebírá barvu zvýraznění z Windows (jen designové tokeny – systémové rozmazání a nativní nabídka zůstávají). */
export const themePresets: ThemeStyle[] = [
  { name: 'Výchozí', useSystemAccent: true, accent: '#3a6df0', radiusCard: 22, glassBlur: 28, glassOpacity: 0.35, barTint: 0.12, dockTint: 0.16 },
  { name: 'Sklo', useSystemAccent: false, accent: '#7c9cff', radiusCard: 28, glassBlur: 48, glassOpacity: 0.18, barTint: 0.05, dockTint: 0.08 },
  { name: 'Grafit', useSystemAccent: false, accent: '#8e8e93', radiusCard: 16, glassBlur: 36, glassOpacity: 0.55, barTint: 0.32, dockTint: 0.36 },
  { name: 'Oceán', useSystemAccent: false, accent: '#00b2ca', radiusCard: 24, glassBlur: 30, glassOpacity: 0.28, barTint: 0.14, dockTint: 0.2 },
  { name: 'Západ slunce', useSystemAccent: false, accent: '#f97316', radiusCard: 22, glassBlur: 26, glassOpacity: 0.32, barTint: 0.12, dockTint: 0.16 },
  { name: 'Máta', useSystemAccent: false, accent: '#10b981', radiusCard: 12, glassBlur: 22, glassOpacity: 0.4, barTint: 0.18, dockTint: 0.22 },
];
