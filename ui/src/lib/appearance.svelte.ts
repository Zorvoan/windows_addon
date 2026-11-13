/**
 * Použije na stránku motiv (ThemeStyle z nastavení) a ztlumení animací (režim výkonu).
 * Motiv jen přepisuje designové tokeny (CSS proměnné) – C# hodnoty už zkontroloval,
 * tady se kontrolují znovu (barva jen #rrggbb, čísla v rozsahu), protože končí v CSS.
 */
import { host } from '$lib/stores/host.svelte';
import type { ThemeStyle } from '$lib/bridge/messages';

const clamp = (v: number, min: number, max: number, fallback: number) =>
  Number.isFinite(v) ? Math.min(max, Math.max(min, v)) : fallback;

/** Relativní jas barvy (WCAG) – pro volbu bílého / tmavého textu na barvě zvýraznění. */
function luminance(hex: string): number {
  const c = [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255).map((v) => (v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4));
  return 0.2126 * (c[0] ?? 0) + 0.7152 * (c[1] ?? 0) + 0.0722 * (c[2] ?? 0);
}

export function applyStyle(root: HTMLElement, style: ThemeStyle | undefined, systemAccent?: string): void {
  if (!style) return;
  const wanted = style.useSystemAccent && systemAccent ? systemAccent : style.accent;
  const accent = /^#[0-9a-fA-F]{6}$/.test(wanted) ? wanted : null;
  const glass = clamp(style.glassOpacity, 0, 1, 0.35);
  const bar = clamp(style.barTint, 0, 0.8, 0.12);
  const dock = clamp(style.dockTint, 0, 0.8, 0.16);
  const set = (name: string, value: string | null) =>
    value === null ? root.style.removeProperty(name) : root.style.setProperty(name, value);

  set('--bw-accent', accent);
  // Text na barvě zvýraznění: bílý, dokud má aspoň kontrast 3:1 (velký / tučný text – jako na Macu);
  // tmavý jen u světlých barev (žlutá, světle zelená…). Dřív vyhrál tmavý o setiny i u modré.
  set('--bw-accent-fg', accent ? (1.05 / (luminance(accent) + 0.05) >= 3 ? '#ffffff' : '#0d1324') : null);
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
      applyStyle(document.documentElement, host.settings?.appearance.style, host.system.accent);
    });
    // Windows: vypnuté efekty průhlednosti / kontrastní motiv -> neprůhledné plochy, silnější okraje.
    $effect(() => {
      const root = document.documentElement;
      if (host.system.reduceTransparency || host.system.highContrast) root.dataset.transparency = 'reduced';
      else delete root.dataset.transparency;
      if (host.system.highContrast) root.dataset.contrast = 'more';
      else delete root.dataset.contrast;
    });
    $effect(() => {
      if (host.performance.reduced) document.documentElement.dataset.motion = 'reduced';
      else delete document.documentElement.dataset.motion;
    });
  });
}
