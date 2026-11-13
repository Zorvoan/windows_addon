import { t } from '$lib/i18n';
import type { IconName } from './icons';

/** Popis a ikona podle kódu počasí WMO (používá Open-Meteo). */
export function describeWeather(code: number | null, isDay: boolean): { label: string; icon: IconName } {
  if (code === null) return { label: t('Neznámé'), icon: 'cloud' };
  if (code === 0) return { label: t('Jasno'), icon: isDay ? 'sun' : 'moon' };
  if (code <= 2) return { label: code === 1 ? t('Skoro jasno') : t('Polojasno'), icon: isDay ? 'cloudSun' : 'cloudMoon' };
  if (code === 3) return { label: t('Zataženo'), icon: 'cloud' };
  if (code === 45 || code === 48) return { label: t('Mlha'), icon: 'fog' };
  if (code >= 51 && code <= 57) return { label: t('Mrholení'), icon: 'rain' };
  if ((code >= 61 && code <= 67) || (code >= 80 && code <= 82)) return { label: t('Déšť'), icon: 'rain' };
  if ((code >= 71 && code <= 77) || code === 85 || code === 86) return { label: t('Sněžení'), icon: 'snow' };
  if (code >= 95) return { label: t('Bouřka'), icon: 'storm' };
  return { label: t('Oblačno'), icon: 'cloud' };
}

export function formatTemp(t: number | null | undefined): string {
  return t === null || t === undefined ? '–' : `${Math.round(t)}°`;
}
