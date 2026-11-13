<!--
  Dynamic Island: černá pilulka nahoře uprostřed.
   - skrytá: nic nehraje a nic se neděje (okno je pak celé "průchozí"),
   - kompaktní: obal alba, název, animované proužky,
   - rozbalená (najetí myší): obal, název, interpret, průběh, ovládání, hlasitost,
   - hlasitost: na chvíli po změně hlasitosti.
  Tvar se hlásí do C# (island.layout -> SetWindowRgn), stejně jako u docku.

  Pohyb: rozměry pilulky řídí pružina (lib/motion), obsah se mezi stavy prolíná.
  Oblast okna se během animace NEhoní po snímcích (zpoždění zprávy by ořízlo rostoucí okraj):
  při roztahování se hned pošle cílová velikost + rezerva na překmit, při zmenšování oblast
  sleduje pilulku (příliš velká oblast by chvíli polykala kliknutí na lištu), po dosednutí přesný tvar.
-->
<script lang="ts">
  import { onMount, tick } from 'svelte';
  import { fade } from 'svelte/transition';
  import { SpringBox, springs, motionReduced } from '$lib/motion.svelte';
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

  // ---- Rozměry pilulky (pružina) ----
  const TOP = 4;
  type Size = { w: number; h: number; r: number };

  /** Šířka kompaktní pilulky je token (horní lišta podle něj nechává místo uprostřed). */
  function compactWidth(): number {
    return parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--bw-island-width')) || 260;
  }

  function sizeFor(m: typeof mode): Size {
    if (m === 'expanded') return { w: 400, h: 156, r: 28 };
    if (m === 'hidden') return { w: 120, h: 28, r: 14 };
    return { w: compactWidth(), h: 28, r: 14 };
  }

  const box = new SpringBox<'w' | 'h' | 'r'>(sizeFor('hidden'), springs.island);
  let envelope: { w: number; h: number } | null = null;

  $effect(() => {
    const target = sizeFor(mode);
    // Obálka po dobu animace: cílová velikost + rezerva na překmit, omezená oknem.
    // (Větší současná pilulka se do oblasti dostane v report() – při zmenšování ji oblast sleduje.)
    envelope = {
      w: Math.min(target.w * 1.04 + 8, window.innerWidth - 4),
      h: Math.min(target.h * 1.04 + 8, window.innerHeight - TOP - 2),
    };
    report();
    box.set(target, { instant: motionReduced() }).then((settled) => {
      if (!settled) return; // přerušeno další změnou – obálku pošle ta
      envelope = null;
      report();
    });
  });

  // ---- Tvar okna do C# ----
  let lastSent = '';

  async function report() {
    await tick();
    const hit: CssRect[] = [];
    if (envelope) {
      const r = pill?.getBoundingClientRect();
      const w = Math.max(envelope.w, r ? r.width + 4 : 0);
      const h = Math.max(envelope.h, r ? r.height + 4 : 0);
      hit.push({ x: Math.floor((window.innerWidth - w) / 2), y: TOP - 2, width: Math.ceil(w), height: Math.ceil(h) });
    } else if (pill && mode !== 'hidden') {
      const r = pill.getBoundingClientRect();
      hit.push({ x: Math.floor(r.left) - 2, y: Math.floor(r.top) - 2, width: Math.ceil(r.width) + 4, height: Math.ceil(r.height) + 4 });
    }
    const json = JSON.stringify(hit);
    if (json !== lastSent) {
      lastSent = json;
      send('island.layout', { hit, band: null });
    }
  }

  // Prolnutí obsahu: nový naběhne s malým zpožděním, starý rychle zmizí (při omezení pohybu hned).
  const fadeIn = () => ({ duration: motionReduced() ? 0 : 160, delay: motionReduced() ? 0 : 70 });
  const fadeOut = () => ({ duration: motionReduced() ? 0 : 90 });

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
  style:width="{Math.max(0, box.value.w)}px"
  style:height="{Math.max(0, box.value.h)}px"
  style:border-radius="{Math.max(0, box.value.r)}px"
  role="region"
  aria-label="Dynamic Island"
  onmouseenter={enter}
  onmouseleave={leave}
>
  {#if mode === 'compact' && media}
    <div class="layer row-compact" in:fade={fadeIn()} out:fade={fadeOut()}>
      {#if media.thumbnail}<img class="art small" src={media.thumbnail} alt="" />{:else}<span class="art small ph"><Icon name="volume" size={12} /></span>{/if}
      <span class="title">{media.title}</span>
      <span class="dots" class:playing={media.playing} aria-hidden="true">{#each [0, 1, 2, 3] as c (c)}<span class="col">{#each [0, 1, 2] as r (r)}<i></i>{/each}</span>{/each}</span>
    </div>
  {:else if mode === 'expanded' && media}
    <div class="layer body-expanded" in:fade={fadeIn()} out:fade={fadeOut()}>
      <div class="top">
        {#if media.thumbnail}<img class="art big" src={media.thumbnail} alt="" />{:else}<span class="art big ph"><Icon name="volume" size={24} /></span>{/if}
        <div class="meta">
          <div class="title">{media.title}</div>
          <div class="artist">{media.artist ?? ''}</div>
          {#if media.source}<div class="source">{media.source}</div>{/if}
        </div>
        <span class="dots big" class:playing={media.playing} aria-hidden="true">{#each [0, 1, 2, 3] as c (c)}<span class="col">{#each [0, 1, 2] as r (r)}<i></i>{/each}</span>{/each}</span>
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
    <div class="layer row-volume" in:fade={fadeIn()} out:fade={fadeOut()}>
      <Icon name={host.volume?.muted ? 'mute' : 'volume'} size={16} />
      <div class="track"><div class="fill" style:width="{host.volume?.muted ? 0 : (host.volume?.level ?? 0) * 100}%"></div></div>
      <span class="pct">{host.volume?.muted ? 'Ztlumeno' : `${Math.round((host.volume?.level ?? 0) * 100)} %`}</span>
    </div>
  {/if}
</div>

<style>
  .pill {
    position: absolute;
    top: 4px;
    left: 50%;
    transform: translateX(-50%);
    background: var(--bw-island-bg);
    color: var(--bw-island-fg);
    overflow: hidden;
    box-shadow:
      0 0 0 1px light-dark(transparent, rgb(237 237 237 / 0.12)),
      0 4px 16px rgb(0 0 0 / 0.3);
    /* Rozměry a zaoblení řídí pružina (style:...), CSS přechod jen pro průhlednost. */
    transition: opacity var(--bw-duration) var(--bw-ease);
  }
  .hidden {
    opacity: 0;
    pointer-events: none;
  }
  /* Obsah má pevnou velikost cílového stavu a je ukotvený nahoře uprostřed; pilulka ho při
     změně tvaru jen ořezává (obsah se během animace nepřeskládává). */
  .layer {
    position: absolute;
    top: 0;
    left: 50%;
    transform: translateX(-50%);
  }
  .row-compact,
  .row-volume {
    display: flex;
    align-items: center;
    gap: var(--bw-space-2);
    height: 100%;
    padding: 0 var(--bw-space-3) 0 var(--bw-space-1);
    font-size: var(--bw-text-sm);
    font-weight: var(--bw-weight-medium);
  }
  .row-compact,
  .row-volume {
    width: var(--bw-island-width);
    height: 28px;
    box-sizing: border-box;
  }
  .row-volume {
    padding: 0 14px;
  }
  .body-expanded {
    width: 400px;
    height: 156px;
    box-sizing: border-box;
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
    border-radius: 10px; /* vnořené zaoblení: 14 − 4 */
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
    gap: var(--bw-space-2);
    padding: var(--bw-space-4) var(--bw-space-5) var(--bw-space-3);
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
    font-weight: var(--bw-weight-medium);
    font-size: var(--bw-text-md);
  }
  .artist,
  .source {
    font-size: var(--bw-text-sm);
    color: #a1a1a6;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .source {
    font-size: var(--bw-text-xs);
    color: #85858b;
  }
  .progress {
    display: flex;
    align-items: center;
    gap: var(--bw-space-2);
    font-size: 11px;
    font-variant-numeric: tabular-nums;
    color: #a1a1a6;
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
    background: #ededed;
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
    gap: var(--bw-space-3);
  }
  .controls button {
    display: grid;
    place-items: center;
    width: 32px;
    height: 32px;
    padding: 0;
    border: 0;
    border-radius: 50%;
    background: none;
    color: inherit;
    transition: background-color var(--bw-duration-fast) var(--bw-ease);
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
    gap: var(--bw-space-1);
    margin-left: var(--bw-space-3);
  }
  .vol input {
    width: 88px;
    accent-color: #ededed;
  }
  /* Podpis shellu: tečková matice. Při přehrávání sloupce teček "dýchají" (živý stav), jinak stojí. */
  .dots {
    display: inline-flex;
    align-items: flex-end;
    gap: 2px;
    flex: none;
  }
  .col {
    display: flex;
    flex-direction: column-reverse;
    gap: 2px;
  }
  .dots i {
    width: 3px;
    height: 3px;
    border-radius: 50%;
    background: #ededed;
    opacity: 0.25;
  }
  .dots i:first-child {
    opacity: 0.9;
  }
  .dots.big i {
    width: 4px;
    height: 4px;
  }
  .dots.playing i {
    animation: level 1.2s var(--bw-ease) infinite alternate;
  }
  .dots.playing .col:nth-child(1) i:nth-child(2) { animation-delay: -0.2s; }
  .dots.playing .col:nth-child(2) i:nth-child(2) { animation-delay: -0.7s; }
  .dots.playing .col:nth-child(2) i:nth-child(3) { animation-delay: -0.4s; }
  .dots.playing .col:nth-child(3) i:nth-child(2) { animation-delay: -1s; }
  .dots.playing .col:nth-child(3) i:nth-child(3) { animation-delay: -0.1s; }
  .dots.playing .col:nth-child(4) i:nth-child(2) { animation-delay: -0.5s; }
  .dots.playing .col:nth-child(4) i:nth-child(3) { animation-delay: -0.9s; }
  .dots.playing i:first-child {
    animation: none;
  }
  @keyframes level {
    from {
      opacity: 0.2;
    }
    to {
      opacity: 0.9;
    }
  }
</style>
