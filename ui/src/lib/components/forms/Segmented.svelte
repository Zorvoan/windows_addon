<!-- Výběr jedné z několika možností (segmentové tlačítko). -->
<script lang="ts" generics="T extends string">
  interface Props {
    value: T;
    options: { value: T; label: string }[];
    label: string;
    onchange: (value: T) => void;
  }
  let { value, options, label, onchange }: Props = $props();
</script>

<div class="seg" role="radiogroup" aria-label={label}>
  {#each options as o (o.value)}
    <button type="button" role="radio" aria-checked={o.value === value} class:active={o.value === value} onclick={() => onchange(o.value)}>
      {o.label}
    </button>
  {/each}
</div>

<style>
  .seg {
    display: inline-flex;
    flex-wrap: nowrap; /* segmenty vždy v jedné řadě */
    white-space: nowrap;
    gap: 2px;
    padding: 2px;
    border-radius: var(--bw-radius-md);
    background: var(--bw-hover);
  }
  button {
    flex: none;
    height: 28px;
    padding: 0 12px;
    border: 0;
    border-radius: 8px;
    background: transparent;
    font-size: var(--bw-text-sm);
  }
  button:hover {
    background: var(--bw-hover);
  }
  .active,
  .active:hover {
    background: var(--bw-surface);
    box-shadow: var(--bw-shadow-sm);
    font-weight: var(--bw-weight-medium);
  }
</style>
