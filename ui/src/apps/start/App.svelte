<!--
  Start / Launchpad: vyhledávání + mřížka všech aplikací (shell:AppsFolder).
  Šipky = výběr, Enter = spustit, Esc = zavřít, pravé tlačítko = připnout do docku.
-->
<script lang="ts">
  import { onMount, tick } from 'svelte';
  import Icon from '$lib/components/Icon.svelte';
  import { host } from '$lib/stores/host.svelte';
  import { ready, send } from '$lib/bridge/bridge';
  import { requestIcons } from '$lib/stores/icons';
  import { rank } from '$lib/search';
  import type { StartApp } from '$lib/bridge/messages';

  const COLUMNS = 6;
  let query = $state('');
  let selected = $state(0);
  let input: HTMLInputElement | undefined = $state();
  let grid: HTMLDivElement | undefined = $state();
  let menu = $state<{ app: StartApp; x: number; y: number } | null>(null);

  let results = $derived(rank(host.startApps, query, (a) => a.name));

  $effect(() => {
    void query;
    selected = 0;
  });

  // Ikony jen pro to, co je vidět (první stovka výsledků).
  $effect(() => requestIcons(results.slice(0, 120).map((a) => a.appId)));

  // Při otevření: vyčistit hledání a dát fokus do pole.
  $effect(() => {
    if (host.startOpen) {
      query = '';
      menu = null;
      tick().then(() => input?.focus());
    }
  });

  function launch(app: StartApp | undefined) {
    if (!app) return;
    send('start.launch', { appId: app.appId });
  }

  async function keydown(e: KeyboardEvent) {
    if (e.key === 'Escape') {
      if (menu) menu = null;
      else send('start.close', {});
      return;
    }
    if (e.key === 'Enter') {
      launch(results[selected]);
      return;
    }
    const move: Record<string, number> = { ArrowRight: 1, ArrowLeft: -1, ArrowDown: COLUMNS, ArrowUp: -COLUMNS };
    const delta = move[e.key];
    if (delta !== undefined && results.length > 0) {
      if ((e.key === 'ArrowLeft' || e.key === 'ArrowRight') && document.activeElement === input && query) return; // posun kurzoru v textu
      e.preventDefault();
      selected = Math.max(0, Math.min(results.length - 1, selected + delta));
      await tick();
      grid?.querySelector('.app.selected')?.scrollIntoView({ block: 'nearest' });
    }
  }

  onMount(() => {
    ready('start');
    input?.focus();
  });
</script>

<svelte:window onkeydown={keydown} onclick={() => (menu = null)} />

<div class="start" class:open={host.startOpen}>
  <div class="search">
    <Icon name="search" size={18} />
    <input bind:this={input} bind:value={query} type="text" placeholder="Hledat" aria-label="Hledat aplikace" spellcheck="false" />
    <button type="button" class="close" aria-label="Zavřít" onclick={() => send('start.close', {})}><Icon name="close" size={14} /></button>
  </div>

  <div class="grid" bind:this={grid} style:--cols={COLUMNS}>
    {#each results as app, i (app.appId)}
      <button
        type="button"
        class="app"
        class:selected={i === selected}
        title={app.name}
        onclick={() => launch(app)}
        onmouseenter={() => (selected = i)}
        oncontextmenu={(e) => {
          e.preventDefault();
          e.stopPropagation();
          menu = { app, x: e.clientX, y: e.clientY };
        }}
      >
        {#if host.icons[app.appId]}
          <img src={host.icons[app.appId]} alt="" draggable="false" />
        {:else}
          <span class="ph">{app.name.charAt(0).toUpperCase()}</span>
        {/if}
        <span class="name">{app.name}</span>
      </button>
    {:else}
      <p class="empty">{host.startApps.length === 0 ? 'Načítám aplikace…' : `Nic nenalezeno pro „${query}“`}</p>
    {/each}
  </div>

  <footer class="power">
    {#if host.user}<span class="who">{host.user.name}</span>{/if}
    <button type="button" class="power-btn" onclick={() => send('settings.open', {})}><Icon name="sliders" size={15} /> Nastavení</button>
    <button
      type="button"
      class="power-btn"
      onclick={() => {
        send('start.close', {});
        send('lock.open', { editing: false });
      }}><Icon name="lock" size={15} /> Zamknout</button
    >
  </footer>

  {#if menu}
    <div class="menu" style:left="{menu.x}px" style:top="{menu.y}px" role="menu">
      <button type="button" role="menuitem" onclick={() => menu && launch(menu.app)}>Otevřít</button>
      <button
        type="button"
        role="menuitem"
        onclick={() => {
          if (menu) send('start.pin', { appId: menu.app.appId });
          menu = null;
        }}>Připnout do docku</button
      >
    </div>
  {/if}
</div>

<style>
  /*
   * Otevření: obsah se z 97 % zvětší pružinou (--bw-spring) a zprůhlední se do plné viditelnosti.
   * Zavření: krátce a bez pružení zpět (C# okno schová, až to doběhne).
   * Pozn.: systémové rozmazání a zaoblený rám kreslí Windows pro celé okno – ty se objeví/zmizí
   * naráz, animuje se jen obsah (proto jen malé zmenšení, aby rozdíl nebyl vidět). Tónování skla
   * je proto na pozadí stránky, ne na animovaném obsahu: panel se objeví celý najednou s rámem.
   */
  :global(html body.shell) {
    /* Stejný jemný tón jako lišta a dock – silnější tón skla (38 %) nad systémovým rozmazáním
       vypadal jako plná šedá plocha. */
    background: var(--bw-bar-tint);
  }
  .start {
    position: fixed;
    inset: 0;
    display: flex;
    flex-direction: column;
    gap: var(--bw-space-4);
    padding: var(--bw-space-6);
    color: var(--bw-fg);
    opacity: 0;
    transform: scale(0.97);
    pointer-events: none;
    transition:
      opacity var(--bw-duration-fast) var(--bw-ease-in),
      transform var(--bw-duration-fast) var(--bw-ease-in);
  }
  .start.open {
    opacity: 1;
    transform: none;
    pointer-events: auto;
    transition:
      opacity var(--bw-duration) var(--bw-ease),
      transform var(--bw-duration-spring) var(--bw-spring);
  }
  .search {
    display: flex;
    align-items: center;
    gap: var(--bw-space-3);
    height: 44px;
    padding: 0 var(--bw-space-4);
    border-radius: var(--bw-radius-lg);
    background: var(--bw-hover);
    border: 1px solid var(--bw-glass-border);
  }
  .search input {
    flex: 1;
    border: 0;
    background: none;
    color: inherit;
    font: inherit;
    font-size: var(--bw-text-lg);
    outline: none;
    user-select: text;
  }
  .search input:focus-visible {
    box-shadow: none; /* fokus je vidět na celém poli hledání */
  }
  .search:focus-within {
    border-color: var(--bw-accent);
  }
  .close {
    display: grid;
    place-items: center;
    width: 28px;
    height: 28px;
    border: 0;
    border-radius: 50%;
    background: none;
    color: var(--bw-fg-muted);
  }
  .close:hover {
    background: var(--bw-hover);
  }
  .grid {
    flex: 1;
    overflow-y: auto;
    display: grid;
    grid-template-columns: repeat(var(--cols), 1fr);
    align-content: start;
    gap: var(--bw-space-2);
  }
  .app {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: var(--bw-space-2);
    padding: var(--bw-space-3) var(--bw-space-1);
    border: 0;
    border-radius: var(--bw-radius-md);
    background: none;
    color: inherit;
    min-width: 0;
  }
  .app.selected {
    background: var(--bw-hover);
  }
  .app img,
  .ph {
    width: 52px;
    height: 52px;
  }
  .app img {
    object-fit: contain;
    filter: drop-shadow(0 2px 4px rgb(0 0 0 / 0.2));
  }
  .ph {
    display: grid;
    place-items: center;
    border-radius: 12px;
    background: var(--bw-raised);
    border: 1px solid var(--bw-border);
    color: var(--bw-fg-muted);
    font-size: 20px;
    font-weight: var(--bw-weight-medium);
  }
  .name {
    max-width: 100%;
    font-size: var(--bw-text-sm);
    text-align: center;
    line-height: 1.25;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }
  .empty {
    grid-column: 1 / -1;
    text-align: center;
    color: var(--bw-fg-muted);
    margin-top: var(--bw-space-6);
  }
  .power {
    display: flex;
    align-items: center;
    justify-content: flex-end;
    gap: var(--bw-space-2);
    padding-top: var(--bw-space-3);
    border-top: 1px solid var(--bw-glass-border);
  }
  .who {
    margin-right: auto;
    font-weight: var(--bw-weight-medium);
  }
  .power-btn {
    display: inline-flex;
    align-items: center;
    gap: var(--bw-space-2);
    height: 32px;
    padding: 0 12px;
    border: 0;
    border-radius: var(--bw-radius-md);
    background: none;
    color: inherit;
  }
  .power-btn:hover {
    background: var(--bw-hover);
  }
  .menu {
    position: fixed;
    display: grid;
    min-width: 180px;
    padding: 4px;
    border-radius: var(--bw-radius-md);
    background: var(--bw-surface);
    border: 1px solid var(--bw-border);
    box-shadow: var(--bw-shadow-md);
  }
  .menu button {
    text-align: left;
    padding: 8px 12px;
    border: 0;
    border-radius: 6px;
    background: none;
    color: inherit;
  }
  .menu button:hover {
    background: var(--bw-accent);
    color: var(--bw-accent-fg);
  }
</style>
