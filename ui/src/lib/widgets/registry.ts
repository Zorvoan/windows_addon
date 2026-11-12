import type { WidgetType } from '$lib/bridge/messages';

/** Velikost a název každého typu widgetu (CSS px). Mřížka: 156 px + mezera 16 px. */
export const UNIT = 156;
export const GAP = 16;

export const widgetInfo: Record<WidgetType, { title: string; w: number; h: number }> = {
  clock: { title: 'Hodiny', w: 1, h: 1 },
  calendar: { title: 'Kalendář', w: 1, h: 1 },
  weather: { title: 'Počasí', w: 1, h: 1 },
  reminders: { title: 'Připomínky', w: 1, h: 1 },
  battery: { title: 'Baterie', w: 2, h: 1 },
};

export function sizeOf(type: WidgetType): { width: number; height: number } {
  const info = widgetInfo[type];
  return { width: info.w * UNIT + (info.w - 1) * GAP, height: info.h * UNIT + (info.h - 1) * GAP };
}
