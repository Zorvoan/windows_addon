<!--
  Nadpis + obsah. Stavební kámen stránek nastavení a později i widgetů.
-->
<script lang="ts">
  import type { Snippet } from 'svelte';

  interface Props {
    title: string;
    description?: string;
    children: Snippet;
  }

  let { title, description, children }: Props = $props();
</script>

<section>
  <header>
    <h2>{title}</h2>
    {#if description}<p>{description}</p>{/if}
  </header>
  {@render children()}
</section>

<style>
  section {
    display: grid;
    gap: var(--bw-space-3);
  }
  h2 {
    margin: 0;
    font-size: 15px;
    font-weight: 600;
  }
  p {
    margin: var(--bw-space-1) 0 0;
    color: var(--bw-fg-muted);
  }
</style>
