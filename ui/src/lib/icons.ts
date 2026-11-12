/**
 * Vlastní sada ikon (24×24, obrysové, tloušťka čáry se nastavuje v Icon.svelte).
 * Každá ikona = seznam SVG cest. Novou ikonu přidáš dopsáním klíče.
 */
export const icons = {
  search: ['M10.5 4a6.5 6.5 0 1 1 0 13a6.5 6.5 0 0 1 0-13z', 'M15.5 15.5L20 20'],
  widgets: [
    'M5.5 4h4a1.5 1.5 0 0 1 1.5 1.5v4a1.5 1.5 0 0 1-1.5 1.5h-4A1.5 1.5 0 0 1 4 9.5v-4A1.5 1.5 0 0 1 5.5 4z',
    'M14.5 4h4a1.5 1.5 0 0 1 1.5 1.5v4a1.5 1.5 0 0 1-1.5 1.5h-4A1.5 1.5 0 0 1 13 9.5v-4A1.5 1.5 0 0 1 14.5 4z',
    'M5.5 13h4a1.5 1.5 0 0 1 1.5 1.5v4a1.5 1.5 0 0 1-1.5 1.5h-4A1.5 1.5 0 0 1 4 18.5v-4A1.5 1.5 0 0 1 5.5 13z',
    'M14.5 13h4a1.5 1.5 0 0 1 1.5 1.5v4a1.5 1.5 0 0 1-1.5 1.5h-4a1.5 1.5 0 0 1-1.5-1.5v-4a1.5 1.5 0 0 1 1.5-1.5z',
  ],
  sliders: ['M4 8h9', 'M17 8h3', 'M15 5.5v5', 'M4 16h3', 'M11 16h9', 'M9 13.5v5'],
  chevronUp: ['M7 14l5-5l5 5'],
  battery: ['M5 8h12a1.5 1.5 0 0 1 1.5 1.5v5A1.5 1.5 0 0 1 17 16H5a1.5 1.5 0 0 1-1.5-1.5v-5A1.5 1.5 0 0 1 5 8z', 'M21 11v2'],
  cloud: ['M7.5 18a4 4 0 0 1-.5-7.97A5.5 5.5 0 0 1 17.6 9.1A4.5 4.5 0 0 1 17 18z'],
  sun: ['M12 8a4 4 0 1 1 0 8a4 4 0 0 1 0-8z', 'M12 2.5v2', 'M12 19.5v2', 'M2.5 12h2', 'M19.5 12h2', 'M5.3 5.3l1.4 1.4', 'M17.3 17.3l1.4 1.4', 'M5.3 18.7l1.4-1.4', 'M17.3 6.7l1.4-1.4'],
  moon: ['M19 14.5A7.5 7.5 0 0 1 9.5 5a7.5 7.5 0 1 0 9.5 9.5z'],
} as const satisfies Record<string, readonly string[]>;

export type IconName = keyof typeof icons;
