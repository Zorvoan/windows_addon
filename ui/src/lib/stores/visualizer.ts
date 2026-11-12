/**
 * Snímky vizualizéru (30× za sekundu). Záměrně NE reaktivní stav Svelte – canvas si je čte
 * sám v requestAnimationFrame, aby se kvůli nim nepřekreslovalo celé UI.
 */
import { on } from '$lib/bridge/bridge';

let latest: number[] = [];
let lastAt = 0;

on('visualizer.frame', (p) => {
  latest = p.bins;
  lastAt = performance.now();
  listeners.forEach((l) => l());
});

const listeners = new Set<() => void>();

export function getBins(): number[] {
  return latest;
}

export function lastFrameAt(): number {
  return lastAt;
}

/** Zavolá se při každém novém snímku (např. pro probuzení animace). */
export function onFrame(listener: () => void): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}
