<!-- Textové pole; uloží se Enterem nebo opuštěním pole. -->
<script lang="ts">
  interface Props {
    value: string;
    label: string;
    placeholder?: string;
    onsubmit: (value: string) => void;
  }
  let { value, label, placeholder = '', onsubmit }: Props = $props();
  let draft = $state('');
  $effect(() => {
    draft = value;
  });
  function commit() {
    if (draft.trim() !== value) onsubmit(draft.trim());
  }
</script>

<input
  type="text"
  aria-label={label}
  {placeholder}
  bind:value={draft}
  onblur={commit}
  onkeydown={(e) => {
    if (e.key === 'Enter') commit();
    if (e.key === 'Escape') draft = value;
  }}
/>

<style>
  input {
    height: 32px;
    width: 220px;
    padding: 0 10px;
    border-radius: var(--bw-radius-md);
    border: 1px solid var(--bw-border);
    background: var(--bw-surface);
    color: inherit;
    font: inherit;
    user-select: text;
  }
  input:focus {
    outline: none;
    box-shadow: var(--bw-focus-ring);
  }
</style>
