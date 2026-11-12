import { send } from '$lib/bridge/bridge';
import { host } from './host.svelte';

const requested = new Set<string>();

/** Vyžádá si ikony aplikací, které ještě nemáme (C# je pošle zprávou icons.result). */
export function requestIcons(appIds: string[]): void {
  const missing = appIds.filter((id) => !(id in host.icons) && !requested.has(id));
  if (missing.length === 0) return;
  missing.forEach((id) => requested.add(id));
  send('icons.request', { appIds: missing });
}
