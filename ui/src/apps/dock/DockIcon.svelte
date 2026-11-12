<!-- Jedna ikona v docku: obrázek (nebo písmeno), tečka spuštěné aplikace, popisek po najetí. -->
<script lang="ts">
  import type { DockItem } from '$lib/bridge/messages';

  interface Props {
    item: DockItem;
    size: number;
    icon: string | undefined;
    hovered: boolean;
    launching: boolean;
    onactivate: () => void;
    oncontext: () => void;
    onhover: (hover: boolean, element: HTMLElement) => void;
  }

  let { item, size, icon, hovered, launching, onactivate, oncontext, onhover }: Props = $props();
  let letter = $derived(item.name.trim().charAt(0).toUpperCase() || '?');
</script>

<div class="slot" style:width="{size}px">
  {#if hovered}
    <div class="label" data-dock-label>{item.name}</div>
  {/if}
  <button
    type="button"
    class="icon"
    class:launching
    data-dock-icon
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
    background: linear-gradient(160deg, #5856d6, #28aabe);
    color: white;
    font-weight: var(--bw-weight-bold);
  }
  .launching {
    animation: bounce 0.9s var(--bw-ease) infinite;
  }
  @keyframes bounce {
    0%,
    100% {
      transform: translateY(0);
    }
    40% {
      transform: translateY(-14px);
    }
  }
  .dot {
    width: 4px;
    height: 4px;
    margin-top: 3px;
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
    bottom: calc(100% + 6px);
    padding: 4px 10px;
    border-radius: var(--bw-radius-pill);
    background: light-dark(rgb(255 255 255 / 0.92), rgb(30 30 34 / 0.92));
    border: 1px solid var(--bw-dock-border);
    color: var(--bw-fg);
    font-size: var(--bw-text-sm);
    white-space: nowrap;
    pointer-events: none;
    box-shadow: var(--bw-shadow-md);
  }
</style>
