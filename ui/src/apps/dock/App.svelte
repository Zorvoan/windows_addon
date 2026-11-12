<!--
  Dock. Okno je široké přes monitor a vyšší než dock; viditelná a klikatelná je jen oblast,
  kterou po každé změně pošleme do C# (dock.layout -> SetWindowRgn). Pod pásem (band) kreslí
  systémové rozmazání samostatné okno, jehož polohu C# řídí podle "band".
-->
<script lang="ts">
  import { onMount, tick } from 'svelte';
  import { host } from '$lib/stores/host.svelte';
  import { ready, send } from '$lib/bridge/bridge';
  import { requestIcons } from '$lib/stores/icons';
  import { magnify } from '$lib/magnify';
  import type { CssRect, DockItem } from '$lib/bridge/messages';
  import DockIcon from './DockIcon.svelte';

  const GAP = 6;
  const PAD = 6;
  const SEPARATOR = 13; // šířka oddělovače včetně okrajů

  let base = $derived(host.settings?.dock.iconSize ?? 48);
  let maxScale = $derived(host.settings?.dock.magnification === false ? 1 : (host.settings?.dock.maxScale ?? 1.7));
  let items = $derived(host.dock);
  let separatorAfter = $derived.by(() => {
    const lastPinned = items.findLastIndex((i) => i.pinned);
    return lastPinned >= 0 && lastPinned < items.length - 1 ? lastPinned : -1;
  });

  let band: HTMLDivElement | undefined = $state();
  let cursor = $state<number | null>(null); // px od levého okraje pásu v klidu
  let strength = $state(0);
  let targetStrength = 0;
  let hovered = $state<string | null>(null);
  let launching = $state<Record<string, number>>({});
  let frame = 0;

  // Středy ikon v klidovém rozložení.
  let centers = $derived.by(() => {
    let x = PAD;
    return items.map((_, i) => {
      const c = x + (base + GAP) / 2;
      x += base + GAP + (i === separatorAfter ? SEPARATOR : 0);
      return c;
    });
  });
  let restWidth = $derived(items.length * (base + GAP) + PAD * 2 + (separatorAfter >= 0 ? SEPARATOR : 0));
  let result = $derived(magnify({ centers, cursor, base, maxScale, strength }));

  function onMove(e: MouseEvent) {
    const restLeft = (window.innerWidth - restWidth) / 2;
    cursor = e.clientX - restLeft;
    targetStrength = 1;
    animate();
  }

  function onLeave() {
    targetStrength = 0;
    hovered = null;
    animate();
  }

  // Plynulý náběh / doznění efektu.
  function animate() {
    if (frame) return;
    const step = () => {
      const reduce = host.performance.reduced; // režim výkonu (zahrnuje i ruční „Omezit animace“)
      strength = reduce ? targetStrength : strength + (targetStrength - strength) * 0.22;
      if (Math.abs(targetStrength - strength) < 0.01) {
        strength = targetStrength;
        if (strength === 0) cursor = null;
        frame = 0;
        report();
        return;
      }
      report();
      frame = requestAnimationFrame(step);
    };
    frame = requestAnimationFrame(step);
  }

  function activate(item: DockItem) {
    if (!item.running) launching = { ...launching, [item.appId]: Date.now() };
    send('dock.activate', { appId: item.appId });
  }

  // Poskakování končí, když aplikace naběhne (nebo po 6 s).
  $effect(() => {
    const running = new Set(items.filter((i) => i.running).map((i) => i.appId));
    const now = Date.now();
    const next = Object.fromEntries(Object.entries(launching).filter(([id, t]) => !running.has(id) && now - t < 6000));
    if (Object.keys(next).length !== Object.keys(launching).length) launching = next;
  });
  setInterval(() => (launching = { ...launching }), 2000);

  $effect(() => requestIcons(items.map((i) => i.appId)));

  // ---- Hlášení tvaru do C# ----
  let lastSent = '';
  let reportQueued = false;

  function rectOf(el: Element): CssRect {
    const r = el.getBoundingClientRect();
    return { x: r.left, y: r.top, width: r.width, height: r.height };
  }

  async function report() {
    if (reportQueued) return;
    reportQueued = true;
    await tick();
    reportQueued = false;
    if (!band) return;

    const bandRect = rectOf(band);
    const hit: CssRect[] = [];
    if (items.length > 0) hit.push(inflate(bandRect, 1));
    // Zvětšené ikony a popisek přesahují pás nahoru -> také musí být "uvnitř okna".
    const parts = [...band.querySelectorAll('[data-dock-icon], [data-dock-label]')].map(rectOf);
    const above = parts.filter((r) => r.y < bandRect.y);
    if (above.length > 0) {
      const top = Math.min(...above.map((r) => r.y));
      const left = Math.min(bandRect.x, ...above.map((r) => r.x));
      const right = Math.max(bandRect.x + bandRect.width, ...above.map((r) => r.x + r.width));
      hit.push(inflate({ x: left, y: top, width: right - left, height: bandRect.y - top + 2 }, 2));
    }

    const payload = { hit, band: items.length > 0 ? bandRect : null };
    const json = JSON.stringify(payload, (_, v) => (typeof v === 'number' ? Math.round(v) : v));
    if (json === lastSent) return;
    lastSent = json;
    send('dock.layout', JSON.parse(json));
  }

  function inflate(r: CssRect, by: number): CssRect {
    return { x: r.x - by, y: r.y - by, width: r.width + by * 2, height: r.height + by * 2 };
  }

  // Změna položek / velikosti -> nový tvar.
  $effect(() => {
    void items.length;
    void base;
    void hovered;
    void result;
    report();
  });

  onMount(() => {
    ready('dock');
    window.addEventListener('resize', () => report());
  });
</script>

{#if items.length > 0}
  <div
    class="band"
    bind:this={band}
    style:transform="translateX(calc(-50% + {result.shift}px))"
    style:height="{base + PAD * 2 + 4}px"
    role="toolbar"
    aria-label="Dock"
    tabindex="-1"
    onmousemove={onMove}
    onmouseleave={onLeave}
  >
    {#each items as item, i (item.appId)}
      <DockIcon
        {item}
        size={result.sizes[i] ?? base}
        icon={host.icons[item.appId]}
        hovered={hovered === item.appId}
        launching={item.appId in launching}
        onactivate={() => activate(item)}
        oncontext={() => send('dock.contextMenu', { appId: item.appId })}
        onhover={(h) => (hovered = h ? item.appId : hovered === item.appId ? null : hovered)}
      />
      {#if i === separatorAfter}
        <span class="separator" aria-hidden="true"></span>
      {/if}
    {/each}
  </div>
{/if}

<style>
  .band {
    position: absolute;
    left: 50%;
    bottom: 0;
    display: flex;
    align-items: flex-end;
    gap: 6px;
    padding: 6px 6px 4px;
    border-radius: var(--bw-radius-dock);
    background: var(--bw-dock-tint);
    border: 1px solid var(--bw-dock-border);
    box-sizing: border-box;
  }
  .separator {
    flex: none;
    align-self: stretch;
    width: 1px;
    margin: 4px 3px 6px;
    background: var(--bw-dock-border);
  }
</style>
