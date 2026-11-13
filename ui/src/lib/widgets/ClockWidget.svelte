<!-- Analogové hodiny. -->
<script lang="ts">
  import { clock } from '$lib/stores/clock.svelte';

  let now = $derived(clock.now);
  let sec = $derived(now.getSeconds());
  let min = $derived(now.getMinutes() + sec / 60);
  let hour = $derived((now.getHours() % 12) + min / 60);
  const numbers = Array.from({ length: 12 }, (_, i) => i + 1);
</script>

<svg viewBox="0 0 100 100" class="clock" role="img" aria-label={now.toLocaleTimeString()}>
  {#each Array.from({ length: 60 }, (_, i) => i) as i (i)}
    <line
      x1="50"
      y1={i % 5 === 0 ? 6 : 7}
      x2="50"
      y2="9.5"
      transform="rotate({i * 6} 50 50)"
      stroke="currentColor"
      stroke-opacity={i % 5 === 0 ? 0.8 : 0.3}
      stroke-width={i % 5 === 0 ? 1 : 0.5}
    />
  {/each}
  {#each numbers as n (n)}
    <text
      x={50 + 33 * Math.sin((n * Math.PI) / 6)}
      y={50 - 33 * Math.cos((n * Math.PI) / 6)}
      text-anchor="middle"
      dominant-baseline="central">{n}</text
    >
  {/each}
  <line x1="50" y1="52" x2="50" y2="28" transform="rotate({hour * 30} 50 50)" class="hand hour" />
  <line x1="50" y1="54" x2="50" y2="16" transform="rotate({min * 6} 50 50)" class="hand minute" />
  <line x1="50" y1="56" x2="50" y2="12" transform="rotate({sec * 6} 50 50)" class="hand second" />
  <circle cx="50" cy="50" r="2" class="pivot" />
</svg>

<style>
  .clock {
    width: 100%;
    height: 100%;
    color: var(--bw-fg);
  }
  text {
    font-size: 8.5px;
    font-weight: var(--bw-weight-normal);
    fill: currentColor;
  }
  .hand {
    stroke: currentColor;
    stroke-linecap: round;
  }
  .hour {
    stroke-width: 3;
  }
  .minute {
    stroke-width: 2;
  }
  .second {
    stroke: var(--bw-accent);
    stroke-width: 0.9;
  }
  .pivot {
    fill: var(--bw-accent);
  }
</style>
