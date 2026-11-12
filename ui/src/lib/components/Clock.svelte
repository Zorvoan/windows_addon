<!--
  Datum a čas ve formátu podle jazyka Windows. Použije horní lišta i widgety.
-->
<script lang="ts">
  import { clock } from '$lib/stores/clock.svelte';

  interface Props {
    showDate?: boolean;
    showSeconds?: boolean;
  }

  let { showDate = true, showSeconds = false }: Props = $props();

  const dateFormat = new Intl.DateTimeFormat(undefined, { weekday: 'short', day: 'numeric', month: 'numeric' });
  let timeFormat = $derived(
    new Intl.DateTimeFormat(undefined, { hour: 'numeric', minute: '2-digit', second: showSeconds ? '2-digit' : undefined }),
  );

  let date = $derived(dateFormat.format(clock.now));
  let time = $derived(timeFormat.format(clock.now));
</script>

<time datetime={clock.now.toISOString()}>
  {#if showDate}<span class="date">{date}</span>{/if}
  <span class="time">{time}</span>
</time>

<style>
  time {
    display: inline-flex;
    gap: var(--bw-space-2);
    font-variant-numeric: tabular-nums;
    white-space: nowrap;
  }
  .time {
    font-weight: var(--bw-weight-medium);
  }
</style>
