<!--
  Zamykací obrazovka Better Windows (před zámkem Windows): tapeta, velké hodiny a datum,
  hudba, počasí, baterie, uživatel. Klik / klávesa = "odemknout" (C# pak zamkne Windows).
  V režimu úprav vpravo panel editoru s živým náhledem.
-->
<script lang="ts">
  import { onMount } from 'svelte';
  import Icon from '$lib/components/Icon.svelte';
  import ClockWidget from '$lib/widgets/ClockWidget.svelte';
  import BatteryIndicator from '$lib/components/BatteryIndicator.svelte';
  import { host } from '$lib/stores/host.svelte';
  import { clock } from '$lib/stores/clock.svelte';
  import { ready, send } from '$lib/bridge/bridge';
  import { describeWeather, formatTemp } from '$lib/weather';
  import type { LockSettings } from '$lib/bridge/messages';
  import LockEditor from './LockEditor.svelte';

  let draft = $state<LockSettings | null>(null);
  let leaving = $state(false);
  let openedAt = 0;

  // Klávesy, které NEodemykají: hlasitost, média, samotné modifikátory (a Win+Shift+L, kterým se zámek otevřel).
  const ignoredKeys = new Set([
    'Shift', 'Control', 'Alt', 'Meta', 'OS', 'AltGraph', 'CapsLock', 'NumLock', 'Tab', 'Fn',
    'AudioVolumeUp', 'AudioVolumeDown', 'AudioVolumeMute', 'MediaPlayPause', 'MediaTrackNext',
    'MediaTrackPrevious', 'MediaStop', 'MediaPlay', 'MediaPause',
  ]);

  function onKey(e: KeyboardEvent) {
    if (host.lock.editing || e.repeat || ignoredKeys.has(e.key) || e.metaKey) return;
    unlock();
  }
  let cfg = $derived(host.lock.editing && draft ? draft : host.settings?.lock);

  const dateFormat = new Intl.DateTimeFormat(undefined, { weekday: 'long', day: 'numeric', month: 'long' });
  let date = $derived(dateFormat.format(clock.now));
  let time = $derived(
    new Intl.DateTimeFormat(undefined, { hour: 'numeric', minute: '2-digit', second: cfg?.showSeconds ? '2-digit' : undefined }).format(clock.now),
  );
  let weather = $derived(describeWeather(host.weather?.weatherCode ?? null, host.weather?.isDay ?? true));

  // Při otevření: připravit koncept pro editor / vrátit animaci odchodu.
  $effect(() => {
    if (host.lock.open) {
      leaving = false;
      openedAt = performance.now();
      draft = host.lock.editing && host.settings ? ($state.snapshot(host.settings.lock) as LockSettings) : null;
    }
  });

  function unlock() {
    // Prvních 700 ms po otevření nic (dojezd zkratky, omylem klik).
    if (!host.lock.open || host.lock.editing || leaving || performance.now() - openedAt < 700) return;
    leaving = true; // animace "odjetí nahoru", pak C# zamkne Windows
    setTimeout(() => send('lock.dismiss', {}), host.performance.reduced ? 0 : 320);
  }

  function saveEditor() {
    if (host.settings && draft) {
      const settings = $state.snapshot(host.settings);
      settings.lock = $state.snapshot(draft) as LockSettings;
      send('settings.update', { settings });
    }
    send('lock.editDone', {});
  }

  onMount(() => ready('lock'));
</script>

<svelte:window onkeydown={onKey} />

<!-- Klik kamkoli = odemknout (klávesy obsluhuje svelte:window výše). -->
<!-- svelte-ignore a11y_click_events_have_key_events -->
<!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
<main class="lock" class:leaving onclick={unlock} style:background-color={host.wallpaper?.color ?? '#101418'}>
  {#if host.wallpaper?.url}
    <img class="wallpaper" src={host.wallpaper.url} alt="" draggable="false" style:filter="blur({cfg?.wallpaperBlur ?? 0}px)" style:transform="scale({(cfg?.wallpaperBlur ?? 0) > 0 ? 1.06 : 1})" />
  {/if}
  <div class="dim" style:opacity={cfg?.dim ?? 0.15}></div>

  <section class="clock {cfg?.clockStyle ?? 'large'} {cfg?.clockPosition ?? 'top'}">
    <div class="date">{date}</div>
    {#if cfg?.clockStyle === 'analog'}
      <div class="analog"><ClockWidget /></div>
    {:else}
      <div class="time">{time}</div>
    {/if}

    <div class="cards">
      {#if cfg?.showWeather && host.weather?.status === 'ok'}
        <div class="card">
          <Icon name={weather.icon} size={22} />
          <div>
            <div class="big">{formatTemp(host.weather.temperature)}</div>
            <div class="small">{host.weather.city} · {weather.label}</div>
          </div>
        </div>
      {/if}
      {#if cfg?.showBattery && host.battery?.present}
        <div class="card"><BatteryIndicator state={host.battery} /></div>
      {/if}
    </div>

    {#if cfg?.showMedia && host.media?.available}
      <!-- Ovládání hudby nesmí "odemknout" obrazovku. -->
      <!-- svelte-ignore a11y_click_events_have_key_events -->
      <div class="media" role="group" aria-label="Hudba" onclick={(e) => e.stopPropagation()}>
        {#if host.media.thumbnail}<img src={host.media.thumbnail} alt="" />{/if}
        <div class="meta">
          <div class="title">{host.media.title}</div>
          <div class="small">{host.media.artist ?? ''}</div>
        </div>
        <button type="button" aria-label="Předchozí" onclick={() => send('media.command', { command: 'previous' })}><Icon name="previous" size={18} /></button>
        <button type="button" aria-label={host.media.playing ? 'Pozastavit' : 'Přehrát'} onclick={() => send('media.command', { command: 'playPause' })}>
          <Icon name={host.media.playing ? 'pause' : 'play'} size={22} />
        </button>
        <button type="button" aria-label="Další" onclick={() => send('media.command', { command: 'next' })}><Icon name="next" size={18} /></button>
      </div>
    {/if}
  </section>

  <footer>
    {#if cfg?.showUser && host.user}
      <div class="user">
        {#if host.user.avatar}<img src={host.user.avatar} alt="" />{:else}<span class="avatar"><Icon name="user" size={26} /></span>{/if}
        <span>{host.user.name}</span>
      </div>
    {/if}
    {#if !host.lock.editing}
      <div class="hint"><Icon name="lock" size={14} /> Klikni nebo stiskni libovolnou klávesu</div>
    {/if}
  </footer>

  {#if host.lock.editing && draft}
    <LockEditor {draft} onchange={(d) => (draft = d)} ondone={saveEditor} oncancel={() => send('lock.editDone', {})} />
  {/if}
</main>

<style>
  .lock {
    position: fixed;
    inset: 0;
    overflow: hidden;
    color: #fff;
    text-shadow: 0 2px 12px rgb(0 0 0 / 0.35);
    transition:
      transform var(--bw-duration-slow) var(--bw-ease),
      opacity var(--bw-duration-slow) var(--bw-ease);
  }
  .lock.leaving {
    transform: translateY(-8%);
    opacity: 0;
  }
  .wallpaper {
    position: absolute;
    inset: 0;
    width: 100%;
    height: 100%;
    object-fit: cover;
  }
  .dim {
    position: absolute;
    inset: 0;
    background: #000;
  }
  .clock {
    position: absolute;
    left: 0;
    right: 0;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: var(--bw-space-4);
  }
  .clock.top {
    top: 9vh;
  }
  .clock.center {
    top: 50%;
    transform: translateY(-58%);
  }
  .date {
    font-size: 22px;
    font-weight: var(--bw-weight-medium);
  }
  .date::first-letter {
    text-transform: uppercase; /* "Pátek 13. listopadu" – velké jen první písmeno */
  }
  .time {
    font-variant-numeric: tabular-nums;
    line-height: 1;
    letter-spacing: -0.02em;
  }
  .large .time {
    font-size: clamp(96px, 12vw, 180px);
    font-weight: 600;
  }
  .thin .time {
    font-size: clamp(110px, 14vw, 210px);
    font-weight: 200;
  }
  .bold .time {
    font-size: clamp(110px, 14vw, 200px);
    font-weight: 800;
    letter-spacing: -0.04em;
  }
  .analog {
    width: 260px;
    height: 260px;
    color: #fff;
    --bw-fg: #fff;
    filter: drop-shadow(0 4px 16px rgb(0 0 0 / 0.35));
  }
  .cards {
    display: flex;
    gap: var(--bw-space-3);
  }
  .card,
  .media {
    display: flex;
    align-items: center;
    gap: var(--bw-space-3);
    padding: var(--bw-space-3) var(--bw-space-4);
    border-radius: var(--bw-radius-lg);
    background: rgb(255 255 255 / 0.14);
    backdrop-filter: blur(20px) saturate(1.4);
    border: 1px solid rgb(255 255 255 / 0.18);
    text-shadow: none;
  }
  .big {
    font-size: 22px;
    font-weight: var(--bw-weight-bold);
  }
  .small {
    font-size: var(--bw-text-sm);
    opacity: 0.8;
  }
  .media {
    min-width: 360px;
  }
  .media img {
    width: 48px;
    height: 48px;
    border-radius: 10px;
    object-fit: cover;
  }
  .media .meta {
    flex: 1;
    min-width: 0;
  }
  .media .title {
    font-weight: var(--bw-weight-bold);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .media button {
    display: grid;
    place-items: center;
    width: 34px;
    height: 34px;
    border: 0;
    border-radius: 50%;
    background: none;
    color: inherit;
  }
  .media button:hover {
    background: rgb(255 255 255 / 0.15);
  }
  footer {
    position: absolute;
    left: 0;
    right: 0;
    bottom: 6vh;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: var(--bw-space-3);
  }
  .user {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: var(--bw-space-2);
    font-size: var(--bw-text-lg);
    font-weight: var(--bw-weight-medium);
  }
  .user img,
  .avatar {
    width: 64px;
    height: 64px;
    border-radius: 50%;
    object-fit: cover;
    border: 2px solid rgb(255 255 255 / 0.6);
  }
  .avatar {
    display: grid;
    place-items: center;
    background: rgb(255 255 255 / 0.18);
  }
  .hint {
    display: flex;
    align-items: center;
    gap: 6px;
    font-size: var(--bw-text-sm);
    opacity: 0.75;
  }
</style>
