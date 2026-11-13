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

<!-- Obal je „kontejner“: když je karta úzká, popisek a ovládání se dají pod sebe (container query). -->
<div class="wrap">
  <div class="row">
    <div class="text">
      <div class="label">{label}</div>
      {#if hint}<div class="hint">{hint}</div>{/if}
    </div>
    <div class="control">{@render children()}</div>
  </div>
</div>

<style>
  .wrap {
    container-type: inline-size;
    border-top: 1px solid var(--bw-border);
  }
  .wrap:first-of-type {
    border-top: 0;
  }
  .row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: var(--bw-space-4);
    padding: var(--bw-space-3) 0;
  }
  .text {
    flex: 1 1 auto;
    min-width: 0;
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
    flex: 0 0 auto; /* ovládání se nesmršťuje ani nezalamuje – zalomí se radši popisek */
    justify-content: flex-end;
  }
  /* Úzká karta: popisek nahoře, ovládání pod ním (nic se neláme uprostřed segmentů). */
  @container (max-width: 520px) {
    .row {
      flex-direction: column;
      align-items: stretch;
      gap: var(--bw-space-2);
    }
    .control {
      justify-content: flex-start;
      flex-wrap: wrap;
    }
  }
</style>
