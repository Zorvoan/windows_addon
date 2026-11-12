<!-- Vlastní značka Better Windows (stejný motiv jako ikona aplikace). -->
<script lang="ts">
  interface Props {
    size?: number;
  }
  let { size = 18 }: Props = $props();
  const id = `bw-logo-${Math.random().toString(36).slice(2, 8)}`;
</script>

<svg width={size} height={size} viewBox="0 0 32 32" aria-label="Better Windows" role="img">
  <defs>
    <linearGradient id={id} x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#5856d6" />
      <stop offset="1" stop-color="#28aabe" />
    </linearGradient>
  </defs>
  <rect x="1" y="1" width="30" height="30" rx="8" fill="url(#{id})" />
  <rect x="6" y="6.5" width="20" height="2.6" rx="1.3" fill="#fff" opacity="0.92" />
  <rect x="9" y="21.5" width="14" height="5" rx="2.5" fill="#fff" opacity="0.32" />
  <circle cx="12.6" cy="24" r="1.3" fill="#fff" />
  <circle cx="16" cy="24" r="1.3" fill="#fff" />
  <circle cx="19.4" cy="24" r="1.3" fill="#fff" />
</svg>
