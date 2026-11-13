<!--
  Malé tlačítko s ikonou (horní lišta, nabídky, widgety).
-->
<script lang="ts">
  import Icon from './Icon.svelte';
  import type { IconName } from '$lib/icons';

  interface Props {
    icon: IconName;
    /** Popisek pro čtečky obrazovky a bublinu po najetí myší. */
    label: string;
    size?: number;
    onclick?: (e: MouseEvent) => void;
  }

  let { icon, label, size = 16, onclick }: Props = $props();
</script>

<button type="button" title={label} aria-label={label} {onclick}>
  <Icon name={icon} {size} />
</button>

<style>
  button {
    display: inline-grid;
    place-items: center;
    height: 28px;
    min-width: 28px;
    padding: 0 var(--bw-space-2);
    border: 0;
    border-radius: var(--bw-radius-sm);
    background: transparent;
    cursor: default;
    color: inherit;
    transition: background-color var(--bw-duration-fast) var(--bw-ease);
  }
  button:hover {
    background: var(--bw-hover);
  }
  button:active {
    background: var(--bw-pressed);
  }
</style>
