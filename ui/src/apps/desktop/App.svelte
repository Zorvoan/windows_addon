<!--
  Vrstva plochy: leží pod všemi aplikacemi. Kreslí tapetu, nad ní vizualizér a widgety.
  Rozmazání karet = CSS backdrop-filter nad tapetou (ve stejné stránce), ne systémové.
-->
<script lang="ts">
  import { onMount } from 'svelte';
  import { ready } from '$lib/bridge/bridge';
  import { host } from '$lib/stores/host.svelte';
  import Visualizer from '$lib/components/Visualizer.svelte';
  import TestCard from './TestCard.svelte';
  import WidgetLayer from './WidgetLayer.svelte';

  let loadedUrl = $state<string | null>(null);
  let desktop = $derived(host.settings?.desktop);

  onMount(() => ready('desktop'));
</script>

<main class="desktop" style:background-color={host.wallpaper?.color ?? '#1e2430'}>
  {#if host.wallpaper?.url}
    {#key host.wallpaper.url}
      <img
        class="wallpaper"
        class:visible={loadedUrl === host.wallpaper.url}
        src={host.wallpaper.url}
        alt=""
        draggable="false"
        onload={() => (loadedUrl = host.wallpaper?.url ?? null)}
      />
    {/key}
  {/if}

  {#if desktop?.visualizer.enabled}
    <Visualizer scene={desktop.visualizer.scene} />
  {/if}

  {#if desktop?.showWidgets}
    <WidgetLayer />
  {/if}

  {#if desktop?.showTestCard}
    <TestCard />
  {/if}
</main>

<style>
  .desktop {
    position: fixed;
    inset: 0;
    overflow: hidden;
  }
  .wallpaper {
    position: absolute;
    inset: 0;
    width: 100%;
    height: 100%;
    object-fit: cover;
    opacity: 0;
    transition: opacity var(--bw-duration-slow) var(--bw-ease);
  }
  .wallpaper.visible {
    opacity: 1;
  }
</style>
