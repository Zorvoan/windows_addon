<!--
  Značka Better Windows: plochý glyf v barvě textu – lišta nahoře a dock se třemi tečkami
  (tečková matice je podpis celého shellu: vizualizér, Launchpad, ostrůvek).
-->
<script lang="ts">
  interface Props {
    size?: number;
  }
  let { size = 16 }: Props = $props();
</script>

<svg width={size} height={size} viewBox="0 0 16 16" fill="currentColor" aria-label="Better Windows" role="img">
  <rect x="1" y="2" width="14" height="2" rx="1" />
  <circle cx="4.5" cy="12" r="1.5" />
  <circle cx="8" cy="12" r="1.5" />
  <circle cx="11.5" cy="12" r="1.5" />
</svg>
