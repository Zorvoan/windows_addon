<!-- Kalendář na měsíc (pondělí první), dnešek zvýrazněný, šipky mezi měsíci. -->
<script lang="ts">
  import Icon from '$lib/components/Icon.svelte';
  import { clock } from '$lib/stores/clock.svelte';

  let offset = $state(0);
  let today = $derived(clock.now);
  let shown = $derived(new Date(today.getFullYear(), today.getMonth() + offset, 1));
  let title = $derived(new Intl.DateTimeFormat(undefined, { month: 'long', year: 'numeric' }).format(shown));

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
    <span class="title">{title}</span>
    <span class="nav">
      <button type="button" aria-label="Předchozí měsíc" onclick={() => offset--}><Icon name="chevronLeft" size={12} /></button>
      <button type="button" aria-label="Další měsíc" onclick={() => offset++}><Icon name="chevronRight" size={12} /></button>
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
  .calendar {
    display: grid;
    gap: 4px;
    font-size: 9px;
  }
  header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }
  .title {
    font-size: var(--bw-text-xs);
    font-weight: var(--bw-weight-bold);
    text-transform: uppercase;
    letter-spacing: 0.02em;
  }
  .nav {
    display: flex;
  }
  .nav button {
    display: grid;
    place-items: center;
    width: 18px;
    height: 18px;
    border: 0;
    border-radius: 50%;
    background: none;
    color: inherit;
  }
  .nav button:hover {
    background: var(--bw-hover);
  }
  .grid {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    row-gap: 1px;
    text-align: center;
  }
  .dow {
    color: var(--bw-fg-muted);
    font-weight: var(--bw-weight-medium);
    padding-bottom: 2px;
  }
  .day {
    display: grid;
    place-items: center;
    height: 15px;
    width: 15px;
    justify-self: center;
    border-radius: 50%;
    font-variant-numeric: tabular-nums;
  }
  .out {
    color: var(--bw-fg-subtle);
  }
  .today {
    background: #f97316;
    color: white;
    font-weight: var(--bw-weight-bold);
  }
</style>
