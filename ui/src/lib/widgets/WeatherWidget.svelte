<!-- Počasí z Open-Meteo (data z C#). -->
<script lang="ts">
  import Icon from '$lib/components/Icon.svelte';
  import { host } from '$lib/stores/host.svelte';
  import { describeWeather, formatTemp } from '$lib/weather';

  let w = $derived(host.weather);
  let info = $derived(describeWeather(w?.weatherCode ?? null, w?.isDay ?? true));
  let updated = $derived(
    w?.updatedAt ? new Intl.DateTimeFormat(undefined, { hour: 'numeric', minute: '2-digit' }).format(new Date(w.updatedAt)) : null,
  );
</script>

<div class="weather">
  {#if w?.status === 'ok'}
    <header>
      <span class="city">{w.city}</span>
      <Icon name={info.icon} size={26} strokeWidth={1.5} />
    </header>
    <div class="temp">{formatTemp(w.temperature)}</div>
    <div class="desc">{info.label}</div>
    <div class="range">Max {formatTemp(w.high)} · Min {formatTemp(w.low)}</div>
    <footer>{updated ? `${updated} · ` : ''}Open-Meteo</footer>
  {:else if w?.status === 'disabled'}
    <p class="muted">Počasí je vypnuté v nastavení</p>
  {:else if w?.status === 'error'}
    <p class="muted">{w.error ?? 'Počasí se nepodařilo načíst.'}</p>
  {:else}
    <p class="muted">Načítám počasí…</p>
  {/if}
</div>

<style>
  .weather {
    height: 100%;
    display: flex;
    flex-direction: column;
  }
  header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
  }
  .city {
    font-weight: var(--bw-weight-medium);
    font-size: var(--bw-text-sm);
  }
  .temp {
    font-size: 36px;
    font-weight: var(--bw-weight-normal);
    line-height: 1.1;
    margin-top: var(--bw-space-1);
  }
  .desc {
    font-weight: var(--bw-weight-medium);
    font-size: var(--bw-text-sm);
  }
  .range {
    font-size: var(--bw-text-xs);
    color: var(--bw-fg-muted);
  }
  footer {
    margin-top: auto;
    font-size: 11px;
    color: var(--bw-fg-subtle);
  }
  .muted {
    margin: auto 0;
    color: var(--bw-fg-muted);
    font-size: var(--bw-text-sm);
  }
</style>
