<!-- Kalendář na měsíc (pondělí první), dnešek zvýrazněný, šipky mezi měsíci. -->
<script lang="ts">
  import { t } from '$lib/i18n';
  import Icon from '$lib/components/Icon.svelte';
  import { clock } from '$lib/stores/clock.svelte';

  let offset = $state(0);
  let today = $derived(clock.now);
  let shown = $derived(new Date(today.getFullYear(), today.getMonth() + offset, 1));
  // Měsíc a rok zvlášť: rok tlumeně, celé na jednom řádku (dřív se název lámal do dvou řádků).
  let month = $derived(new Intl.DateTimeFormat(undefined, { month: 'long' }).format(shown));
  let year = $derived(shown.getFullYear());

  // Zkratky dnů: 1. 1. 2024 bylo pondělí.
  const dayNames = Array.from({ length: 7 }, (_, i) =>
    new Intl.DateTimeFormat(undefined, { weekday: 'narrow' }).format(new Date(2024, 0, 1 + i)),
  );

  let cells = $derived.by(() => {
    const first = new Date(shown.getFullYear(), shown.getMonth(), 1);
    const startOffset = (first.getDay() + 6) % 7; // pondělí = 0
    return Array.from({ length: 42 }, (_, i) => {
      const d = new Date(shown.getFullYear(), shown.getMonth(), 1 - startOffset + i);
      return {
        key: d.toDateString(),
        day: d.getDate(),
        inMonth: d.getMonth() === shown.getMonth(),
        isToday: d.toDateString() === today.toDateString(),
      };
    });
  });
</script>

<div class="calendar">
  <header>
    <span class="title">{month} <span class="year">{year}</span></span>
    <span class="nav">
      <button type="button" aria-label={t('Předchozí měsíc')} onclick={() => offset--}><Icon name="chevronLeft" size={12} /></button>
      <button type="button" aria-label={t('Další měsíc')} onclick={() => offset++}><Icon name="chevronRight" size={12} /></button>
    </span>
  </header>
  <div class="grid">
    {#each dayNames as d, i (i)}
      <span class="dow">{d}</span>
    {/each}
    {#each cells as c (c.key)}
      <span class="day" class:out={!c.inMonth} class:today={c.isToday}>{c.day}</span>
    {/each}
  </div>
</div>

<style>
  /*
   * Velikost buňky se počítá z místa v kartě (container query): 7 řádků (dny v týdnu + 6 týdnů)
   * pod záhlavím, 7 sloupců. Číslo i kroužek dneška se odvozují od buňky -> vždy se vejdou,
   * ať je karta zaoblená a odsazená jakkoli.
   */
  .calendar {
    container-type: size;
    display: flex;
    flex-direction: column;
    gap: 3px;
    height: 100%;
  }
  header {
    flex: none;
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 4px;
    height: 16px;
  }
  .title {
    min-width: 0;
    overflow: hidden;
    white-space: nowrap;
    text-overflow: ellipsis;
    font-size: 11px;
    font-weight: var(--bw-weight-medium);
  }
  .title::first-letter {
    text-transform: uppercase;
  }
  .year {
    color: var(--bw-fg-muted);
    font-weight: var(--bw-weight-normal);
  }
  .nav {
    flex: none;
    display: flex;
  }
  .nav button {
    display: grid;
    place-items: center;
    width: 16px;
    height: 16px;
    padding: 0;
    border: 0;
    border-radius: 50%;
    background: none;
    color: inherit;
  }
  .nav button:hover {
    background: var(--bw-hover);
  }
  .grid {
    --cell: min(calc(100cqw / 7), calc((100cqh - 19px) / 7));
    flex: 1;
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    grid-auto-rows: var(--cell);
    align-content: space-between;
    font-size: calc(var(--cell) * 0.6);
    font-variant-numeric: tabular-nums;
    line-height: 1;
  }
  .dow,
  .day {
    display: grid;
    place-items: center;
    justify-self: center;
    width: var(--cell);
    height: var(--cell);
  }
  .dow {
    color: var(--bw-fg-subtle);
    font-size: 0.85em;
  }
  .day {
    border-radius: 50%;
  }
  .out {
    color: var(--bw-fg-subtle);
  }
  .today {
    background: var(--bw-accent);
    color: var(--bw-accent-fg);
    font-weight: var(--bw-weight-medium);
  }
</style>
