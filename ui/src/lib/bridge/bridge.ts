/**
 * Most C# <-> JS přes WebView2:
 *  - C# -> JS: CoreWebView2.PostWebMessageAsJson -> event "message" na window.chrome.webview
 *  - JS -> C#: window.chrome.webview.postMessage(obj)
 * Mimo WebView2 (prohlížeč při vývoji) se zprávy jen vypisují do konzole.
 */
import type { HostPayload, HostType, UiPayload, UiType } from './messages';

interface WebView {
  postMessage(message: unknown): void;
  addEventListener(type: 'message', listener: (event: MessageEvent) => void): void;
}

const webview: WebView | undefined = (window as unknown as { chrome?: { webview?: WebView } }).chrome?.webview;

type AnyHandler = (payload: unknown) => void;
const handlers = new Map<string, Set<AnyHandler>>();

webview?.addEventListener('message', (event) => dispatch(event.data));

function dispatch(message: unknown): void {
  if (!message || typeof message !== 'object') return;
  const { type, payload } = message as { type?: unknown; payload?: unknown };
  if (typeof type !== 'string') return;
  handlers.get(type)?.forEach((handler) => {
    try {
      handler(payload);
    } catch (err) {
      log('error', `Obsluha zprávy ${type} selhala: ${String(err)}`);
    }
  });
}

/** Poslouchá zprávu z C#. Vrací funkci pro odhlášení. */
export function on<T extends HostType>(type: T, handler: (payload: HostPayload<T>) => void): () => void {
  let set = handlers.get(type);
  if (!set) handlers.set(type, (set = new Set()));
  set.add(handler as AnyHandler);
  return () => set.delete(handler as AnyHandler);
}

/** Pošle zprávu do C#. */
export function send<T extends UiType>(type: T, payload: UiPayload<T>): void {
  if (webview) webview.postMessage({ type, payload });
  else console.debug('[bridge]', type, payload);
}

/** Stránka je připravená – C# pošle aktuální stav (nastavení, baterie, počasí...). */
export function ready(page: string): void {
  send('ui.ready', { page });
}

export function log(level: 'info' | 'warn' | 'error', message: string): void {
  if (webview) send('ui.log', { level, message });
  else console[level === 'info' ? 'log' : level](message);
}

// Chyby v JS ať jsou vidět v logu aplikace.
window.addEventListener('error', (e) => log('error', `${e.message} (${e.filename}:${e.lineno})`));
window.addEventListener('unhandledrejection', (e) => log('error', `Nezachycený Promise: ${String(e.reason)}`));
