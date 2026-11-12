<!-- Řádek nastavení: popisek (a nápověda) vlevo, ovládací prvek vpravo. -->
<script lang="ts">
  import type { Snippet } from 'svelte';
  interface Props {
    label: string;
    hint?: string;
    children: Snippet;
  }
  let { label, hint, children }: Props = $props();
</script>

<div class="row">
  <div class="text">
    <div class="label">{label}</div>
    {#if hint}<div class="hint">{hint}</div>{/if}
  </div>
  <div class="control">{@render children()}</div>
</div>

<style>
  .row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: var(--bw-space-4);
    padding: var(--bw-space-3) 0;
    border-top: 1px solid var(--bw-border);
  }
  .row:first-of-type {
    border-top: 0;
  }
  .label {
    font-weight: var(--bw-weight-medium);
  }
  .hint {
    margin-top: 2px;
    color: var(--bw-fg-muted);
    font-size: var(--bw-text-sm);
    max-width: 420px;
  }
  .control {
    display: flex;
    align-items: center;
    gap: var(--bw-space-2);
    flex-wrap: wrap;
    justify-content: flex-end;
  }
</style>
