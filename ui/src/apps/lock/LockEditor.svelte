<!-- Panel editoru zamykací obrazovky: změny se hned ukazují v náhledu, "Hotovo" je uloží. -->
<script lang="ts">
  import Segmented from '$lib/components/forms/Segmented.svelte';
  import Toggle from '$lib/components/forms/Toggle.svelte';
  import Slider from '$lib/components/forms/Slider.svelte';
  import Button from '$lib/components/forms/Button.svelte';
  import type { LockSettings } from '$lib/bridge/messages';

  interface Props {
    draft: LockSettings;
    onchange: (draft: LockSettings) => void;
    ondone: () => void;
    oncancel: () => void;
  }

  let { draft, onchange, ondone, oncancel }: Props = $props();
  const set = <K extends keyof LockSettings>(key: K, value: LockSettings[K]) => onchange({ ...draft, [key]: value });
</script>

<!-- Kliky a klávesy uvnitř editoru nesmí "odemknout" obrazovku pod ním. -->
<!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
<aside class="editor" onclick={(e) => e.stopPropagation()} onkeydown={(e) => e.stopPropagation()}>
  <h2>Upravit zamykací obrazovku</h2>

  <span class="label">Styl hodin</span>
  <Segmented
    label="Styl hodin"
    value={draft.clockStyle}
    options={[
      { value: 'large', label: 'Velké' },
      { value: 'thin', label: 'Tenké' },
      { value: 'bold', label: 'Tučné' },
      { value: 'analog', label: 'Ručičky' },
    ]}
    onchange={(v) => set('clockStyle', v)}
  />

  <span class="label">Umístění</span>
  <Segmented
    label="Umístění hodin"
    value={draft.clockPosition}
    options={[
      { value: 'top', label: 'Nahoře' },
      { value: 'center', label: 'Uprostřed' },
    ]}
    onchange={(v) => set('clockPosition', v)}
  />

  <div class="row"><span>Sekundy</span><Toggle label="Sekundy" checked={draft.showSeconds} onchange={(v) => set('showSeconds', v)} /></div>
  <div class="row"><span>Hudba</span><Toggle label="Hudba" checked={draft.showMedia} onchange={(v) => set('showMedia', v)} /></div>
  <div class="row"><span>Počasí</span><Toggle label="Počasí" checked={draft.showWeather} onchange={(v) => set('showWeather', v)} /></div>
  <div class="row"><span>Baterie</span><Toggle label="Baterie" checked={draft.showBattery} onchange={(v) => set('showBattery', v)} /></div>
  <div class="row"><span>Uživatel</span><Toggle label="Uživatel" checked={draft.showUser} onchange={(v) => set('showUser', v)} /></div>

  <span class="label">Rozmazání tapety</span>
  <Slider label="Rozmazání tapety" min={0} max={40} value={draft.wallpaperBlur} format={(v) => `${v} px`} onchange={(v) => set('wallpaperBlur', v)} />
  <span class="label">Ztmavení tapety</span>
  <Slider label="Ztmavení tapety" min={0} max={0.7} step={0.05} value={draft.dim} format={(v) => `${Math.round(v * 100)} %`} onchange={(v) => set('dim', v)} />

  <div class="buttons">
    <Button onclick={oncancel}>Zrušit</Button>
    <Button variant="primary" onclick={ondone}>Hotovo</Button>
  </div>
</aside>

<style>
  .editor {
    position: absolute;
    right: 24px;
    top: 24px;
    bottom: 24px;
    width: 320px;
    overflow-y: auto;
    display: flex;
    flex-direction: column;
    gap: var(--bw-space-2);
    padding: var(--bw-space-5);
    border-radius: var(--bw-radius-card);
    background: var(--bw-surface);
    color: var(--bw-fg);
    box-shadow: var(--bw-shadow-card);
  }
  h2 {
    margin: 0 0 var(--bw-space-2);
    font-size: var(--bw-text-lg);
  }
  .label {
    margin-top: var(--bw-space-2);
    font-size: var(--bw-text-sm);
    color: var(--bw-fg-muted);
  }
  .row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 4px 0;
  }
  .buttons {
    margin-top: auto;
    display: flex;
    justify-content: flex-end;
    gap: var(--bw-space-2);
    padding-top: var(--bw-space-4);
  }
</style>
