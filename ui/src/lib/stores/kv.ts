/** Úložiště dat widgetů v C# (%AppData%\BetterWindows\widgets-data.json). */
import { send } from '$lib/bridge/bridge';
import { host } from './host.svelte';

const loaded = new Set<string>();

/** Hodnota z úložiště (reaktivně přes host.store). První čtení si ji vyžádá z C#. */
export function load(key: string): void {
  if (loaded.has(key)) return;
  loaded.add(key);
  send('store.get', { key });
}

export function save(key: string, value: unknown): void {
  host.store[key] = value;
  send('store.set', { key, value });
}
