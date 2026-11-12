<!--
  Horní lišta. Rozmazané pozadí kreslí systém (DWM) pod celým oknem,
  tady se jen přidává jemný tón a obsah. Data chodí z C# (host store).
-->
<script lang="ts">
  import { onMount } from 'svelte';
  import Logo from '$lib/components/Logo.svelte';
  import Clock from '$lib/components/Clock.svelte';
  import IconButton from '$lib/components/IconButton.svelte';
  import BatteryIndicator from '$lib/components/BatteryIndicator.svelte';
  import WeatherBadge from '$lib/components/WeatherBadge.svelte';
  import PrivacyDots from '$lib/components/PrivacyDots.svelte';
  import { host } from '$lib/stores/host.svelte';
  import { ready, send } from '$lib/bridge/bridge';

  let appName = $derived(host.foreground?.appName || 'Plocha');
  let title = $derived(host.foreground?.title ?? '');

  onMount(() => ready('topbar'));
</script>

<header class="bar">
  <div class="left">
    <span class="logo"><Logo size={18} /></span>
    <span class="app-name" title={title}>{appName}</span>
  </div>

  <!-- Volné místo pro Dynamic Island. Ten bude samostatné okno nad lištou. -->
  <div class="island-slot" aria-hidden="true"></div>

  <div class="right">
    <PrivacyDots state={host.privacy} />
    <span class="item"><WeatherBadge state={host.weather} /></span>
    <span class="item"><BatteryIndicator state={host.battery} /></span>
    <IconButton icon="sliders" label="Nastavení Better Windows" onclick={() => send('settings.open', {})} />
    <span class="clock"><Clock /></span>
  </div>
</header>

<style>
  .bar {
    height: 100vh;
    display: grid;
    grid-template-columns: 1fr var(--bw-island-width) 1fr;
    align-items: center;
    padding: 0 var(--bw-space-2) 0 var(--bw-space-3);
    background: var(--bw-bar-tint);
    color: var(--bw-bar-fg);
    font-size: var(--bw-text-md);
  }
  .left,
  .right {
    display: flex;
    align-items: center;
    gap: var(--bw-space-1);
    min-width: 0;
  }
  .right {
    justify-content: flex-end;
  }
  .item:not(:empty) {
    padding: 0 var(--bw-space-2);
  }
  .logo {
    display: inline-grid;
    margin-right: var(--bw-space-2);
  }
  .app-name {
    font-weight: var(--bw-weight-bold);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .clock {
    padding: 0 var(--bw-space-2);
  }
</style>
