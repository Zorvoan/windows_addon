<!-- Ikona baterie s úrovní nabití a bleskem při nabíjení. Zobrazí se jen, když baterie existuje. -->
<script lang="ts">
  import { t } from '$lib/i18n';
  import type { BatteryState } from '$lib/bridge/messages';

  interface Props {
    state: BatteryState | null;
    showPercent?: boolean;
  }

  let { state, showPercent = true }: Props = $props();
  let percent = $derived(Math.max(0, Math.min(100, state?.percent ?? 0)));
  let low = $derived(percent <= 15 && !state?.charging);
</script>

{#if state?.present}
  <span class="battery" class:low title={t('Baterie {p} %', { p: percent }) + (state.charging ? ' – ' + t('nabíjí se') : state.onAc ? ' – ' + t('napájení ze sítě') : '')}>
    {#if showPercent}<span class="pct">{percent} %</span>{/if}
    <svg width="24" height="13" viewBox="0 0 24 13" aria-hidden="true">
      <rect x="0.75" y="0.75" width="19.5" height="11.5" rx="3" fill="none" stroke="currentColor" stroke-opacity="0.55" stroke-width="1.2" />
      <rect x="2.4" y="2.4" width={(16.2 * percent) / 100} height="8.2" rx="1.6" fill="currentColor" class="level" />
      <rect x="21.4" y="4.2" width="1.8" height="4.6" rx="0.9" fill="currentColor" fill-opacity="0.55" />
      {#if state.charging}
        <path d="M11.6 2.2 7.6 7h2.6l-.9 3.8 4-4.8h-2.6z" fill="var(--bw-bolt, #fff)" stroke="rgb(0 0 0 / 0.35)" stroke-width="0.4" />
      {/if}
    </svg>
  </span>
{/if}

<style>
  .battery {
    display: inline-flex;
    align-items: center;
    gap: var(--bw-space-1);
    font-variant-numeric: tabular-nums;
  }
  .pct {
    font-size: var(--bw-text-sm);
    white-space: nowrap;
  }
  .low .level {
    fill: var(--bw-negative);
  }
</style>
