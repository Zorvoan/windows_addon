<!--
  Horní lišta. Rozmazané pozadí kreslí systém (DWM) pod celým oknem,
  tady se jen přidává jemný tón a obsah.

  Krok 1: statické rozvržení. Název aplikace (krok 3), počasí a baterie (krok 4)
  a indikátory mikrofonu/kamery (krok 5) se doplní později.
-->
<script lang="ts">
  import Logo from '$lib/components/Logo.svelte';
  import Clock from '$lib/components/Clock.svelte';
  import IconButton from '$lib/components/IconButton.svelte';

  // Dočasně pevně; od kroku 3 přijde z C# (okno v popředí).
  let activeAppName = $state('Better Windows');
</script>

<header class="bar">
  <div class="left">
    <span class="logo"><Logo size={18} /></span>
    <span class="app-name">{activeAppName}</span>
  </div>

  <!-- Volné místo pro Dynamic Island. Ten bude samostatné okno nad lištou. -->
  <div class="island-slot" aria-hidden="true"></div>

  <div class="right">
    <IconButton icon="widgets" label="Widgety" />
    <IconButton icon="search" label="Hledat" />
    <IconButton icon="sliders" label="Ovládací centrum" />
    <span class="clock"><Clock /></span>
  </div>
</header>

<style>
  .bar {
    height: 100vh;
    display: grid;
    grid-template-columns: 1fr var(--bw-island-width) 1fr;
    align-items: center;
    padding: 0 var(--bw-space-2) 0 var(--bw-space-3);
    background: var(--bw-bar-tint);
    color: var(--bw-bar-fg);
    font-size: var(--bw-text-md);
  }
  .left,
  .right {
    display: flex;
    align-items: center;
    gap: var(--bw-space-1);
    min-width: 0;
  }
  .right {
    justify-content: flex-end;
  }
  .logo {
    display: inline-grid;
    margin-right: var(--bw-space-2);
  }
  .app-name {
    font-weight: var(--bw-weight-bold);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .clock {
    padding: 0 var(--bw-space-2);
  }
</style>
