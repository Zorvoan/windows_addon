<!--
  Testovací skleněná karta (krok 2): ověřuje rozmazání přes CSS backdrop-filter nad tapetou,
  režim vložení plochy a jestli vrstva dostává myš a klávesnici.
-->
<script lang="ts">
  import { desktopTextInput } from '$lib/desktopTextInput';
  import Surface from '$lib/components/Surface.svelte';
  import { host } from '$lib/stores/host.svelte';

  const modeLabels: Record<string, string> = {
    'embed-raised': 'Vloženo do plochy (Windows 11 24H2+)',
    'embed-classic': 'Vloženo do plochy (klasicky)',
    bottom: 'Samostatné okno vespod (HWND_BOTTOM)',
    failed: 'Selhalo',
    off: 'Vypnuto',
  };

  let clicks = $state(0);
  let typed = $state('');
  let mode = $derived(host.desktop ? (modeLabels[host.desktop.mode] ?? host.desktop.mode) : 'Čekám na údaje…');
</script>

<div class="card">
  <Surface variant="glass" padding="lg">
    <div class="content">
      <h2>Vrstva plochy</h2>
      <dl>
        <dt>Režim</dt>
        <dd>{mode}</dd>
        {#if host.desktop?.detail}
          <dt>Detail</dt>
          <dd class="muted">{host.desktop.detail}</dd>
        {/if}
        <dt>Ikony plochy</dt>
        <dd>{host.desktop?.iconsHidden ? 'skryté' : 'viditelné'}</dd>
      </dl>
      <div class="tests">
        <button type="button" onclick={() => clicks++}>Test myši: {clicks}×</button>
        <input type="text" placeholder="Test klávesnice – zkus psát" bind:value={typed} use:desktopTextInput />
      </div>
      <p class="hint">Kartu vypneš v Nastavení › Plocha</p>
    </div>
  </Surface>
</div>

<style>
  .card {
    position: absolute;
    right: 32px;
    top: 64px;
    width: 340px;
    color: var(--bw-fg);
  }
  .content {
    display: grid;
    gap: var(--bw-space-3);
  }
  h2 {
    margin: 0;
    font-size: var(--bw-text-lg);
    font-weight: var(--bw-weight-medium);
  }
  dl {
    display: grid;
    grid-template-columns: auto 1fr;
    gap: var(--bw-space-1) var(--bw-space-3);
    margin: 0;
  }
  dt {
    color: var(--bw-fg-muted);
  }
  dd {
    margin: 0;
  }
  .muted,
  .hint {
    color: var(--bw-fg-muted);
    font-size: var(--bw-text-sm);
  }
  .hint {
    margin: 0;
  }
  .tests {
    display: grid;
    gap: var(--bw-space-2);
  }
  button,
  input {
    height: 32px;
    border-radius: var(--bw-radius-md);
    border: 1px solid var(--bw-glass-border);
    background: var(--bw-hover);
    padding: 0 var(--bw-space-3);
    font: inherit;
    color: inherit;
    user-select: text;
  }
  button:hover {
    background: var(--bw-pressed);
  }
</style>
