<script lang="ts">
  import type { Snippet } from 'svelte';
  interface Props {
    variant?: 'default' | 'primary' | 'danger';
    disabled?: boolean;
    onclick?: () => void;
    children: Snippet;
  }
  let { variant = 'default', disabled = false, onclick, children }: Props = $props();
</script>

<button type="button" class={variant} {disabled} {onclick}>{@render children()}</button>

<style>
  button {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    height: 32px;
    padding: 0 14px;
    border-radius: var(--bw-radius-md);
    border: 1px solid var(--bw-border);
    background: var(--bw-surface);
    font-size: var(--bw-text-sm);
    font-weight: var(--bw-weight-medium);
    white-space: nowrap;
  }
  button:hover {
    background: var(--bw-hover);
  }
  button:disabled {
    opacity: 0.5;
  }
  .primary {
    background: var(--bw-accent);
    border-color: transparent;
    color: white;
  }
  .primary:hover {
    filter: brightness(1.08);
    background: var(--bw-accent);
  }
  .danger {
    color: #ef4444;
  }
</style>
