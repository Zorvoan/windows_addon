<!--
  Okno nastavení. Každá změna se hned uloží (zpráva settings.update) a C# ji použije.
-->
<script lang="ts">
  import { onMount } from 'svelte';
  import { on, ready, send } from '$lib/bridge/bridge';
  import { themePresets } from '$lib/themes';
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
  let themeCode = $state('');
  let importCode = $state('');
  let importMessage = $state<{ ok: boolean; text: string } | null>(null);

  on('theme.code', (p) => (themeCode = p.code));
  on('theme.importResult', (p) => {
    importMessage = p.ok ? { ok: true, text: 'Motiv použit' } : { ok: false, text: p.error ?? 'Kód nejde použít.' };
    if (p.ok) importCode = '';
  });

  const sections = [
    { id: 'appearance', title: 'Vzhled' },
    { id: 'theme', title: 'Motiv' },
    { id: 'performance', title: 'Výkon' },
    { id: 'desktop', title: 'Plocha' },
    { id: 'visualizer', title: 'Vizualizér' },
    { id: 'dock', title: 'Dock' },
    { id: 'start', title: 'Start' },
    { id: 'island', title: 'Dynamic Island' },
    { id: 'lock', title: 'Zamykací obrazovka' },
    { id: 'topbar', title: 'Horní lišta' },
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
    <div class="brand"><Logo size={16} /> Better Windows</div>
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
          <Section title="Vzhled" description="Světlý a tmavý režim se řídí nastavením Windows">
            <div>
              <SettingRow label="Rozmazání horní lišty" hint="Systémové je rozmazání Windows 11, vlastní tón je průhlednější">
                <Segmented label="Rozmazání horní lišty" value={s.appearance.topBarBackdrop} options={backdropOptions} onchange={(v) => update((x) => (x.appearance.topBarBackdrop = v))} />
              </SettingRow>
              <SettingRow label="Rozmazání docku">
                <Segmented label="Rozmazání docku" value={s.appearance.dockBackdrop} options={backdropOptions} onchange={(v) => update((x) => (x.appearance.dockBackdrop = v))} />
              </SettingRow>
              <SettingRow label="Omezit animace" hint="Vypne zvětšování s doběhem a další plynulé přechody">
                <Toggle label="Omezit animace" checked={s.appearance.reduceMotion} onchange={(v) => update((x) => (x.appearance.reduceMotion = v))} />
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="theme">
        <Surface>
          <Section title="Motiv" description="Barvy, průhlednost, rozmazání a zaoblení. Nemění tón systémového rozmazání (ten určuje Windows) ani nativní kontextovou nabídku.">
            <div>
              <div class="presets">
                {#each themePresets as preset (preset.name)}
                  <button
                    type="button"
                    class="preset"
                    class:active={s.appearance.style.name === preset.name}
                    onclick={() => update((x) => (x.appearance.style = { ...preset }))}
                  >
                    <span class="swatch" style:background={preset.useSystemAccent ? host.system.accent : preset.accent}></span>
                    {preset.name}
                  </button>
                {/each}
              </div>
              <SettingRow label="Barva zvýraznění podle Windows" hint="Přizpůsobení › Barvy › Barva zvýraznění">
                <Toggle
                  label="Barva zvýraznění podle Windows"
                  checked={s.appearance.style.useSystemAccent}
                  onchange={(v) => update((x) => (x.appearance.style = { ...x.appearance.style, useSystemAccent: v }))}
                />
              </SettingRow>
              <SettingRow label="Vlastní barva zvýraznění">
                <input
                  disabled={s.appearance.style.useSystemAccent}
                  type="color"
                  class="color"
                  aria-label="Barva zvýraznění"
                  value={s.appearance.style.accent}
                  onchange={(e) => update((x) => (x.appearance.style = { ...x.appearance.style, accent: e.currentTarget.value, useSystemAccent: false, name: 'Vlastní' }))}
                />
              </SettingRow>
              <SettingRow label="Zaoblení karet">
                <Slider label="Zaoblení karet" min={0} max={40} value={s.appearance.style.radiusCard} format={(v) => `${v} px`} onchange={(v) => update((x) => (x.appearance.style = { ...x.appearance.style, radiusCard: v, name: 'Vlastní' }))} />
              </SettingRow>
              <SettingRow label="Rozmazání widgetů">
                <Slider label="Rozmazání widgetů" min={0} max={60} value={s.appearance.style.glassBlur} format={(v) => `${v} px`} onchange={(v) => update((x) => (x.appearance.style = { ...x.appearance.style, glassBlur: v, name: 'Vlastní' }))} />
              </SettingRow>
              <SettingRow label="Neprůhlednost widgetů">
                <Slider label="Neprůhlednost widgetů" min={0} max={1} step={0.05} value={s.appearance.style.glassOpacity} format={(v) => `${Math.round(v * 100)} %`} onchange={(v) => update((x) => (x.appearance.style = { ...x.appearance.style, glassOpacity: v, name: 'Vlastní' }))} />
              </SettingRow>
              <SettingRow label="Tón horní lišty" hint="Přidává barvu přes systémové rozmazání">
                <Slider label="Tón horní lišty" min={0} max={0.8} step={0.02} value={s.appearance.style.barTint} format={(v) => `${Math.round(v * 100)} %`} onchange={(v) => update((x) => (x.appearance.style = { ...x.appearance.style, barTint: v, name: 'Vlastní' }))} />
              </SettingRow>
              <SettingRow label="Tón docku">
                <Slider label="Tón docku" min={0} max={0.8} step={0.02} value={s.appearance.style.dockTint} format={(v) => `${Math.round(v * 100)} %`} onchange={(v) => update((x) => (x.appearance.style = { ...x.appearance.style, dockTint: v, name: 'Vlastní' }))} />
              </SettingRow>
              <SettingRow label="Sdílet motiv" hint="Kód obsahuje motiv a velikost ikon docku. Pošli ho komukoli s Better Windows.">
                <Button onclick={() => send('theme.export', {})}>Vytvořit kód</Button>
              </SettingRow>
              {#if themeCode}
                <div class="code">
                  <input type="text" readonly value={themeCode} aria-label="Kód motivu" onfocus={(e) => e.currentTarget.select()} />
                  <Button onclick={() => navigator.clipboard?.writeText(themeCode)}>Kopírovat</Button>
                </div>
              {/if}
              <SettingRow label="Použít kód motivu">
                <input class="import" type="text" placeholder="BW1.…" aria-label="Kód motivu k importu" bind:value={importCode} />
                <Button variant="primary" disabled={!importCode.trim()} onclick={() => send('theme.import', { code: importCode })}>Použít</Button>
              </SettingRow>
              {#if importMessage}
                <p class="message" class:error={!importMessage.ok}>{importMessage.text}</p>
              {/if}
            </div>
          </Section>
        </Surface>
      </section>

      <section id="performance">
        <Surface>
          <Section title="Výkon" description="Ztlumí animace a pozastaví vizualizér, když na nich nezáleží nebo když šetříš baterii">
            <div>
              <SettingRow label="Režim">
                <Segmented
                  label="Režim výkonu"
                  value={s.performance.mode}
                  options={[
                    { value: 'auto', label: 'Automaticky' },
                    { value: 'alwaysReduced', label: 'Vždy úsporně' },
                    { value: 'neverReduced', label: 'Vždy plné animace' },
                  ]}
                  onchange={(v) => update((x) => (x.performance.mode = v))}
                />
              </SettingRow>
              <SettingRow label="Ztlumit na baterii">
                <Toggle label="Ztlumit na baterii" checked={s.performance.reduceOnBattery} disabled={s.performance.mode !== 'auto'} onchange={(v) => update((x) => (x.performance.reduceOnBattery = v))} />
              </SettingRow>
              <SettingRow label="Ztlumit při aplikaci přes celou obrazovku" hint="Hry, videa, prezentace">
                <Toggle label="Ztlumit při celé obrazovce" checked={s.performance.reduceOnFullscreen} disabled={s.performance.mode !== 'auto'} onchange={(v) => update((x) => (x.performance.reduceOnFullscreen = v))} />
              </SettingRow>
              <SettingRow label="Teď">
                <span class="status">{host.performance.reduced ? 'Ztlumené animace' : 'Plné animace'}</span>
                <span class="muted small">{host.performance.reason}</span>
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="desktop">
        <Surface>
          <Section title="Plocha" description="Vrstva pod všemi aplikacemi: tapeta, widgety a vizualizér">
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
              <SettingRow label="Testovací karta" hint="Diagnostika vrstvy plochy (režim, test myši a klávesnice)">
                <Toggle label="Testovací karta" checked={s.desktop.showTestCard} onchange={(v) => update((x) => (x.desktop.showTestCard = v))} />
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="visualizer">
        <Surface>
          <Section title="Vizualizér" description="Reaguje na zvuk, který hraje z počítače (ne na mikrofon)">
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
              <SettingRow label="Náhledy oken" hint="Po najetí na ikonu spuštěné aplikace">
                <Toggle label="Náhledy oken" checked={s.dock.previews} onchange={(v) => update((x) => (x.dock.previews = v))} />
              </SettingRow>
              <SettingRow label="Launchpad a Koš" hint="Ikona Startu na začátku docku a koš na konci">
                <Toggle label="Launchpad" checked={s.dock.showLaunchpad} onchange={(v) => update((x) => (x.dock.showLaunchpad = v))} />
                <Toggle label="Koš" checked={s.dock.showTrash} onchange={(v) => update((x) => (x.dock.showTrash = v))} />
              </SettingRow>
              <SettingRow label="Největší zvětšení">
                <Slider label="Největší zvětšení" min={1} max={2.5} step={0.1} value={s.dock.maxScale} format={(v) => `${v.toFixed(1)}×`} onchange={(v) => update((x) => (x.dock.maxScale = v))} />
              </SettingRow>
              <SettingRow label="Připnuté aplikace" hint="Připnout jde pravým tlačítkem na ikonu v docku">
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

      <section id="start">
        <Surface>
          <Section title="Start" description="Vyhledávání a všechny aplikace. Otevřeš ho kliknutím na logo v horní liště.">
            <div>
              <SettingRow label="Zapnuto">
                <Toggle label="Start" checked={s.start.enabled} onchange={(v) => update((x) => (x.start.enabled = v))} />
              </SettingRow>
              <SettingRow
                label="Otevírat klávesou Windows"
                hint="Samotný stisk klávesy Windows otevře tento Start místo Startu Windows. Zkratky (Win+E, Win+L…) fungují dál. Když je v popředí aplikace spuštěná jako správce, otevře se Start Windows."
              >
                <Toggle label="Otevírat klávesou Windows" checked={s.start.replaceWinKey} disabled={!s.start.enabled} onchange={(v) => update((x) => (x.start.replaceWinKey = v))} />
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="island">
        <Surface>
          <Section title="Dynamic Island" description="Právě hrající hudba a hlasitost nahoře uprostřed">
            <div>
              <SettingRow label="Zapnuto">
                <Toggle label="Dynamic Island" checked={s.island.enabled} onchange={(v) => update((x) => (x.island.enabled = v))} />
              </SettingRow>
              <SettingRow label="Ukazovat změnu hlasitosti">
                <Toggle label="Ukazovat změnu hlasitosti" checked={s.island.showVolume} onchange={(v) => update((x) => (x.island.showVolume = v))} />
              </SettingRow>
              <SettingRow
                label="Oznámení"
                hint="Číst oznámení ostatních aplikací Windows dovolí jen aplikacím nainstalovaným jako balíček (MSIX). Better Windows zatím balíček není, proto oznámení v ostrůvku nejsou."
              >
                <span class="muted small">nedostupné</span>
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="lock">
        <Surface>
          <Section
            title="Zamykací obrazovka"
            description="Vlastní obrazovka s hodinami, hudbou a počasím, která se ukáže PŘED zámkem Windows. Otevřeš ji zkratkou Win+Shift+L nebo tlačítkem Zamknout ve Startu."
          >
            <div>
              <SettingRow label="Zapnuto">
                <Toggle label="Zamykací obrazovka" checked={s.lock.enabled} onchange={(v) => update((x) => (x.lock.enabled = v))} />
              </SettingRow>
              <SettingRow label="Po odemknutí zamknout Windows" hint="Doporučeno. Naše obrazovka nic nechrání (jde obejít Ctrl+Alt+Del); heslo, PIN nebo Windows Hello chce až zámek Windows.">
                <Toggle label="Po odemknutí zamknout Windows" checked={s.lock.requireWindowsLock} onchange={(v) => update((x) => (x.lock.requireWindowsLock = v))} />
              </SettingRow>
              <SettingRow label="Ukázat po nečinnosti" hint="Ne během hudby, videa a her přes celou obrazovku">
                <Slider label="Ukázat po nečinnosti" min={0} max={60} step={1} value={s.lock.idleMinutes} format={(v) => (v === 0 ? 'nikdy' : `${v} min`)} onchange={(v) => update((x) => (x.lock.idleMinutes = v))} />
              </SettingRow>
              <SettingRow label="Vzhled" hint="Styl hodin, umístění, co se ukazuje, rozmazání tapety – s živým náhledem. Náhled Windows nezamkne.">
                <Button onclick={() => send('lock.open', { editing: true })}><Icon name="pencil" size={14} /> Upravit</Button>
                <Button onclick={() => send('lock.open', { editing: false, preview: true })}><Icon name="lock" size={14} /> Náhled</Button>
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="topbar">
        <Surface>
          <Section title="Horní lišta">
            <div>
              <SettingRow label="Právě hraje" hint="Mini přehrávač vedle názvu aplikace">
                <Toggle label="Právě hraje" checked={s.topBar.showNowPlaying} onchange={(v) => update((x) => (x.topBar.showNowPlaying = v))} />
              </SettingRow>
              <SettingRow label="Jazyk klávesnice">
                <Toggle label="Jazyk klávesnice" checked={s.topBar.showLanguage} onchange={(v) => update((x) => (x.topBar.showLanguage = v))} />
              </SettingRow>
              <SettingRow label="Uživatel">
                <Toggle label="Uživatel" checked={s.topBar.showUser} onchange={(v) => update((x) => (x.topBar.showUser = v))} />
              </SettingRow>
              <SettingRow label="Tlačítko Ukončit">
                <Toggle label="Tlačítko Ukončit" checked={s.topBar.showExit} onchange={(v) => update((x) => (x.topBar.showExit = v))} />
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="weather">
        <Surface>
          <Section title="Počasí" description="Open-Meteo, bez účtu a API klíče">
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
    font-weight: var(--bw-weight-medium);
    padding: 0 var(--bw-space-2) var(--bw-space-4);
  }
  nav a {
    padding: 7px var(--bw-space-3);
    border-radius: var(--bw-radius-sm);
    color: inherit;
    text-decoration: none;
  }
  nav a {
    color: var(--bw-fg-muted);
    transition: background-color var(--bw-duration-fast) var(--bw-ease), color var(--bw-duration-fast) var(--bw-ease);
  }
  nav a:hover {
    background: var(--bw-hover);
    color: var(--bw-fg);
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
    color: var(--bw-negative);
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
  .small {
    font-size: var(--bw-text-sm);
  }
  .presets {
    display: flex;
    flex-wrap: wrap;
    gap: var(--bw-space-2);
    padding-bottom: var(--bw-space-3);
  }
  .preset {
    display: inline-flex;
    align-items: center;
    gap: var(--bw-space-2);
    height: 32px;
    padding: 0 var(--bw-space-3);
    border-radius: var(--bw-radius-md);
    border: 1px solid var(--bw-border);
    background: var(--bw-surface);
    font-size: var(--bw-text-sm);
  }
  .preset.active {
    border-color: var(--bw-accent);
  }
  .swatch {
    width: 12px;
    height: 12px;
    border-radius: 50%;
  }
  .color {
    width: 44px;
    height: 30px;
    padding: 0;
    border: 1px solid var(--bw-border);
    border-radius: var(--bw-radius-sm);
    background: none;
  }
  .code {
    display: flex;
    gap: var(--bw-space-2);
    padding-bottom: var(--bw-space-3);
  }
  .code input,
  .import {
    flex: 1;
    min-width: 0;
    height: 32px;
    padding: 0 10px;
    border-radius: var(--bw-radius-md);
    border: 1px solid var(--bw-border);
    background: var(--bw-surface);
    color: inherit;
    font-family: var(--bw-font-mono);
    font-size: var(--bw-text-sm);
    user-select: text;
  }
  .import {
    width: 260px;
    flex: none;
  }
  .message {
    margin: 0;
    color: var(--bw-positive);
    font-size: var(--bw-text-sm);
  }
  .message.error {
    color: var(--bw-negative);
  }
</style>
