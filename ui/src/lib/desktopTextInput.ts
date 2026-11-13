/**
 * Psaní do textových polí widgetů na ploše.
 *
 * Vrstva plochy je vložená do okna plochy Windows: myš k ní projde, klávesnice ne (fokus má
 * plocha Windows). Klik do pole proto pošle C# jeho polohu a obsah, C# přes pole ukáže skutečné
 * vstupní okno a po Enteru / kliknutí jinam vrátí text sem (desktop.textResult).
 * Na ostatních stránkách (zámek, nastavení…) se nic nemění – tam klávesnice funguje normálně.
 *
 * Použití: <input use:desktopTextInput={{ onsubmit: add }} />
 */
import { on, send } from '$lib/bridge/bridge';

let counter = 0;

export function desktopTextInput(node: HTMLInputElement, opts: { onsubmit?: () => void } = {}) {
  if (!location.pathname.endsWith('desktop.html')) return {};
  const key = `text-${++counter}`;
  let options = opts;

  const open = (e: PointerEvent) => {
    if (e.button !== 0) return;
    e.preventDefault(); // fokus by stejně nedostal – nenechat blikat kurzor v nepoužitelném poli
    const r = node.getBoundingClientRect();
    send('desktop.textInput', {
      key,
      rect: { x: r.left, y: r.top, width: r.width, height: r.height },
      value: node.value,
      placeholder: node.placeholder || null,
      fontSize: parseFloat(getComputedStyle(node).fontSize) || 12,
    });
  };

  const off = on('desktop.textResult', (p) => {
    if (p.key !== key) return;
    node.value = p.value;
    node.dispatchEvent(new Event('input', { bubbles: true })); // bind:value se aktualizuje
    if (p.submit) {
      if (options.onsubmit) options.onsubmit();
      else node.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
    }
  });

  node.addEventListener('pointerdown', open);
  return {
    update(next: { onsubmit?: () => void } = {}) {
      options = next;
    },
    destroy() {
      node.removeEventListener('pointerdown', open);
      off();
    },
  };
}
