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

  import Icon from '$lib/components/Icon.svelte';

  let appName = $derived(host.foreground?.appName || 'Plocha');
  let title = $derived(host.foreground?.title ?? '');
  let bar = $derived(host.settings?.topBar);
  let media = $derived(host.media);
  let confirmExit = $state(false);
  let exitTimer = 0;

  /** "Ukončit" (jako "Exit Familiar"): první klik se zeptá, druhý ukončí. */
  function exitClick() {
    if (confirmExit) {
      send('app.exit', {});
      return;
    }
    confirmExit = true;
    clearTimeout(exitTimer);
    exitTimer = window.setTimeout(() => (confirmExit = false), 3000);
  }

  onMount(() => ready('topbar'));
</script>

<header class="bar">
  <div class="left">
    <button type="button" class="logo" title="Start" aria-label="Start" onclick={() => send('start.toggle', {})}><Logo size={18} /></button>
    <span class="app-name" title={title}>{appName}</span>
    {#if bar?.showNowPlaying !== false && media?.available}
      <span class="now-playing" title={`${media.title}${media.artist ? ' – ' + media.artist : ''}`}>
        {#if media.thumbnail}<img src={media.thumbnail} alt="" />{/if}
        <span class="np-title">{media.title}</span>
        {#if media.artist}<span class="np-artist">{media.artist}</span>{/if}
        <button type="button" aria-label={media.playing ? 'Pozastavit' : 'Přehrát'} onclick={() => send('media.command', { command: 'playPause' })}>
          <Icon name={media.playing ? 'pause' : 'play'} size={12} />
        </button>
        <button type="button" aria-label="Další" disabled={!media.canNext} onclick={() => send('media.command', { command: 'next' })}><Icon name="next" size={12} /></button>
      </span>
    {/if}
  </div>

  <!-- Volné místo pro Dynamic Island. Ten bude samostatné okno nad lištou. -->
  <div class="island-slot" aria-hidden="true"></div>

  <div class="right">
    {#if bar?.showExit !== false}
      <button type="button" class="exit" class:confirm={confirmExit} onclick={exitClick}>
        {confirmExit ? 'Opravdu ukončit?' : 'Ukončit'} <Icon name="exit" size={11} />
      </button>
    {/if}
    <IconButton icon="search" label="Hledat (Start)" onclick={() => send('start.toggle', {})} />
    <PrivacyDots state={host.privacy} />
    <IconButton icon="sliders" label="Nastavení Better Windows" onclick={() => send('settings.open', {})} />
    {#if host.weather?.status === 'ok'}<span class="pill"><WeatherBadge state={host.weather} /></span>{/if}
    <span class="item"><BatteryIndicator state={host.battery} /></span>
    {#if bar?.showLanguage !== false && host.inputLanguage?.code}
      <span class="lang" title={host.inputLanguage.name}>{host.inputLanguage.code}</span>
    {/if}
    {#if bar?.showUser !== false && host.user}
      <span class="user">
        {#if host.user.avatar}<img src={host.user.avatar} alt="" />{:else}<Icon name="user" size={14} />{/if}
        <span>{host.user.name}</span>
      </span>
    {/if}
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
  .right > :global(*) {
    flex: none; /* položky vpravo se nesmršťují ani nezalamují */
  }
  .item:not(:empty) {
    padding: 0 var(--bw-space-2);
  }
  .logo {
    display: inline-grid;
    place-items: center;
    width: 28px;
    height: 26px;
    margin-right: var(--bw-space-1);
    padding: 0;
    border: 0;
    border-radius: var(--bw-radius-sm);
    background: none;
  }
  .logo:hover {
    background: var(--bw-hover);
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
  .now-playing {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    min-width: 0;
    max-width: 320px;
    margin-left: var(--bw-space-3);
    padding: 2px 4px 2px 3px;
    border-radius: var(--bw-radius-pill);
    background: var(--bw-hover);
    font-size: var(--bw-text-sm);
  }
  .now-playing img {
    width: 18px;
    height: 18px;
    border-radius: 50%;
    object-fit: cover;
  }
  .np-title {
    font-weight: var(--bw-weight-medium);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .np-artist {
    color: var(--bw-fg-muted);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    max-width: 90px;
  }
  .now-playing button {
    display: grid;
    place-items: center;
    flex: none;
    width: 20px;
    height: 20px;
    padding: 0;
    border: 0;
    border-radius: 50%;
    background: none;
  }
  .now-playing button:hover {
    background: var(--bw-pressed);
  }
  .now-playing button:disabled {
    opacity: 0.4;
  }
  .exit {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    height: 22px;
    padding: 0 10px;
    margin-right: var(--bw-space-1);
    border-radius: var(--bw-radius-pill);
    border: 1px solid var(--bw-border);
    background: var(--bw-hover);
    font-size: var(--bw-text-xs);
    font-weight: var(--bw-weight-medium);
  }
  .exit.confirm {
    background: #ef4444;
    border-color: transparent;
    color: white;
  }
  .pill {
    display: inline-flex;
    align-items: center;
    height: 22px;
    padding: 0 8px;
    margin: 0 var(--bw-space-1);
    border-radius: var(--bw-radius-pill);
    background: var(--bw-hover);
  }
  .lang {
    padding: 0 var(--bw-space-2);
    font-size: var(--bw-text-xs);
    font-weight: var(--bw-weight-bold);
    letter-spacing: 0.03em;
  }
  .user {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 0 var(--bw-space-2);
    font-size: var(--bw-text-sm);
    white-space: nowrap;
  }
  /* Užší obrazovky: méně důležité položky postupně schovat. */
  @media (max-width: 1400px) {
    .np-artist,
    .user span {
      display: none;
    }
  }
  @media (max-width: 1200px) {
    .now-playing,
    .lang,
    .exit {
      display: none;
    }
  }
  .user img {
    width: 20px;
    height: 20px;
    border-radius: 50%;
    object-fit: cover;
  }
</style>
