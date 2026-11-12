<!-- Oranžová tečka = mikrofon, zelená = kamera (právě je používá nějaká aplikace). -->
<script lang="ts">
  import type { PrivacyState } from '$lib/bridge/messages';

  interface Props {
    state: PrivacyState;
  }

  let { state }: Props = $props();
</script>

{#if state.micInUse || state.camInUse}
  <span class="dots">
    {#if state.camInUse}
      <span class="dot cam" title={`Kameru používá: ${state.camApps.join(', ') || 'neznámá aplikace'}`}></span>
    {/if}
    {#if state.micInUse}
      <span class="dot mic" title={`Mikrofon používá: ${state.micApps.join(', ') || 'neznámá aplikace'}`}></span>
    {/if}
  </span>
{/if}

<style>
  .dots {
    display: inline-flex;
    gap: 5px;
    padding: 0 var(--bw-space-1);
  }
  .dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    animation: appear var(--bw-duration-slow) var(--bw-ease-spring);
  }
  .mic {
    background: var(--bw-privacy-mic);
    box-shadow: 0 0 6px var(--bw-privacy-mic);
  }
  .cam {
    background: var(--bw-privacy-cam);
    box-shadow: 0 0 6px var(--bw-privacy-cam);
  }
  @keyframes appear {
    from {
      transform: scale(0);
    }
  }
</style>
