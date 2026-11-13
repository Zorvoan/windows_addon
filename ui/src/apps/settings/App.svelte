<!--
  Okno nastavení. Každá změna se hned uloží (zpráva settings.update) a C# ji použije.
-->
<script lang="ts">
  import { t } from '$lib/i18n';
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
    importMessage = p.ok ? { ok: true, text: t('Motiv použit') } : { ok: false, text: p.error ?? t('Kód nejde použít.') };
    if (p.ok) importCode = '';
  });

  const sections = [
    { id: 'appearance', title: t('Vzhled') },
    { id: 'theme', title: t('Motiv') },
    { id: 'performance', title: t('Výkon') },
    { id: 'desktop', title: t('Plocha') },
    { id: 'visualizer', title: t('Vizualizér') },
    { id: 'dock', title: t('Dock') },
    { id: 'start', title: t('Start') },
    { id: 'island', title: t('Dynamic Island') },
    { id: 'lock', title: t('Zamykací obrazovka') },
    { id: 'topbar', title: t('Horní lišta') },
    { id: 'weather', title: t('Počasí') },
    { id: 'taskbar', title: t('Hlavní panel') },
    { id: 'about', title: t('O aplikaci') },
  ];

  const backdropOptions: { value: BackdropMode; label: string }[] = [
    { value: 'systemAcrylic', label: t('Systémové') },
    { value: 'accentAcrylic', label: t('Vlastní tón') },
    { value: 'none', label: t('Žádné') },
  ];

  const modeLabels: Record<string, string> = {
    'embed-raised': t('Vloženo do plochy (Windows 11 24H2+)'),
    'embed-classic': t('Vloženo do plochy (klasicky)'),
    bottom: t('Samostatné okno vespod (HWND_BOTTOM)'),
    failed: t('Selhalo'),
    off: t('Vypnuto'),
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
      <p class="muted">{t('Načítám nastavení…')}</p>
    {:else}
      <section id="appearance">
        <Surface>
          <Section title={t('Vzhled')} description={t('Světlý a tmavý režim se řídí nastavením Windows')}>
            <div>
              <SettingRow label={t('Rozmazání horní lišty')} hint={t('Systémové je rozmazání Windows 11, vlastní tón je průhlednější')}>
                <Segmented label={t('Rozmazání horní lišty')} value={s.appearance.topBarBackdrop} options={backdropOptions} onchange={(v) => update((x) => (x.appearance.topBarBackdrop = v))} />
              </SettingRow>
              <SettingRow label={t('Rozmazání docku')}>
                <Segmented label={t('Rozmazání docku')} value={s.appearance.dockBackdrop} options={backdropOptions} onchange={(v) => update((x) => (x.appearance.dockBackdrop = v))} />
              </SettingRow>
              <SettingRow label={t('Omezit animace')} hint={t('Vypne zvětšování s doběhem a další plynulé přechody')}>
                <Toggle label={t('Omezit animace')} checked={s.appearance.reduceMotion} onchange={(v) => update((x) => (x.appearance.reduceMotion = v))} />
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="theme">
        <Surface>
          <Section title={t('Motiv')} description={t('Barvy, průhlednost, rozmazání a zaoblení. Nemění tón systémového rozmazání (ten určuje Windows) ani nativní kontextovou nabídku.')}>
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
                    {t(preset.name)}
                  </button>
                {/each}
              </div>
              <SettingRow label={t('Barva zvýraznění podle Windows')} hint={t('Přizpůsobení › Barvy › Barva zvýraznění')}>
                <Toggle
                  label={t('Barva zvýraznění podle Windows')}
                  checked={s.appearance.style.useSystemAccent}
                  onchange={(v) => update((x) => (x.appearance.style = { ...x.appearance.style, useSystemAccent: v }))}
                />
              </SettingRow>
              <SettingRow label={t('Vlastní barva zvýraznění')}>
                <input
                  disabled={s.appearance.style.useSystemAccent}
                  type="color"
                  class="color"
                  aria-label={t('Barva zvýraznění')}
                  value={s.appearance.style.accent}
                  onchange={(e) => update((x) => (x.appearance.style = { ...x.appearance.style, accent: e.currentTarget.value, useSystemAccent: false, name: 'Vlastní' }))}
                />
              </SettingRow>
              <SettingRow label={t('Zaoblení karet')}>
                <Slider label={t('Zaoblení karet')} min={0} max={40} value={s.appearance.style.radiusCard} format={(v) => `${v} px`} onchange={(v) => update((x) => (x.appearance.style = { ...x.appearance.style, radiusCard: v, name: 'Vlastní' }))} />
              </SettingRow>
              <SettingRow label={t('Rozmazání widgetů')}>
                <Slider label={t('Rozmazání widgetů')} min={0} max={60} value={s.appearance.style.glassBlur} format={(v) => `${v} px`} onchange={(v) => update((x) => (x.appearance.style = { ...x.appearance.style, glassBlur: v, name: 'Vlastní' }))} />
              </SettingRow>
              <SettingRow label={t('Neprůhlednost widgetů')}>
                <Slider label={t('Neprůhlednost widgetů')} min={0} max={1} step={0.05} value={s.appearance.style.glassOpacity} format={(v) => `${Math.round(v * 100)} %`} onchange={(v) => update((x) => (x.appearance.style = { ...x.appearance.style, glassOpacity: v, name: 'Vlastní' }))} />
              </SettingRow>
              <SettingRow label={t('Tón horní lišty')} hint={t('Přidává barvu přes systémové rozmazání')}>
                <Slider label={t('Tón horní lišty')} min={0} max={0.8} step={0.02} value={s.appearance.style.barTint} format={(v) => `${Math.round(v * 100)} %`} onchange={(v) => update((x) => (x.appearance.style = { ...x.appearance.style, barTint: v, name: 'Vlastní' }))} />
              </SettingRow>
              <SettingRow label={t('Tón docku')}>
                <Slider label={t('Tón docku')} min={0} max={0.8} step={0.02} value={s.appearance.style.dockTint} format={(v) => `${Math.round(v * 100)} %`} onchange={(v) => update((x) => (x.appearance.style = { ...x.appearance.style, dockTint: v, name: 'Vlastní' }))} />
              </SettingRow>
              <SettingRow label={t('Sdílet motiv')} hint={t('Kód obsahuje motiv a velikost ikon docku. Pošli ho komukoli s Better Windows.')}>
                <Button onclick={() => send('theme.export', {})}>{t('Vytvořit kód')}</Button>
              </SettingRow>
              {#if themeCode}
                <div class="code">
                  <input type="text" readonly value={themeCode} aria-label={t('Kód motivu')} onfocus={(e) => e.currentTarget.select()} />
                  <Button onclick={() => navigator.clipboard?.writeText(themeCode)}>{t('Kopírovat')}</Button>
                </div>
              {/if}
              <SettingRow label={t('Použít kód motivu')}>
                <input class="import" type="text" placeholder="BW1.…" aria-label={t('Kód motivu k importu')} bind:value={importCode} />
                <Button variant="primary" disabled={!importCode.trim()} onclick={() => send('theme.import', { code: importCode })}>{t('Použít')}</Button>
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
          <Section title={t('Výkon')} description={t('Ztlumí animace a pozastaví vizualizér, když na nich nezáleží nebo když šetříš baterii')}>
            <div>
              <SettingRow label={t('Režim')}>
                <Segmented
                  label={t('Režim výkonu')}
                  value={s.performance.mode}
                  options={[
                    { value: 'auto', label: t('Automaticky') },
                    { value: 'alwaysReduced', label: t('Vždy úsporně') },
                    { value: 'neverReduced', label: t('Vždy plné animace') },
                  ]}
                  onchange={(v) => update((x) => (x.performance.mode = v))}
                />
              </SettingRow>
              <SettingRow label={t('Ztlumit na baterii')}>
                <Toggle label={t('Ztlumit na baterii')} checked={s.performance.reduceOnBattery} disabled={s.performance.mode !== 'auto'} onchange={(v) => update((x) => (x.performance.reduceOnBattery = v))} />
              </SettingRow>
              <SettingRow label={t('Ztlumit při aplikaci přes celou obrazovku')} hint={t('Hry, videa, prezentace')}>
                <Toggle label={t('Ztlumit při celé obrazovce')} checked={s.performance.reduceOnFullscreen} disabled={s.performance.mode !== 'auto'} onchange={(v) => update((x) => (x.performance.reduceOnFullscreen = v))} />
              </SettingRow>
              <SettingRow label={t('Teď')}>
                <span class="status">{host.performance.reduced ? t('Ztlumené animace') : t('Plné animace')}</span>
                <span class="muted small">{host.performance.reason}</span>
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="desktop">
        <Surface>
          <Section title={t('Plocha')} description={t('Vrstva pod všemi aplikacemi: tapeta, widgety a vizualizér')}>
            <div>
              <SettingRow label={t('Vrstva plochy')}>
                <Toggle label={t('Vrstva plochy')} checked={s.desktop.enabled} onchange={(v) => update((x) => (x.desktop.enabled = v))} />
              </SettingRow>
              <SettingRow label={t('Způsob umístění')} hint={t('Automaticky zkusí vložení do plochy Windows, při neúspěchu okno vespod. Okno vespod nedostává klávesnici.')}>
                <Segmented
                  label={t('Způsob umístění')}
                  value={s.desktop.mode}
                  options={[
                    { value: 'auto', label: t('Automaticky') },
                    { value: 'embed', label: t('Vložit do plochy') },
                    { value: 'bottom', label: t('Okno vespod') },
                  ]}
                  onchange={(v) => update((x) => (x.desktop.mode = v))}
                />
              </SettingRow>
              <SettingRow label={t('Stav')} hint={host.desktop?.detail ?? undefined}>
                <span class="status">{host.desktop ? (modeLabels[host.desktop.mode] ?? host.desktop.mode) : '–'}</span>
              </SettingRow>
              <SettingRow label={t('Skrýt ikony plochy')} hint={t('Jen když je vrstva vložená do plochy. Bez skrytí ikon nereagují widgety na myš.')}>
                <Toggle label={t('Skrýt ikony plochy')} checked={s.desktop.hideIcons} onchange={(v) => update((x) => (x.desktop.hideIcons = v))} />
              </SettingRow>
              <SettingRow label={t('Tapeta')} hint={s.desktop.wallpaperPath ?? t('Aktuální tapeta Windows')}>
                <Button onclick={() => action('pickWallpaper')}><Icon name="image" size={14} /> {t('Vybrat obrázek…')}</Button>
                {#if s.desktop.wallpaperPath}
                  <Button onclick={() => action('clearWallpaper')}>{t('Použít tapetu Windows')}</Button>
                {/if}
              </SettingRow>
              <SettingRow label={t('Widgety')}>
                <Toggle label={t('Widgety')} checked={s.desktop.showWidgets} onchange={(v) => update((x) => (x.desktop.showWidgets = v))} />
              </SettingRow>
              <SettingRow label={t('Testovací karta')} hint={t('Diagnostika vrstvy plochy (režim, test myši a klávesnice)')}>
                <Toggle label={t('Testovací karta')} checked={s.desktop.showTestCard} onchange={(v) => update((x) => (x.desktop.showTestCard = v))} />
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="visualizer">
        <Surface>
          <Section title={t('Vizualizér')} description={t('Reaguje na zvuk, který hraje z počítače (ne na mikrofon)')}>
            <div>
              <SettingRow label={t('Zapnuto')}>
                <Toggle label={t('Vizualizér')} checked={s.desktop.visualizer.enabled} onchange={(v) => update((x) => (x.desktop.visualizer.enabled = v))} />
              </SettingRow>
              <SettingRow label={t('Scéna')}>
                <Segmented
                  label={t('Scéna')}
                  value={s.desktop.visualizer.scene}
                  options={[
                    { value: 'dots', label: t('Tečky') },
                    { value: 'wave', label: t('Vlna') },
                    { value: 'ring', label: t('Kruh') },
                  ]}
                  onchange={(v) => update((x) => (x.desktop.visualizer.scene = v))}
                />
              </SettingRow>
              <SettingRow label={t('Citlivost')}>
                <Slider label={t('Citlivost')} min={0.2} max={3} step={0.1} value={s.desktop.visualizer.sensitivity} format={(v) => `${v.toFixed(1)}×`} onchange={(v) => update((x) => (x.desktop.visualizer.sensitivity = v))} />
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="dock">
        <Surface>
          <Section title={t('Dock')}>
            <div>
              <SettingRow label={t('Zapnuto')}>
                <Toggle label={t('Dock')} checked={s.dock.enabled} onchange={(v) => update((x) => (x.dock.enabled = v))} />
              </SettingRow>
              <SettingRow label={t('Velikost ikon')}>
                <Slider label={t('Velikost ikon')} min={32} max={96} step={2} value={s.dock.iconSize} format={(v) => `${v} px`} onchange={(v) => update((x) => (x.dock.iconSize = v))} />
              </SettingRow>
              <SettingRow label={t('Zvětšování při najetí')}>
                <Toggle label={t('Zvětšování')} checked={s.dock.magnification} onchange={(v) => update((x) => (x.dock.magnification = v))} />
              </SettingRow>
              <SettingRow label={t('Náhledy oken')} hint={t('Po najetí na ikonu spuštěné aplikace')}>
                <Toggle label={t('Náhledy oken')} checked={s.dock.previews} onchange={(v) => update((x) => (x.dock.previews = v))} />
              </SettingRow>
              <SettingRow label={t('Launchpad a Koš')} hint={t('Ikona Startu na začátku docku a koš na konci')}>
                <Toggle label={t('Launchpad')} checked={s.dock.showLaunchpad} onchange={(v) => update((x) => (x.dock.showLaunchpad = v))} />
                <Toggle label={t('Koš')} checked={s.dock.showTrash} onchange={(v) => update((x) => (x.dock.showTrash = v))} />
              </SettingRow>
              <SettingRow label={t('Největší zvětšení')}>
                <Slider label={t('Největší zvětšení')} min={1} max={2.5} step={0.1} value={s.dock.maxScale} format={(v) => `${v.toFixed(1)}×`} onchange={(v) => update((x) => (x.dock.maxScale = v))} />
              </SettingRow>
              <SettingRow label={t('Připnuté aplikace')} hint={t('Připnout jde pravým tlačítkem na ikonu v docku')}>
                <span></span>
              </SettingRow>
              <ul class="pinned">
                {#each s.dock.pinned as p (p.appId)}
                  <li>
                    {#if host.icons[p.appId]}<img src={host.icons[p.appId]} alt="" />{:else}<span class="ph">{p.name.charAt(0)}</span>{/if}
                    <span class="name" title={p.appId}>{p.name}</span>
                    <Button onclick={() => action('unpin', p.appId)}>{t('Odepnout')}</Button>
                  </li>
                {:else}
                  <li class="muted">{t('Nic není připnuté.')}</li>
                {/each}
              </ul>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="start">
        <Surface>
          <Section title={t('Start')} description={t('Vyhledávání a všechny aplikace. Otevřeš ho kliknutím na logo v horní liště.')}>
            <div>
              <SettingRow label={t('Zapnuto')}>
                <Toggle label={t('Start')} checked={s.start.enabled} onchange={(v) => update((x) => (x.start.enabled = v))} />
              </SettingRow>
              <SettingRow
                label={t('Otevírat klávesou Windows')}
                hint={t('Samotný stisk klávesy Windows otevře tento Start místo Startu Windows. Zkratky (Win+E, Win+L…) fungují dál. Když je v popředí aplikace spuštěná jako správce, otevře se Start Windows.')}
              >
                <Toggle label={t('Otevírat klávesou Windows')} checked={s.start.replaceWinKey} disabled={!s.start.enabled} onchange={(v) => update((x) => (x.start.replaceWinKey = v))} />
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="island">
        <Surface>
          <Section title={t('Dynamic Island')} description={t('Právě hrající hudba a hlasitost nahoře uprostřed')}>
            <div>
              <SettingRow label={t('Zapnuto')}>
                <Toggle label={t('Dynamic Island')} checked={s.island.enabled} onchange={(v) => update((x) => (x.island.enabled = v))} />
              </SettingRow>
              <SettingRow label={t('Ukazovat změnu hlasitosti')}>
                <Toggle label={t('Ukazovat změnu hlasitosti')} checked={s.island.showVolume} onchange={(v) => update((x) => (x.island.showVolume = v))} />
              </SettingRow>
              <SettingRow
                label={t('Oznámení')}
                hint={t('Číst oznámení ostatních aplikací Windows dovolí jen aplikacím nainstalovaným jako balíček (MSIX). Better Windows zatím balíček není, proto oznámení v ostrůvku nejsou.')}
              >
                <span class="muted small">{t('nedostupné')}</span>
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="lock">
        <Surface>
          <Section
            title={t('Zamykací obrazovka')}
            description={t('Vlastní obrazovka s hodinami, hudbou a počasím, která se ukáže PŘED zámkem Windows. Otevřeš ji zkratkou Win+Shift+L nebo tlačítkem Zamknout ve Startu.')}
          >
            <div>
              <SettingRow label={t('Zapnuto')}>
                <Toggle label={t('Zamykací obrazovka')} checked={s.lock.enabled} onchange={(v) => update((x) => (x.lock.enabled = v))} />
              </SettingRow>
              <SettingRow label={t('Po odemknutí zamknout Windows')} hint={t('Doporučeno. Naše obrazovka nic nechrání (jde obejít Ctrl+Alt+Del); heslo, PIN nebo Windows Hello chce až zámek Windows.')}>
                <Toggle label={t('Po odemknutí zamknout Windows')} checked={s.lock.requireWindowsLock} onchange={(v) => update((x) => (x.lock.requireWindowsLock = v))} />
              </SettingRow>
              <SettingRow label={t('Ukázat po nečinnosti')} hint={t('Ne během hudby, videa a her přes celou obrazovku')}>
                <Slider label={t('Ukázat po nečinnosti')} min={0} max={60} step={1} value={s.lock.idleMinutes} format={(v) => (v === 0 ? t('nikdy') : `${v} min`)} onchange={(v) => update((x) => (x.lock.idleMinutes = v))} />
              </SettingRow>
              <SettingRow label={t('Vzhled')} hint={t('Styl hodin, umístění, co se ukazuje, rozmazání tapety – s živým náhledem. Náhled Windows nezamkne.')}>
                <Button onclick={() => send('lock.open', { editing: true })}><Icon name="pencil" size={14} /> {t('Upravit')}</Button>
                <Button onclick={() => send('lock.open', { editing: false, preview: true })}><Icon name="lock" size={14} /> {t('Náhled')}</Button>
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="topbar">
        <Surface>
          <Section title={t('Horní lišta')}>
            <div>
              <SettingRow label={t('Právě hraje')} hint={t('Mini přehrávač vedle názvu aplikace')}>
                <Toggle label={t('Právě hraje')} checked={s.topBar.showNowPlaying} onchange={(v) => update((x) => (x.topBar.showNowPlaying = v))} />
              </SettingRow>
              <SettingRow label={t('Jazyk klávesnice')}>
                <Toggle label={t('Jazyk klávesnice')} checked={s.topBar.showLanguage} onchange={(v) => update((x) => (x.topBar.showLanguage = v))} />
              </SettingRow>
              <SettingRow label={t('Uživatel')}>
                <Toggle label={t('Uživatel')} checked={s.topBar.showUser} onchange={(v) => update((x) => (x.topBar.showUser = v))} />
              </SettingRow>
              <SettingRow label={t('Tlačítko Ukončit')}>
                <Toggle label={t('Tlačítko Ukončit')} checked={s.topBar.showExit} onchange={(v) => update((x) => (x.topBar.showExit = v))} />
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="weather">
        <Surface>
          <Section title={t('Počasí')} description={t('Open-Meteo, bez účtu a API klíče')}>
            <div>
              <SettingRow label={t('Zapnuto')}>
                <Toggle label={t('Počasí')} checked={s.weather.enabled} onchange={(v) => update((x) => (x.weather.enabled = v))} />
              </SettingRow>
              <SettingRow
                label={t('Město')}
                hint={s.weather.resolved ? t('Nalezeno: {place}', { place: s.weather.resolved.name + (s.weather.resolved.country ? ', ' + s.weather.resolved.country : '') }) : (host.weather?.error ?? t('Uloží se Enterem'))}
              >
                <TextField label={t('Město')} value={s.weather.city} placeholder={t('např. Praha')} onsubmit={(v) => update((x) => (x.weather.city = v))} />
                <Button onclick={() => action('refreshWeather')}><Icon name="refresh" size={14} /> {t('Obnovit')}</Button>
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="taskbar">
        <Surface>
          <Section title={t('Hlavní panel Windows')}>
            <div>
              <SettingRow
                label={t('Skrýt hlavní panel')}
                hint={t('Vrátí se při ukončení aplikace, při pádu i po zabití procesu. Windows si přitom zapamatuje „automaticky skrývat“ – to aplikace při ukončení také vrací.')}
              >
                {#if confirmTaskbar}
                  <span class="confirm">{t('Opravdu skrýt?')}</span>
                  <Button
                    variant="primary"
                    onclick={() => {
                      confirmTaskbar = false;
                      update((x) => (x.taskbar.hide = true));
                    }}>{t('Skrýt')}</Button
                  >
                  <Button onclick={() => (confirmTaskbar = false)}>{t('Zrušit')}</Button>
                {:else}
                  <Toggle
                    label={t('Skrýt hlavní panel')}
                    checked={s.taskbar.hide}
                    onchange={(v) => {
                      if (v) confirmTaskbar = true;
                      else update((x) => (x.taskbar.hide = false));
                    }}
                  />
                {/if}
              </SettingRow>
              <SettingRow label={t('Nouzová obnova')} hint={t('Ukáže ikony plochy i hlavní panel a vypne jejich skrývání. Totéž je v nabídce ikony v oznamovací oblasti.')}>
                <Button onclick={() => action('restoreSystem')}>{t('Obnovit ikony plochy a hlavní panel')}</Button>
              </SettingRow>
            </div>
          </Section>
        </Surface>
      </section>

      <section id="about">
        <Surface>
          <Section title={t('O aplikaci')}>
            <div>
              <dl class="about">
                <dt>{t('Verze')}</dt>
                <dd>{host.diagnostics?.version ?? '–'}</dd>
                <dt>Windows</dt>
                <dd>{host.diagnostics?.windowsVersion ?? '–'}</dd>
                <dt>{t('Vrstva plochy')}</dt>
                <dd>{host.diagnostics?.desktopMode ?? '–'} {host.diagnostics?.desktopDetail ? `– ${host.diagnostics.desktopDetail}` : ''}</dd>
                <dt>{t('Data')}</dt>
                <dd class="path">{host.diagnostics?.dataDir ?? '–'}</dd>
              </dl>
              <div class="buttons">
                <Button onclick={() => action('openLogs')}><Icon name="folder" size={14} /> {t('Otevřít logy')}</Button>
                <Button onclick={() => action('openDataDir')}><Icon name="folder" size={14} /> {t('Složka s daty')}</Button>
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
    grid-template-columns: 200px minmax(0, 1fr);
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
    min-width: 0;
    align-content: start;
  }
  /* Úzké okno: navigace jako vodorovný pruh nahoře, obsah přes celou šířku. */
  @media (max-width: 760px) {
    .layout {
      grid-template-columns: minmax(0, 1fr);
    }
    nav {
      position: sticky;
      z-index: 1;
      height: auto;
      flex-direction: row;
      align-items: center;
      overflow-x: auto;
      padding: var(--bw-space-2) var(--bw-space-3);
      border-right: 0;
      border-bottom: 1px solid var(--bw-border);
      background: var(--bw-page);
    }
    .brand {
      padding: 0 var(--bw-space-3) 0 var(--bw-space-1);
      flex: none;
    }
    nav a {
      flex: none;
      white-space: nowrap;
    }
    main {
      padding: var(--bw-space-4);
    }
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
