<!--
  Obecný panel. Použije se v nastavení, widgetech, nabídce Start i vyskakovacích nabídkách.
   - "solid": neprůhledný (okno nastavení)
   - "glass": sklo s rozmazáním toho, co je ZA ním ve stejné stránce (widgety nad tapetou)
-->
<script lang="ts">
  import type { Snippet } from 'svelte';

  interface Props {
    variant?: 'solid' | 'glass';
    padding?: 'none' | 'sm' | 'md' | 'lg';
    /** Přesné odsazení v px (přebije `padding`) – widgety ho odvozují od zaoblení rohů. */
    padPx?: number;
    children: Snippet;
  }

  let { variant = 'solid', padding = 'md', padPx, children }: Props = $props();
</script>

<div class="surface {variant} pad-{padding}" style:padding={padPx !== undefined ? `${padPx}px` : null}>
  {@render children()}
</div>

<style>
  .surface {
    border-radius: var(--bw-radius-card);
  }
  .solid {
    background: var(--bw-surface);
    border: 1px solid var(--bw-border);
  }
  .glass {
    background: var(--bw-glass-bg);
    border: 1px solid var(--bw-glass-border);
    box-shadow:
      inset 0 1px 0 var(--bw-glass-highlight),
      var(--bw-shadow-card);
    backdrop-filter: blur(var(--bw-glass-blur)) saturate(var(--bw-glass-saturate));
  }
  .pad-none { padding: 0; }
  .pad-sm { padding: var(--bw-space-2); }
  .pad-md { padding: var(--bw-space-4); }
  .pad-lg { padding: var(--bw-space-6); }
</style>
