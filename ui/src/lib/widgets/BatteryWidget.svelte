<!-- Baterie tohoto počítače (kruhový ukazatel). -->
<script lang="ts">
  import Icon from '$lib/components/Icon.svelte';
  import { host } from '$lib/stores/host.svelte';

  let b = $derived(host.battery);
  let pct = $derived(Math.max(0, Math.min(100, b?.percent ?? 0)));
  const R = 30;
  const C = 2 * Math.PI * R;
</script>

<div class="battery">
  <span class="title">Baterie</span>
  {#if b?.present}
    <div class="row">
      <svg viewBox="0 0 72 72" class="ring" aria-hidden="true">
        <circle cx="36" cy="36" r={R} class="track" />
        <circle cx="36" cy="36" r={R} class="level" class:low={pct <= 15 && !b.charging} stroke-dasharray={C} stroke-dashoffset={C * (1 - pct / 100)} />
        <foreignObject x="22" y="22" width="28" height="28">
          <div class="icon"><Icon name={b.charging ? 'bolt' : 'battery'} size={22} /></div>
        </foreignObject>
      </svg>
      <div>
        <div class="label">Tento počítač</div>
        <div class="pct">{pct} %</div>
        <div class="state">{b.charging ? 'Nabíjí se' : b.onAc ? 'Napájení ze sítě' : 'Na baterii'}</div>
      </div>
    </div>
  {:else}
    <p class="muted">Tento počítač nemá baterii</p>
  {/if}
</div>

<style>
  .battery {
    height: 100%;
    display: flex;
    flex-direction: column;
    gap: var(--bw-space-2);
  }
  .title {
    font-weight: var(--bw-weight-medium);
    font-size: var(--bw-text-sm);
  }
  .row {
    display: flex;
    align-items: center;
    gap: var(--bw-space-4);
    margin: auto 0;
  }
  .ring {
    width: 84px;
    height: 84px;
    transform: rotate(-90deg);
  }
  .ring foreignObject {
    transform: rotate(90deg);
    transform-origin: 36px 36px;
  }
  .icon {
    display: grid;
    place-items: center;
    width: 28px;
    height: 28px;
  }
  circle {
    fill: none;
    stroke-width: 5;
  }
  .track {
    stroke: var(--bw-hover);
  }
  .level {
    stroke: var(--bw-positive);
    stroke-linecap: round;
    transition: stroke-dashoffset var(--bw-duration-slow) var(--bw-ease);
  }
  .level.low {
    stroke: var(--bw-negative);
  }
  .label {
    font-size: var(--bw-text-sm);
    color: var(--bw-fg-muted);
  }
  .pct {
    font-size: var(--bw-text-2xl);
    font-weight: var(--bw-weight-medium);
  }
  .state {
    font-size: var(--bw-text-xs);
    color: var(--bw-fg-muted);
  }
  .muted {
    margin: auto 0;
    color: var(--bw-fg-muted);
    font-size: var(--bw-text-sm);
  }
</style>
