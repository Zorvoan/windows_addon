<!--
  Dynamic Island: černá pilulka nahoře uprostřed.
   - skrytá: nic nehraje a nic se neděje (okno je pak celé "průchozí"),
   - kompaktní: obal alba, název, animované proužky,
   - rozbalená (najetí myší): obal, název, interpret, průběh, ovládání, hlasitost,
   - hlasitost: na chvíli po změně hlasitosti.
  Tvar se hlásí do C# (island.layout -> SetWindowRgn), stejně jako u docku.

  Pohyb: časová osa a pružiny podle referenční nahrávky (./motion.ts), obsah se rozmazává
  a zaostřuje podle postupu; rozbalení rozmaže i obsah lišty (zpráva island.expanded).
  Oblast okna se během animace NEhoní po snímcích (zpoždění zprávy by ořízlo rostoucí okraj):
  při roztahování se hned pošle cílová velikost + rezerva na překmit, při zmenšování oblast
  sleduje pilulku (příliš velká oblast by chvíli polykala kliknutí na lištu), po dosednutí přesný tvar.
-->
<script lang="ts">
  import { onMount, tick } from 'svelte';
  import { motionReduced } from '$lib/motion.svelte';
  import { IslandAnimator, EXPANDED, type IslandLook, type IslandMode } from './motion';
  import Icon from '$lib/components/Icon.svelte';
  import Waveform from '$lib/components/Waveform.svelte';
  import Marquee from '$lib/components/Marquee.svelte';
  import { host } from '$lib/stores/host.svelte';
  import { on, ready, send } from '$lib/bridge/bridge';
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

  // ---- Pohyb pilulky ----
  const TOP = 4;

  /** Šířka kompaktní pilulky je token (horní lišta podle něj nechává místo uprostřed). */
  function compactWidth(): number {
    return parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--bw-island-width')) || 260;
  }

  function sizeFor(m: IslandMode): { w: number; h: number } {
    if (m === 'expanded') return { w: EXPANDED.w, h: EXPANDED.h };
    if (m === 'hidden') return { w: 120, h: 28 };
    return { w: compactWidth(), h: 28 };
  }

  let envelope: { w: number; h: number } | null = null;
  const animator = new IslandAnimator(
    TOP,
    sizeFor,
    (l) => (look = l),
    () => {
      envelope = null;
      report();
    },
  );
  let look: IslandLook = $state(animator.look);

  // Okno hrající aplikace vletělo do ostrůvku (C# ho při minimalizaci animuje sem) -> pohltit.
  on('island.absorb', () => {
    const c = sizeFor(mode);
    envelope = { w: Math.min(c.w * 1.2 + 8, window.innerWidth), h: Math.min(c.h * 1.45 + 8 + TOP, window.innerHeight) };
    report();
    animator.absorb(motionReduced()); // po doznění: přesný tvar (onSettle)
  });

  let lastExpanded = false;
  let lastMode: IslandMode | null = null;
  $effect(() => {
    const m = mode;
    const reduced = motionReduced();
    // Efekt se spustí i při změně režimu výkonu (motionReduced) – tvar a animace jen při změně stavu,
    // jinak by obálka zůstala nastavená navždy (pružina se nerozjede, takže nepřijde „dosednutí“).
    if (m === lastMode) return;
    lastMode = m;
    const target = sizeFor(m);
    // Lišta rozmaže svůj obsah, dokud je ostrůvek rozbalený.
    const expanded = mode === 'expanded';
    if (expanded !== lastExpanded) {
      lastExpanded = expanded;
      send('island.expanded', { expanded });
    }
    // Obálka po dobu animace: cílová velikost + rezerva na překmit, omezená oknem.
    // (Větší současná pilulka se do oblasti dostane v report() – při zmenšování ji oblast sleduje.)
    const shadow = mode === 'expanded' ? SHADOW : { side: 0, bottom: 0 };
    envelope = {
      w: Math.min(target.w * 1.04 + 8 + shadow.side * 2, window.innerWidth),
      h: Math.min(target.h * 1.04 + 8 + TOP + shadow.bottom, window.innerHeight),
    };
    report();
    animator.go(m, reduced); // po dosednutí: envelope = null + přesný tvar (onSettle)
  });

  /*
   * Stín musí zůstat UVNITŘ tvaru okna (SetWindowRgn), jinak se uřízne do ostré hrany.
   * Kompaktní pilulka: jen 1px okraj v tmavém režimu (vejde se do 2px rezervy).
   * Rozbalená: měkký stín podle rozbalení (0..1); tvar okna se o SHADOW rozšíří (report()).
   */
  const SHADOW = { side: 26, bottom: 34 }; // dosah stínu 0 8px 24px (+ rezerva)
  function pillShadow(s: number): string {
    const k = Math.round(s * 100) / 100;
    const ring = '0 0 0 1px light-dark(transparent, rgb(237 237 237 / 0.12))';
    return k > 0.01 ? `${ring}, 0 ${k * 8}px ${k * 24}px rgb(0 0 0 / ${k * 0.3})` : ring;
  }

  // ---- Tvar okna do C# ----
  let lastSent = '';

  async function report() {
    await tick();
    const hit: CssRect[] = [];
    if (envelope) {
      const r = pill?.getBoundingClientRect();
      // Při sbalování ještě dobíhá stín -> i s ním.
      const side = look.shadow > 0.01 ? SHADOW.side : 2;
      const bottom = look.shadow > 0.01 ? SHADOW.bottom : 2;
      const w = Math.min(window.innerWidth, Math.max(envelope.w, r ? r.width + side * 2 : 0));
      const h = Math.min(window.innerHeight, Math.max(envelope.h, r ? r.bottom + bottom : 0));
      // Od horního okraje: pilulka se při nafouknutí a „želé“ na konci sbalení posune i kousek nahoru.
      hit.push({ x: Math.floor((window.innerWidth - w) / 2), y: 0, width: Math.ceil(w), height: Math.ceil(h) });
    } else if (pill && mode !== 'hidden') {
      const r = pill.getBoundingClientRect();
      // Rozbalená pilulka má stín -> tvar okna i s ním (jinak by se uřízl).
      const side = look.shadow > 0.01 ? SHADOW.side : 2;
      const bottom = look.shadow > 0.01 ? SHADOW.bottom : 2;
      const x = Math.max(0, Math.floor(r.left) - side);
      const right = Math.min(window.innerWidth, Math.ceil(r.right) + side);
      const y = Math.max(0, Math.floor(r.top) - 2);
      hit.push({ x, y, width: right - x, height: Math.min(window.innerHeight, Math.ceil(r.bottom) + bottom) - y });
    }
    const json = JSON.stringify(hit);
    if (json !== lastSent) {
      lastSent = json;
      send('island.layout', { hit, band: null });
    }
  }

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
  style:top="{look.top}px"
  style:width="{Math.max(0, look.w)}px"
  style:height="{Math.max(0, look.h)}px"
  style:border-radius="{Math.max(0, look.r)}px"
  style:box-shadow={pillShadow(look.shadow)}
  role="region"
  aria-label="Dynamic Island"
  onmouseenter={enter}
  onmouseleave={leave}
>
  <!-- Všechny vrstvy jsou připravené v DOM a jen se prolínají (vytváření obsahu při najetí
       myší by stálo čas právě v prvním snímku animace). -->
  {#if media}
    <div
      class="layer row-compact"
      class:shown={mode === 'compact'}
      aria-hidden={mode !== 'compact'}
      style:opacity={look.compact.opacity}
      style:filter={look.compact.blur > 0.05 ? `blur(${look.compact.blur}px)` : null}
      style:visibility={look.compact.opacity > 0.01 ? 'visible' : 'hidden'}
    >
      {#if media.thumbnail}<img class="art small" src={media.thumbnail} alt="" />{:else}<span class="art small ph"><Icon name="volume" size={12} /></span>{/if}
      <span class="title"><Marquee text={media.title ?? ''} /></span>
      <Waveform playing={media.playing} art={media.thumbnail} active={look.compact.opacity > 0.01} width={22} height={16} />
    </div>
    <div
      class="layer body-expanded"
      class:shown={mode === 'expanded'}
      aria-hidden={mode !== 'expanded'}
      style:opacity={look.expanded.opacity}
      style:filter={look.expanded.blur > 0.05 ? `blur(${look.expanded.blur}px)` : null}
      style:visibility={look.expanded.opacity > 0.01 ? 'visible' : 'hidden'}
      style:transform="translateX(-50%) scale({look.expanded.scale})"
    >
      <div class="top">
        {#if media.thumbnail}<img class="art big" src={media.thumbnail} alt="" />{:else}<span class="art big ph"><Icon name="volume" size={24} /></span>{/if}
        <div class="meta">
          <div class="title"><Marquee text={media.title ?? ''} /></div>
          <div class="artist"><Marquee text={media.artist ?? ''} /></div>
          {#if media.source}<div class="source">{media.source}</div>{/if}
        </div>
        <Waveform playing={media.playing} art={media.thumbnail} active={look.expanded.opacity > 0.01} width={28} height={22} />
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
  {/if}
  {#if host.volume}
    <div
      class="layer row-volume"
      class:shown={mode === 'volume'}
      aria-hidden={mode !== 'volume'}
      style:opacity={look.volume.opacity}
      style:visibility={look.volume.opacity > 0.01 ? 'visible' : 'hidden'}
    >
      <Icon name={host.volume?.muted ? 'mute' : 'volume'} size={16} />
      <div class="track"><div class="fill" style:width="{host.volume?.muted ? 0 : (host.volume?.level ?? 0) * 100}%"></div></div>
      <span class="pct">{host.volume?.muted ? 'Ztlumeno' : `${Math.round((host.volume?.level ?? 0) * 100)} %`}</span>
    </div>
  {/if}
</div>

<style>
  .pill {
    position: absolute;
    left: 50%;
    transform: translateX(-50%);
    background: var(--bw-island-bg);
    color: var(--bw-island-fg);
    overflow: hidden;
    /* Stín (roste s rozbalením) nastavuje pillShadow() přímo – ne přes CSS proměnnou, ta se dědí
       a její změna v každém snímku by přepočítala styl celého obsahu. */
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
    transform-origin: top center;
    /* Průhlednost, rozmazání a zmenšení řídí motion.ts (inline styl); skrytá vrstva má
       visibility: hidden, takže její tlačítka nejdou kliknout ani vybrat Tabem. */
    pointer-events: none;
  }
  .layer.shown {
    pointer-events: auto;
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
    /* Sloupec nesmí růst s délkou názvu (jinak dlouhý název vytlačí proužky ven a nikdy „nepřeteče“). */
    grid-template-columns: minmax(0, 1fr);
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
</style>
