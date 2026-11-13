<!-- Jedna ikona v docku: obrázek (nebo písmeno), tečka spuštěné aplikace, popisek po najetí. -->
<script lang="ts">
  import type { DockItem } from '$lib/bridge/messages';
  import { untrack } from 'svelte';
  import { motionReduced } from '$lib/motion.svelte';

  interface Props {
    item: DockItem;
    size: number;
    icon: string | undefined;
    hovered: boolean;
    launching: boolean;
    /** Výška skoku jako podíl velikosti ikony. */
    bounce: number;
    onbouncing: (bouncing: boolean) => void;
    onactivate: () => void;
    oncontext: () => void;
    onhover: (hover: boolean, element: HTMLElement) => void;
  }

  let { item, size, icon, hovered, launching, bounce, onbouncing, onactivate, oncontext, onhover }: Props = $props();
  let letter = $derived(item.name.trim().charAt(0).toUpperCase() || '?');

  // Poskakování při spouštění: stejně vysoké skoky, dokud aplikace nenaběhne. Pak se dokončí
  // rozběhnutý skok (konec cyklu = ikona na zemi), takže ikona nikdy neskočí dolů naráz.
  let bouncing = $state(false);
  let stopTimer = 0;
  $effect(() => {
    if (launching) {
      clearTimeout(stopTimer);
      stopTimer = 0;
      if (!bouncing && !motionReduced()) bouncing = true;
    } else if (bouncing && !stopTimer) {
      stopTimer = window.setTimeout(() => (bouncing = false), 1000); // pojistka, kdyby cyklus neskončil
    }
  });
  $effect(() => {
    const b = bouncing;
    untrack(() => onbouncing(b)); // jen při změně bouncing (ne při změně stavu rodiče)
  });

  function cycleEnd() {
    if (!launching) {
      clearTimeout(stopTimer);
      stopTimer = 0;
      bouncing = false;
    }
  }
</script>

<div class="slot" style:width="{size}px">
  {#if hovered}
    <div class="label" data-dock-label>{item.name}</div>
  {/if}
  <button
    type="button"
    class="icon"
    class:bouncing
    data-dock-icon
    data-dock-bouncing={bouncing ? '' : undefined}
    style:--lift="{-size * bounce}px"
    onanimationiteration={cycleEnd}
    style:width="{size}px"
    style:height="{size}px"
    aria-label={item.name}
    onclick={onactivate}
    oncontextmenu={(e) => {
      e.preventDefault();
      oncontext();
    }}
    onmouseenter={(e) => onhover(true, e.currentTarget)}
    onmouseleave={(e) => onhover(false, e.currentTarget)}
  >
    {#if icon}
      <img src={icon} alt="" draggable="false" />
    {:else}
      <span class="placeholder" style:font-size="{size * 0.42}px">{letter}</span>
    {/if}
  </button>
  <span class="dot" class:visible={item.running} class:active={item.active}></span>
</div>

<style>
  .slot {
    position: relative;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: flex-end;
    flex: none;
  }
  .icon {
    display: grid;
    place-items: center;
    padding: 0;
    border: 0;
    background: none;
    cursor: default;
    transform-origin: bottom center;
  }
  .icon img {
    width: 100%;
    height: 100%;
    object-fit: contain;
    filter: drop-shadow(0 2px 4px rgb(0 0 0 / 0.25));
    pointer-events: none;
  }
  .icon:active img {
    filter: drop-shadow(0 2px 4px rgb(0 0 0 / 0.25)) brightness(0.8);
  }
  .placeholder {
    width: 86%;
    height: 86%;
    display: grid;
    place-items: center;
    border-radius: 22%;
    background: var(--bw-raised);
    border: 1px solid var(--bw-border);
    color: var(--bw-fg-muted);
    font-weight: var(--bw-weight-medium);
  }
  /*
   * Skok jako pod gravitací: nahoru zpomaluje, dolů zrychluje (průběh paraboly), krátká pauza
   * na zemi. Výška je stejná při každém skoku (tak to dělá i dock v macOS při spouštění).
   */
  .bouncing {
    animation: bounce 0.72s linear infinite;
  }
  @keyframes bounce {
    0% {
      transform: translateY(0);
      animation-timing-function: cubic-bezier(0.33, 0.67, 0.67, 1); /* nahoru: zpomalení */
    }
    42% {
      transform: translateY(var(--lift));
      animation-timing-function: cubic-bezier(0.33, 0, 0.67, 0.33); /* dolů: zrychlení */
    }
    84%,
    100% {
      transform: translateY(0);
    }
  }
  @media (prefers-reduced-motion: reduce) {
    .bouncing {
      animation: none;
    }
  }
  :global(:root[data-motion='reduced']) .bouncing {
    animation: none;
  }
  .dot {
    width: 4px;
    height: 4px;
    margin-top: 4px;
    border-radius: 50%;
    background: var(--bw-fg);
    opacity: 0;
    transition:
      opacity var(--bw-duration) var(--bw-ease),
      width var(--bw-duration) var(--bw-ease);
  }
  .dot.visible {
    opacity: 0.55;
  }
  .dot.active {
    opacity: 0.95;
    width: 6px;
  }
  .label {
    position: absolute;
    bottom: calc(100% + 8px);
    padding: 4px 8px;
    border-radius: var(--bw-radius-sm);
    background: light-dark(rgb(255 255 255 / 0.94), rgb(25 25 27 / 0.94));
    border: 1px solid var(--bw-dock-border);
    color: var(--bw-fg);
    font-size: var(--bw-text-sm);
    white-space: nowrap;
    pointer-events: none;
    box-shadow: var(--bw-shadow-md);
  }
</style>
