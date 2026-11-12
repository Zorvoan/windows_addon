<!--
  Obecný "panel": zaoblený podklad s okrajem. Použije se v nastavení,
  widgetech, nabídce Start i vyskakovacích nabídkách.
-->
<script lang="ts">
  import type { Snippet } from 'svelte';

  interface Props {
    /** "translucent" = poloprůhledný (nad rozmazaným pozadím okna), "solid" = neprůhledný */
    variant?: 'translucent' | 'solid';
    padding?: 'none' | 'sm' | 'md' | 'lg';
    children: Snippet;
  }

  let { variant = 'solid', padding = 'md', children }: Props = $props();
</script>

<div class="surface {variant} pad-{padding}">
  {@render children()}
</div>

<style>
  .surface {
    border: 1px solid var(--bw-border);
    border-radius: var(--bw-radius-md);
  }
  .solid {
    background: var(--bw-surface-strong);
  }
  .translucent {
    background: var(--bw-surface);
  }
  .pad-none { padding: 0; }
  .pad-sm { padding: var(--bw-space-2); }
  .pad-md { padding: var(--bw-space-4); }
  .pad-lg { padding: var(--bw-space-6); }
</style>
