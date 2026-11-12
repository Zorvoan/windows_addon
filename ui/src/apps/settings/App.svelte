<script lang="ts">
  import Surface from '$lib/components/Surface.svelte';
  import Section from '$lib/components/Section.svelte';
  import { onMount } from 'svelte';
  import { ready } from '$lib/bridge/bridge';

  onMount(() => ready('settings'));
</script>

<main>
  <h1>Better Windows</h1>
  <Surface variant="solid">
    <Section
      title="Nastavení"
      description="Zatím jen kostra. Skutečná nastavení (město pro počasí, připnuté aplikace, hlavní panel) přibudou v kroku 13."
    >
      <p class="ok">WebView2 i sestavení rozhraní fungují.</p>
    </Section>
  </Surface>
</main>

<style>
  :global(body) {
    background: var(--bw-page);
  }
  main {
    max-width: 640px; font-family: var(--bw-font);
    margin: 0 auto;
    padding: var(--bw-space-6);
    display: grid;
    gap: var(--bw-space-4);
  }
  h1 {
    margin: 0;
    font-size: 24px;
    font-weight: 600;
  }
  .ok {
    margin: 0;
    color: var(--bw-accent);
  }
</style>
