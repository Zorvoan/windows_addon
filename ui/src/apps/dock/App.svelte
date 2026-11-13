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
  import { follow, followTau, motionReduced } from '$lib/motion.svelte';
  import type { CssRect, DockItem } from '$lib/bridge/messages';
  import DockIcon from './DockIcon.svelte';
  import { launchpadIcon, trashEmptyIcon, trashFullIcon } from '$lib/dockIcons';

  // Speciální položky jako v docku Familiar: Launchpad (Start) na začátku, Koš na konci.
  const LAUNCHPAD: DockItem = { appId: 'bw:launchpad', name: 'Launchpad', pinned: true, running: false, active: false, windowCount: 0 };
  const TRASH: DockItem = { appId: 'bw:trash', name: 'Koš', pinned: true, running: false, active: false, windowCount: 0 };

  const GAP = 8;
  const PAD = 8;
  const SEPARATOR = 8;
  const BOUNCE = 0.5;
  const LABEL_SPACE = 40; // popisek nad zvětšenou ikonou // výška skoku při spouštění jako podíl velikosti ikony // oddělovač: 1px čára + okraje (plus mezera GAP)

  let base = $derived(host.settings?.dock.iconSize ?? 48);
  let maxScale = $derived(host.settings?.dock.magnification === false ? 1 : (host.settings?.dock.maxScale ?? 1.7));
  let showLaunchpad = $derived(host.settings?.dock.showLaunchpad !== false);
  let showTrash = $derived(host.settings?.dock.showTrash !== false);
  let items = $derived([...(showLaunchpad ? [LAUNCHPAD] : []), ...host.dock, ...(showTrash ? [TRASH] : [])]);
  // Oddělovače: za připnutými (když jsou i nepřipnuté) a před košem.
  let separators = $derived.by(() => {
    const set = new Set<number>();
    const apps = host.dock;
    const offset = showLaunchpad ? 1 : 0;
    const lastPinned = apps.findLastIndex((i) => i.pinned);
    if (lastPinned >= 0 && lastPinned < apps.length - 1) set.add(lastPinned + offset);
    if (showTrash && items.length > 1) set.add(items.length - 2);
    return set;
  });

  function iconFor(item: DockItem): string | undefined {
    if (item.appId === LAUNCHPAD.appId) return launchpadIcon;
    if (item.appId === TRASH.appId) return host.trashCount > 0 ? trashFullIcon : trashEmptyIcon;
    return host.icons[item.appId];
  }

  let band: HTMLDivElement | undefined = $state();
  let cursor = $state<number | null>(null); // px od levého okraje pásu v klidu
  let strength = $state(0);
  let targetStrength = 0;
  let hovered = $state<string | null>(null);
  let launching = $state<Record<string, number>>({});
  // Ikony, které právě poskakují (i dobíhající poslední skok po naběhnutí aplikace) -> rezerva v oblasti okna.
  let bouncing = $state<Record<string, true>>({});
  let frame = 0;
  let lastFrame = 0;
  // Poslední pozice myši; do stavu (a tím do DOM) se promítne jen jednou za snímek v animate().
  let pendingCursor: number | null = null;

  // Středy ikon v klidovém rozložení.
  let centers = $derived.by(() => {
    let x = PAD;
    return items.map((_, i) => {
      const c = x + (base + GAP) / 2;
      x += base + GAP + (separators.has(i) ? SEPARATOR : 0);
      return c;
    });
  });
  let restWidth = $derived(items.length * (base + GAP) + PAD * 2 + separators.size * SEPARATOR);
  let result = $derived(magnify({ centers, cursor, base, maxScale, strength }));

  function onMove(e: MouseEvent) {
    const restLeft = (window.innerWidth - restWidth) / 2;
    pendingCursor = e.clientX - restLeft;
    targetStrength = 1;
    animate();
  }

  function onLeave() {
    targetStrength = 0;
    hovered = null;
    send('dock.hover', { appId: null, rect: null });
    animate();
  }

  /** Náhledy oken: C# je ukáže nad ikonou spuštěné aplikace (po krátkém zpoždění). */
  function hover(item: DockItem, h: boolean, element: HTMLElement) {
    hovered = h ? item.appId : hovered === item.appId ? null : hovered;
    if (h && item.running) send('dock.hover', { appId: item.appId, rect: rectOf(element) });
    else if (!h) send('dock.hover', { appId: null, rect: null });
  }

  // Plynulý náběh / doznění efektu – podle skutečného času, takže stejně rychle na 60 i 144 Hz.
  function animate() {
    if (frame) return;
    lastFrame = performance.now();
    const step = (now: number) => {
      const dt = now - lastFrame;
      lastFrame = now;
      if (targetStrength > 0 && pendingCursor !== null && pendingCursor !== cursor) cursor = pendingCursor;
      strength = motionReduced() ? targetStrength : follow(strength, targetStrength, dt, followTau.dock);
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

  function setBouncing(appId: string, b: boolean) {
    if (b === appId in bouncing) return;
    const next = { ...bouncing };
    if (b) next[appId] = true;
    else delete next[appId];
    bouncing = next;
  }

  function activate(item: DockItem) {
    if (!item.running && !item.appId.startsWith('bw:')) launching = { ...launching, [item.appId]: Date.now() };
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

  $effect(() => requestIcons(host.dock.map((i) => i.appId)));

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
    // Poskakující ikona: celá dráha skoku nad klidovou polohou (slot se nehýbe, ikona ano).
    for (const el of band.querySelectorAll<HTMLElement>('[data-dock-bouncing]')) {
      const slot = el.parentElement ? rectOf(el.parentElement) : rectOf(el);
      const lift = el.offsetHeight * BOUNCE;
      parts.push({ x: slot.x, y: slot.y - lift - 2, width: slot.width, height: lift + 2 });
    }
    const above = parts.filter((r) => r.y < bandRect.y);
    if (above.length > 0) {
      const top = Math.min(...above.map((r) => r.y));
      const left = Math.min(bandRect.x, ...above.map((r) => r.x));
      const right = Math.max(bandRect.x + bandRect.width, ...above.map((r) => r.x + r.width));
      hit.push(inflate({ x: left, y: top, width: right - left, height: bandRect.y - top + 2 }, 2));
    }

    // Během zvětšování: stálá obálka nejvyššího možného zvětšení místo tvaru po snímcích
    // (každá změna = SetWindowRgn s překreslením okna v C#). Po doznění zase přesný tvar.
    if (items.length > 0 && (targetStrength > 0 || strength > 0)) {
      const env = envelope(bandRect);
      const all = [env, ...hit];
      const left = Math.min(...all.map((r) => r.x));
      const top = Math.min(...all.map((r) => r.y));
      const right = Math.max(...all.map((r) => r.x + r.width));
      const bottom = Math.max(...all.map((r) => r.y + r.height));
      hit.length = 0;
      hit.push({ x: left, y: top, width: right - left, height: bottom - top });
    }

    const payload = { hit, band: items.length > 0 ? bandRect : null };
    const json = JSON.stringify(payload, (_, v) => (typeof v === 'number' ? Math.round(v) : v));
    if (json === lastSent) return;
    lastSent = json;
    send('dock.layout', JSON.parse(json));
  }

  /** Největší rozšíření pásu při zvětšení (přes všechny polohy kurzoru), počítá se jen při změně docku. */
  let maxExtra = $derived.by(() => {
    if (maxScale <= 1 || centers.length === 0) return 0;
    let best = 0;
    const end = restWidth;
    for (let x = 0; x <= end; x += 4) {
      const { sizes } = magnify({ centers, cursor: x, base, maxScale, strength: 1 });
      best = Math.max(best, sizes.reduce((a, v) => a + v, 0) - base * sizes.length);
    }
    return best;
  });

  function envelope(bandRect: CssRect): CssRect {
    const restLeft = (window.innerWidth - restWidth) / 2;
    const bottom = bandRect.y + bandRect.height;
    const lift = base * maxScale * (1 + BOUNCE) + LABEL_SPACE;
    return { x: restLeft - maxExtra - 2, y: bottom - lift, width: restWidth + maxExtra * 2 + 4, height: lift + 2 };
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
    void Object.keys(bouncing).length;
    report();
  });

  onMount(() => {
    ready('dock');
    window.addEventListener('resize', () => report());
  });
</script>

{#if host.settings && items.length > 0}
  <div
    class="band"
    bind:this={band}
    style:transform="translateX(calc(-50% + {result.shift}px))"
    style:height="{base + PAD + 12}px"
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
        icon={iconFor(item)}
        hovered={hovered === item.appId}
        launching={item.appId in launching}
        bounce={BOUNCE}
        onbouncing={(b) => setBouncing(item.appId, b)}
        onactivate={() => activate(item)}
        oncontext={() => send('dock.contextMenu', { appId: item.appId })}
        onhover={(h, el) => hover(item, h, el)}
      />
      {#if separators.has(i)}
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
    gap: 8px;
    padding: 8px 8px 4px;
    border-radius: var(--bw-radius-dock);
    background: var(--bw-dock-tint);
    border: 1px solid var(--bw-dock-border);
    box-sizing: border-box;
  }
  .separator {
    flex: none;
    align-self: stretch;
    width: 1px;
    margin: 4px 0 8px;
    background: var(--bw-dock-border);
  }
</style>
