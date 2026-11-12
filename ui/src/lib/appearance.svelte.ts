/**
 * Použije na stránku motiv (ThemeStyle z nastavení) a ztlumení animací (režim výkonu).
 * Motiv jen přepisuje designové tokeny (CSS proměnné) – C# hodnoty už zkontroloval,
 * tady se kontrolují znovu (barva jen #rrggbb, čísla v rozsahu), protože končí v CSS.
 */
import { host } from '$lib/stores/host.svelte';
import type { ThemeStyle } from '$lib/bridge/messages';

const clamp = (v: number, min: number, max: number, fallback: number) =>
  Number.isFinite(v) ? Math.min(max, Math.max(min, v)) : fallback;

export function applyStyle(root: HTMLElement, style: ThemeStyle | undefined): void {
  if (!style) return;
  const accent = /^#[0-9a-fA-F]{6}$/.test(style.accent) ? style.accent : null;
  const glass = clamp(style.glassOpacity, 0, 1, 0.35);
  const bar = clamp(style.barTint, 0, 0.8, 0.12);
  const dock = clamp(style.dockTint, 0, 0.8, 0.16);
  const set = (name: string, value: string | null) =>
    value === null ? root.style.removeProperty(name) : root.style.setProperty(name, value);

  set('--bw-accent', accent);
  set('--bw-radius-card', `${clamp(style.radiusCard, 0, 40, 22)}px`);
  set('--bw-glass-blur', `${clamp(style.glassBlur, 0, 60, 28)}px`);
  set('--bw-glass-bg', `light-dark(rgb(255 255 255 / ${glass}), rgb(18 18 22 / ${glass}))`);
  set('--bw-bar-tint', `light-dark(rgb(255 255 255 / ${bar}), rgb(0 0 0 / ${bar}))`);
  set('--bw-dock-tint', `light-dark(rgb(255 255 255 / ${dock}), rgb(0 0 0 / ${dock}))`);
}

/** Zavolat jednou v main.ts každé stránky. */
export function initAppearance(): void {
  $effect.root(() => {
    $effect(() => {
      applyStyle(document.documentElement, host.settings?.appearance.style);
    });
    $effect(() => {
      if (host.performance.reduced) document.documentElement.dataset.motion = 'reduced';
      else delete document.documentElement.dataset.motion;
    });
  });
}
