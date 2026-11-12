/** Ruční přepnutí motivu pro celou stránku. 'system' = podle Windows. */
export type ThemeMode = 'system' | 'light' | 'dark';

export function setTheme(mode: ThemeMode): void {
  const root = document.documentElement;
  if (mode === 'system') delete root.dataset.theme;
  else root.dataset.theme = mode;
}
