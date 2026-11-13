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
      <span class="dot cam" role="img" aria-label={`Kameru používá: ${state.camApps.join(', ') || 'neznámá aplikace'}`} title={`Kameru používá: ${state.camApps.join(', ') || 'neznámá aplikace'}`}></span>
    {/if}
    {#if state.micInUse}
      <span class="dot mic" role="img" aria-label={`Mikrofon používá: ${state.micApps.join(', ') || 'neznámá aplikace'}`} title={`Mikrofon používá: ${state.micApps.join(', ') || 'neznámá aplikace'}`}></span>
    {/if}
  </span>
{/if}

<style>
  .dots {
    display: inline-flex;
    gap: var(--bw-space-1);
    padding: 0 var(--bw-space-1);
  }
  .dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
  }
  .mic {
    background: var(--bw-privacy-mic);
  }
  /* Kamera má jiný TVAR než mikrofon (zaoblený čtverec vs. kruh) – nerozlišuje se jen barvou. */
  .cam {
    background: var(--bw-privacy-cam);
    border-radius: 2px;
  }
</style>
