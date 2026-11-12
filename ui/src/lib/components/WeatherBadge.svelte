<!-- Kompaktní počasí (ikona + teplota) pro horní lištu. -->
<script lang="ts">
  import Icon from './Icon.svelte';
  import type { WeatherState } from '$lib/bridge/messages';
  import { describeWeather, formatTemp } from '$lib/weather';

  interface Props {
    state: WeatherState | null;
  }

  let { state }: Props = $props();
  let info = $derived(describeWeather(state?.weatherCode ?? null, state?.isDay ?? true));
</script>

{#if state?.status === 'ok'}
  <span class="weather" title={`${state.city ?? ''}: ${info.label}, ${formatTemp(state.temperature)} (max ${formatTemp(state.high)}, min ${formatTemp(state.low)})`}>
    <Icon name={info.icon} size={15} />
    <span>{formatTemp(state.temperature)}</span>
  </span>
{:else if state?.status === 'error'}
  <span class="weather muted" title={`Počasí: ${state.error ?? 'chyba'}`}>
    <Icon name="cloud" size={15} />
    <span>–</span>
  </span>
{/if}

<style>
  .weather {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    font-variant-numeric: tabular-nums;
  }
  .muted {
    opacity: 0.5;
  }
</style>
