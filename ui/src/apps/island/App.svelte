<!--
  Dynamic Island: černá pilulka nahoře uprostřed.
   - skrytá: nic nehraje a nic se neděje (okno je pak celé "průchozí"),
   - kompaktní: obal alba, název, animované proužky,
   - rozbalená (najetí myší): obal, název, interpret, průběh, ovládání, hlasitost,
   - hlasitost: na chvíli po změně hlasitosti.
  Tvar se hlásí do C# (island.layout -> SetWindowRgn), stejně jako u docku.
-->
<script lang="ts">
  import { onMount, tick } from 'svelte';
  import Icon from '$lib/components/Icon.svelte';
  import { host } from '$lib/stores/host.svelte';
  import { ready, send } from '$lib/bridge/bridge';
  import type { CssRect } from '$lib/bridge/messages';

  let pill: HTMLDivElement | undefined = $state();
  let hovering = $state(false);
  let hudUntil = $state(0);
  let now = $state(Date.now());
  let leaveTimer = 0;

  let media = $derived(host.media);
  let hasMedia = $derived(!!media?.available && host.settings?.island.enabled !== false);
  let showHud = $derived(now < hudUntil && host.settings?.island.showVolume !== false);
  let mode = $derived<'hidden' | 'compact' | 'expanded' | 'volume'>(
    hovering && hasMedia ? 'expanded' : showHud ? 'volume' : hasMedia ? 'compact' : 'hidden',
  );

  // Ukazatel hlasitosti při změně (klávesy hlasitosti, posuvník).
  $effect(() => {
    if (host.volume?.changed) hudUntil = Date.now() + 1600;
  });

  // Pozice skladby: dopočítává se lokálně (C# ji posílá jen při změně).
  let basePos = 0;
  let baseAt = 0;
  $effect(() => {
    basePos = media?.position ?? 0;
    baseAt = Date.now();
  });
  let position = $derived.by(() => {
    if (!media?.duration) return null;
    const p = media.playing ? basePos + (now - baseAt) / 1000 : basePos;
    return Math.min(media.duration, Math.max(0, p));
  });

  setInterval(() => (now = Date.now()), 500);

  function fmt(t: number | null | undefined): string {
    if (t === null || t === undefined || !Number.isFinite(t)) return '–:––';
    const m = Math.floor(t / 60);
    const s = Math.floor(t % 60);
    return `${m}:${s.toString().padStart(2, '0')}`;
  }

  function enter() {
    clearTimeout(leaveTimer);
    hovering = true;
  }

  function leave() {
    clearTimeout(leaveTimer);
    leaveTimer = window.setTimeout(() => (hovering = false), 300);
  }

  // ---- Tvar okna do C# (během animace každý snímek) ----
  let lastSent = '';
  let animUntil = 0;

  async function report() {
    await tick();
    const hit: CssRect[] = [];
    if (pill && mode !== 'hidden') {
      const r = pill.getBoundingClientRect();
      hit.push({ x: Math.floor(r.left) - 2, y: Math.floor(r.top) - 2, width: Math.ceil(r.width) + 4, height: Math.ceil(r.height) + 4 });
    }
    const json = JSON.stringify(hit);
    if (json !== lastSent) {
      lastSent = json;
      send('island.layout', { hit, band: null });
    }
  }

  function animateReport() {
    animUntil = performance.now() + 700;
    const step = () => {
      report();
      if (performance.now() < animUntil) requestAnimationFrame(step);
    };
    requestAnimationFrame(step);
  }

  $effect(() => {
    void mode;
    animateReport();
  });

  onMount(() => {
    ready('island');
    const ro = new ResizeObserver(() => report());
    if (pill) ro.observe(pill);
    return () => ro.disconnect();
  });
</script>

<div
  class="pill {mode}"
  bind:this={pill}
  role="region"
  aria-label="Dynamic Island"
  onmouseenter={enter}
  onmouseleave={leave}
>
  {#if mode === 'compact' && media}
    <div class="row-compact">
      {#if media.thumbnail}<img class="art small" src={media.thumbnail} alt="" />{:else}<span class="art small ph"><Icon name="volume" size={12} /></span>{/if}
      <span class="title">{media.title}</span>
      <span class="bars" class:playing={media.playing} aria-hidden="true"><i></i><i></i><i></i><i></i></span>
    </div>
  {:else if mode === 'expanded' && media}
    <div class="body-expanded">
      <div class="top">
        {#if media.thumbnail}<img class="art big" src={media.thumbnail} alt="" />{:else}<span class="art big ph"><Icon name="volume" size={24} /></span>{/if}
        <div class="meta">
          <div class="title">{media.title}</div>
          <div class="artist">{media.artist ?? ''}</div>
          {#if media.source}<div class="source">{media.source}</div>{/if}
        </div>
        <span class="bars big" class:playing={media.playing} aria-hidden="true"><i></i><i></i><i></i><i></i></span>
      </div>
      {#if media.duration}
        <div class="progress">
          <span>{fmt(position)}</span>
          <div class="track"><div class="fill" style:width="{((position ?? 0) / media.duration) * 100}%"></div></div>
          <span>{fmt(media.duration)}</span>
        </div>
      {/if}
      <div class="controls">
        <button type="button" aria-label="Předchozí" disabled={!media.canPrevious} onclick={() => send('media.command', { command: 'previous' })}><Icon name="previous" size={18} /></button>
        <button type="button" class="play" aria-label={media.playing ? 'Pozastavit' : 'Přehrát'} onclick={() => send('media.command', { command: 'playPause' })}>
          <Icon name={media.playing ? 'pause' : 'play'} size={22} />
        </button>
        <button type="button" aria-label="Další" disabled={!media.canNext} onclick={() => send('media.command', { command: 'next' })}><Icon name="next" size={18} /></button>
        <span class="vol">
          <button type="button" aria-label={host.volume?.muted ? 'Zapnout zvuk' : 'Ztlumit'} onclick={() => send('volume.set', { level: null, muted: !host.volume?.muted })}>
            <Icon name={host.volume?.muted ? 'mute' : 'volume'} size={16} />
          </button>
          <input
            type="range"
            min="0"
            max="1"
            step="0.02"
            aria-label="Hlasitost"
            value={host.volume?.level ?? 0}
            oninput={(e) => send('volume.set', { level: Number(e.currentTarget.value), muted: null })}
          />
        </span>
      </div>
    </div>
  {:else if mode === 'volume'}
    <div class="row-volume">
      <Icon name={host.volume?.muted ? 'mute' : 'volume'} size={16} />
      <div class="track"><div class="fill" style:width="{host.volume?.muted ? 0 : (host.volume?.level ?? 0) * 100}%"></div></div>
      <span class="pct">{host.volume?.muted ? 'Ztlumeno' : `${Math.round((host.volume?.level ?? 0) * 100)} %`}</span>
    </div>
  {/if}
</div>

<style>
  .pill {
    position: absolute;
    top: 3px;
    left: 50%;
    transform: translateX(-50%);
    background: var(--bw-island-bg);
    color: var(--bw-island-fg);
    overflow: hidden;
    box-shadow: 0 6px 24px rgb(0 0 0 / 0.35);
    transition:
      width var(--bw-duration-slow) var(--bw-ease-spring),
      height var(--bw-duration-slow) var(--bw-ease-spring),
      border-radius var(--bw-duration-slow) var(--bw-ease),
      opacity var(--bw-duration) var(--bw-ease);
  }
  .hidden {
    width: 120px;
    height: 30px;
    border-radius: 15px;
    opacity: 0;
    pointer-events: none;
  }
  .row-compact,
  .row-volume {
    display: flex;
    align-items: center;
    gap: 8px;
    height: 100%;
    padding: 0 10px 0 5px;
    font-size: var(--bw-text-sm);
    font-weight: var(--bw-weight-medium);
  }
  .pill.compact {
    width: var(--bw-island-width);
    height: 30px;
    border-radius: 15px;
  }
  .pill.volume {
    width: var(--bw-island-width);
    height: 30px;
    border-radius: 15px;
  }
  .row-volume {
    padding: 0 14px;
  }
  .pill.expanded {
    width: 400px;
    height: 156px;
    border-radius: 28px;
  }
  .title {
    flex: 1;
    min-width: 0;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .art {
    flex: none;
    object-fit: cover;
  }
  .art.small {
    width: 20px;
    height: 20px;
    border-radius: 6px;
  }
  .art.big {
    width: 56px;
    height: 56px;
    border-radius: 12px;
  }
  .ph {
    display: grid;
    place-items: center;
    background: rgb(255 255 255 / 0.12);
  }
  .body-expanded {
    display: grid;
    gap: 8px;
    padding: 14px 18px 10px;
  }
  .top {
    display: flex;
    gap: 12px;
    align-items: center;
  }
  .meta {
    flex: 1;
    min-width: 0;
  }
  .meta .title {
    font-weight: var(--bw-weight-bold);
    font-size: var(--bw-text-md);
  }
  .artist,
  .source {
    font-size: var(--bw-text-sm);
    color: rgb(245 245 247 / 0.65);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .source {
    font-size: var(--bw-text-xs);
    color: rgb(245 245 247 / 0.45);
  }
  .progress {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 10px;
    font-variant-numeric: tabular-nums;
    color: rgb(245 245 247 / 0.6);
  }
  .track {
    flex: 1;
    height: 4px;
    border-radius: 2px;
    background: rgb(255 255 255 / 0.2);
    overflow: hidden;
  }
  .fill {
    height: 100%;
    background: #fff;
    border-radius: 2px;
  }
  .pct {
    min-width: 62px;
    text-align: right;
    font-variant-numeric: tabular-nums;
    font-size: var(--bw-text-xs);
  }
  .controls {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 14px;
  }
  .controls button {
    display: grid;
    place-items: center;
    width: 30px;
    height: 30px;
    padding: 0;
    border: 0;
    border-radius: 50%;
    background: none;
    color: inherit;
  }
  .controls button:hover {
    background: rgb(255 255 255 / 0.12);
  }
  .controls button:disabled {
    opacity: 0.35;
  }
  .controls .play {
    width: 36px;
    height: 36px;
  }
  .vol {
    display: flex;
    align-items: center;
    gap: 4px;
    margin-left: 10px;
  }
  .vol input {
    width: 90px;
    accent-color: #fff;
  }
  .bars {
    display: inline-flex;
    align-items: center;
    gap: 2px;
    height: 14px;
  }
  .bars i {
    width: 3px;
    height: 4px;
    border-radius: 2px;
    background: #fff;
    opacity: 0.85;
  }
  .bars.big {
    height: 22px;
  }
  .bars.playing i {
    animation: eq 0.9s ease-in-out infinite alternate;
  }
  .bars.playing i:nth-child(2) {
    animation-delay: -0.3s;
  }
  .bars.playing i:nth-child(3) {
    animation-delay: -0.6s;
  }
  .bars.playing i:nth-child(4) {
    animation-delay: -0.15s;
  }
  @keyframes eq {
    from {
      height: 4px;
    }
    to {
      height: 100%;
    }
  }
</style>
