/**
 * Sdílené "teď" pro všechny komponenty se stejným časem (hodiny v liště, widgety...).
 * Tiká přesně na začátku každé sekundy; běží jen jeden časovač na stránku.
 */
let now = $state(new Date());
let started = false;

function tick(): void {
  now = new Date();
  setTimeout(tick, 1000 - (Date.now() % 1000));
}

export const clock = {
  get now(): Date {
    // Časovač se spustí při prvním čtení. Stav se mění až v callbacku časovače,
    // nikdy během vykreslování (to Svelte zakazuje).
    if (!started) {
      started = true;
      setTimeout(tick, 1000 - (Date.now() % 1000));
    }
    return now;
  },
};
