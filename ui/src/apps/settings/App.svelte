<!--
  Okno nastavení. Každá změna se hned uloží (zpráva settings.update) a C# ji použije.
-->
<script lang="ts">
  import { onMount } from 'svelte';
  import { ready, send } from '$lib/bridge/bridge';
  import { host } from '$lib/stores/host.svelte';
  import { requestIcons } from '$lib/stores/icons';
  import Surface from '$lib/components/Surface.svelte';
  import Section from '$lib/components/Section.svelte';
  import Logo from '$lib/components/Logo.svelte';
  import Icon from '$lib/components/Icon.svelte';
  import Toggle from '$lib/components/forms/Toggle.svelte';
  import Segmented from '$lib/components/forms/Segmented.svelte';
  import Slider from '$lib/components/forms/Slider.svelte';
  import Button from '$lib/components/forms/Button.svelte';
  import TextField from '$lib/components/forms/TextField.svelte';
  import SettingRow from '$lib/components/forms/SettingRow.svelte';
  import type { AppSettings, BackdropMode, SettingsAction } from '$lib/bridge/messages';

  let s = $derived(host.settings);
  let confirmTaskbar = $state(false);

  const sections = [
    { id: 'appearance', title: 'Vzhled' },
    { id: 'desktop', title: 'Plocha' },
    { id: 'visualizer', title: 'Vizualizér' },
    { id: 'dock', title: 'Dock' },
    { id: 'weather', title: 'Počasí' },
    { id: 'taskbar', title: 'Hlavní panel' },
    { id: 'about', title: 'O aplikaci' },
  ];

  const backdropOptions: { value: BackdropMode; label: string }[] = [
    { value: 'systemAcrylic', label: 'Systémové' },
    { value: 'accentAcrylic', label: 'Vlastní tón' },
    { value: 'none', label: 'Žádné' },
  ];

  const modeLabels: Record<string, string> = {
    'embed-raised': 'Vloženo do plochy (Windows 11 24H2+)',
    'embed-classic': 'Vloženo do plochy (klasicky)',
    bottom: 'Samostatné okno vespod (HWND_BOTTOM)',
    failed: 'Selhalo',
    off: 'Vypnuto',
  };

  /** Upraví kopii nastavení a pošle ji do C# (ten ji uloží a použije). */
  function update(mutate: (settings: AppSettings) => void) {
    if (!host.settings) return;
    const copy = $state.snapshot(host.settings) as AppSettings;
    mutate(copy);
    host.settings = copy; // hned ukázat; C# pošle potvrzený stav zpět
    send('settings.update', { settings: copy });
  }

  function action(name: SettingsAction, arg: string | null = null) {
    send('settings.action', { action: name, arg });
  }

  $effect(() => {
    if (s) requestIcons(s.dock.pinned.map((p) => p.appId));
  });

  onMount(() => ready('settings'));
</script>

<div class="layout">
  <nav>
    <div class="brand"><Logo size={22} /> Better Windows</div>
    {#each sections as sec (sec.id)}
      <a href="#{sec.id}">{sec.title}</a>
    {/each}
  </nav>

  <main>
    {#if !s}
      <p class="muted">Načítám nastavení…</p>
    {:else}
      <section id="appearance">
        <Surface>
          <Section title="Vzhled" description="Barvy a průhlednost se řídí jedním místem – designovými tokeny.">
            <div>
              <SettingRow label="Motiv">
                <Segmented
                  label="Motiv"
                  value={s.appearance.theme}
                  options={[
                    { value: 'system', label: 'Podle Windows' },
                    { value: 'light', label: 'Světlý' },
                    { value: 'dark', label: 'Tmavý' },
                  ]}
                  onchange={(v) => update((x) => (x.appearance.theme = v))}
                />
              </SettingRow>
              <SettingRow label="Rozmazání horní lišty" hint="Systémové = Windows 11 Acrylic. Vlastní tón = starší API, průhlednější.">
                <Segmented label="Rozmazání horní lišty" value={s.appearance.topBarBackdrop} options={backdropOptions} onchange={(v) => update((x) => (x.appearance.topBarBackdrop = v))} />
              </SettingRow>
              <SettingRow label="Rozmazání docku">
                <Segmented label="Rozmazání docku" value={s.appearance.dockBackdrop} options={backdropOptions} onchange={(v) => update((x) => (x.appearance.dockBackdrop = v))} />
              </SettingRow>
              <SettingRow label="Omezit animace" hint="Vypne zvětšování s doběhem a další plynulé přechody.">
                <Toggle label="Omezit animace" checked={s.appearance.reduceMotion} onchange={(v) => update((x) => (x.appearance.reduceMotion = v))} />
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="desktop">
        <Surface>
          <Section title="Plocha" description="Vrstva pod všemi aplikacemi: tapeta, widgety a vizualizér.">
            <div>
              <SettingRow label="Vrstva plochy">
                <Toggle label="Vrstva plochy" checked={s.desktop.enabled} onchange={(v) => update((x) => (x.desktop.enabled = v))} />
              </SettingRow>
              <SettingRow label="Způsob umístění" hint="Automaticky zkusí vložení do plochy Windows, při neúspěchu okno vespod. Okno vespod nedostává klávesnici.">
                <Segmented
                  label="Způsob umístění"
                  value={s.desktop.mode}
                  options={[
                    { value: 'auto', label: 'Automaticky' },
                    { value: 'embed', label: 'Vložit do plochy' },
                    { value: 'bottom', label: 'Okno vespod' },
                  ]}
                  onchange={(v) => update((x) => (x.desktop.mode = v))}
                />
              </SettingRow>
              <SettingRow label="Stav" hint={host.desktop?.detail ?? undefined}>
                <span class="status">{host.desktop ? (modeLabels[host.desktop.mode] ?? host.desktop.mode) : '–'}</span>
              </SettingRow>
              <SettingRow label="Skrýt ikony plochy" hint="Jen když je vrstva vložená do plochy. Bez skrytí ikon nereagují widgety na myš.">
                <Toggle label="Skrýt ikony plochy" checked={s.desktop.hideIcons} onchange={(v) => update((x) => (x.desktop.hideIcons = v))} />
              </SettingRow>
              <SettingRow label="Tapeta" hint={s.desktop.wallpaperPath ?? 'Aktuální tapeta Windows'}>
                <Button onclick={() => action('pickWallpaper')}><Icon name="image" size={14} /> Vybrat obrázek…</Button>
                {#if s.desktop.wallpaperPath}
                  <Button onclick={() => action('clearWallpaper')}>Použít tapetu Windows</Button>
                {/if}
              </SettingRow>
              <SettingRow label="Widgety">
                <Toggle label="Widgety" checked={s.desktop.showWidgets} onchange={(v) => update((x) => (x.desktop.showWidgets = v))} />
              </SettingRow>
              <SettingRow label="Testovací karta" hint="Diagnostika vrstvy plochy (režim, test myši a klávesnice).">
                <Toggle label="Testovací karta" checked={s.desktop.showTestCard} onchange={(v) => update((x) => (x.desktop.showTestCard = v))} />
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="visualizer">
        <Surface>
          <Section title="Vizualizér" description="Reaguje na zvuk, který hraje z počítače (ne na mikrofon).">
            <div>
              <SettingRow label="Zapnuto">
                <Toggle label="Vizualizér" checked={s.desktop.visualizer.enabled} onchange={(v) => update((x) => (x.desktop.visualizer.enabled = v))} />
              </SettingRow>
              <SettingRow label="Scéna">
                <Segmented
                  label="Scéna"
                  value={s.desktop.visualizer.scene}
                  options={[
                    { value: 'dots', label: 'Tečky' },
                    { value: 'wave', label: 'Vlna' },
                    { value: 'ring', label: 'Kruh' },
                  ]}
                  onchange={(v) => update((x) => (x.desktop.visualizer.scene = v))}
                />
              </SettingRow>
              <SettingRow label="Citlivost">
                <Slider label="Citlivost" min={0.2} max={3} step={0.1} value={s.desktop.visualizer.sensitivity} format={(v) => `${v.toFixed(1)}×`} onchange={(v) => update((x) => (x.desktop.visualizer.sensitivity = v))} />
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="dock">
        <Surface>
          <Section title="Dock">
            <div>
              <SettingRow label="Zapnuto">
                <Toggle label="Dock" checked={s.dock.enabled} onchange={(v) => update((x) => (x.dock.enabled = v))} />
              </SettingRow>
              <SettingRow label="Velikost ikon">
                <Slider label="Velikost ikon" min={32} max={96} step={2} value={s.dock.iconSize} format={(v) => `${v} px`} onchange={(v) => update((x) => (x.dock.iconSize = v))} />
              </SettingRow>
              <SettingRow label="Zvětšování při najetí">
                <Toggle label="Zvětšování" checked={s.dock.magnification} onchange={(v) => update((x) => (x.dock.magnification = v))} />
              </SettingRow>
              <SettingRow label="Největší zvětšení">
                <Slider label="Největší zvětšení" min={1} max={2.5} step={0.1} value={s.dock.maxScale} format={(v) => `${v.toFixed(1)}×`} onchange={(v) => update((x) => (x.dock.maxScale = v))} />
              </SettingRow>
              <SettingRow label="Připnuté aplikace" hint="Připnout jde pravým tlačítkem na ikonu v docku.">
                <span></span>
              </SettingRow>
              <ul class="pinned">
                {#each s.dock.pinned as p (p.appId)}
                  <li>
                    {#if host.icons[p.appId]}<img src={host.icons[p.appId]} alt="" />{:else}<span class="ph">{p.name.charAt(0)}</span>{/if}
                    <span class="name" title={p.appId}>{p.name}</span>
                    <Button onclick={() => action('unpin', p.appId)}>Odepnout</Button>
                  </li>
                {:else}
                  <li class="muted">Nic není připnuté.</li>
                {/each}
              </ul>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="weather">
        <Surface>
          <Section title="Počasí" description="Open-Meteo, bez účtu a API klíče.">
            <div>
              <SettingRow label="Zapnuto">
                <Toggle label="Počasí" checked={s.weather.enabled} onchange={(v) => update((x) => (x.weather.enabled = v))} />
              </SettingRow>
              <SettingRow
                label="Město"
                hint={s.weather.resolved ? `Nalezeno: ${s.weather.resolved.name}${s.weather.resolved.country ? ', ' + s.weather.resolved.country : ''}` : (host.weather?.error ?? 'Uloží se Enterem')}
              >
                <TextField label="Město" value={s.weather.city} placeholder="např. Praha" onsubmit={(v) => update((x) => (x.weather.city = v))} />
                <Button onclick={() => action('refreshWeather')}><Icon name="refresh" size={14} /> Obnovit</Button>
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="taskbar">
        <Surface>
          <Section title="Hlavní panel Windows">
            <div>
              <SettingRow
                label="Skrýt hlavní panel"
                hint="Vrátí se při ukončení aplikace, při pádu i po zabití procesu. Windows si přitom zapamatuje „automaticky skrývat“ – to aplikace při ukončení také vrací."
              >
                {#if confirmTaskbar}
                  <span class="confirm">Opravdu skrýt?</span>
                  <Button
                    variant="primary"
                    onclick={() => {
                      confirmTaskbar = false;
                      update((x) => (x.taskbar.hide = true));
                    }}>Skrýt</Button
                  >
                  <Button onclick={() => (confirmTaskbar = false)}>Zrušit</Button>
                {:else}
                  <Toggle
                    label="Skrýt hlavní panel"
                    checked={s.taskbar.hide}
                    onchange={(v) => {
                      if (v) confirmTaskbar = true;
                      else update((x) => (x.taskbar.hide = false));
                    }}
                  />
                {/if}
              </SettingRow>
              <SettingRow label="Nouzová obnova" hint="Ukáže ikony plochy i hlavní panel a vypne jejich skrývání. Totéž je v nabídce ikony v oznamovací oblasti.">
                <Button onclick={() => action('restoreSystem')}>Obnovit ikony plochy a hlavní panel</Button>
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="about">
        <Surface>
          <Section title="O aplikaci">
            <div>
              <dl class="about">
                <dt>Verze</dt>
                <dd>{host.diagnostics?.version ?? '–'}</dd>
                <dt>Windows</dt>
                <dd>{host.diagnostics?.windowsVersion ?? '–'}</dd>
                <dt>Vrstva plochy</dt>
                <dd>{host.diagnostics?.desktopMode ?? '–'} {host.diagnostics?.desktopDetail ? `– ${host.diagnostics.desktopDetail}` : ''}</dd>
                <dt>Data</dt>
                <dd class="path">{host.diagnostics?.dataDir ?? '–'}</dd>
              </dl>
              <div class="buttons">
                <Button onclick={() => action('openLogs')}><Icon name="folder" size={14} /> Otevřít logy</Button>
                <Button onclick={() => action('openDataDir')}><Icon name="folder" size={14} /> Složka s daty</Button>
              </div>
            </div>
          </Section>
        </Surface>
      </section>
    {/if}
  </main>
</div>

<style>
  :global(body) {
    background: var(--bw-page);
  }
  :global(html) {
    scroll-behavior: smooth;
  }
  .layout {
    display: grid;
    grid-template-columns: 200px 1fr;
    min-height: 100vh;
  }
  nav {
    position: sticky;
    top: 0;
    height: 100vh;
    padding: var(--bw-space-6) var(--bw-space-3);
    display: flex;
    flex-direction: column;
    gap: 2px;
    border-right: 1px solid var(--bw-border);
  }
  .brand {
    display: flex;
    align-items: center;
    gap: var(--bw-space-2);
    font-weight: var(--bw-weight-bold);
    padding: 0 var(--bw-space-2) var(--bw-space-4);
  }
  nav a {
    padding: 7px var(--bw-space-3);
    border-radius: var(--bw-radius-sm);
    color: inherit;
    text-decoration: none;
  }
  nav a:hover {
    background: var(--bw-hover);
  }
  main {
    padding: var(--bw-space-6);
    display: grid;
    gap: var(--bw-space-4);
    max-width: 820px;
    align-content: start;
  }
  section {
    scroll-margin-top: var(--bw-space-6);
  }
  .muted {
    color: var(--bw-fg-muted);
  }
  .status {
    font-weight: var(--bw-weight-medium);
  }
  .confirm {
    color: #ef4444;
    font-weight: var(--bw-weight-medium);
  }
  .pinned {
    list-style: none;
    margin: 0;
    padding: 0;
    display: grid;
    gap: var(--bw-space-2);
  }
  .pinned li {
    display: flex;
    align-items: center;
    gap: var(--bw-space-3);
  }
  .pinned img,
  .ph {
    width: 28px;
    height: 28px;
    flex: none;
  }
  .ph {
    display: grid;
    place-items: center;
    border-radius: 8px;
    background: var(--bw-hover);
    font-weight: var(--bw-weight-bold);
  }
  .pinned .name {
    flex: 1;
  }
  .about {
    display: grid;
    grid-template-columns: 140px 1fr;
    gap: var(--bw-space-2);
    margin: 0 0 var(--bw-space-4);
  }
  dt {
    color: var(--bw-fg-muted);
  }
  dd {
    margin: 0;
  }
  .path {
    user-select: text;
    word-break: break-all;
  }
  .buttons {
    display: flex;
    gap: var(--bw-space-2);
  }
</style>
