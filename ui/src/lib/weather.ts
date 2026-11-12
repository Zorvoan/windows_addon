import type { IconName } from './icons';

/** Popis a ikona podle kódu počasí WMO (používá Open-Meteo). */
export function describeWeather(code: number | null, isDay: boolean): { label: string; icon: IconName } {
  if (code === null) return { label: 'Neznámé', icon: 'cloud' };
  if (code === 0) return { label: 'Jasno', icon: isDay ? 'sun' : 'moon' };
  if (code <= 2) return { label: code === 1 ? 'Skoro jasno' : 'Polojasno', icon: isDay ? 'cloudSun' : 'cloudMoon' };
  if (code === 3) return { label: 'Zataženo', icon: 'cloud' };
  if (code === 45 || code === 48) return { label: 'Mlha', icon: 'fog' };
  if (code >= 51 && code <= 57) return { label: 'Mrholení', icon: 'rain' };
  if ((code >= 61 && code <= 67) || (code >= 80 && code <= 82)) return { label: 'Déšť', icon: 'rain' };
  if ((code >= 71 && code <= 77) || code === 85 || code === 86) return { label: 'Sněžení', icon: 'snow' };
  if (code >= 95) return { label: 'Bouřka', icon: 'storm' };
  return { label: 'Oblačno', icon: 'cloud' };
}

export function formatTemp(t: number | null | undefined): string {
  return t === null || t === undefined ? '–' : `${Math.round(t)}°`;
}
