<!--
  Widgety na ploše. Pozice jsou v nastavení (settings.desktop.widgets).
  "Upravit widgety": přetahování (mřížka 8 px), přidání, odebrání; "Hotovo" uloží.
-->
<script lang="ts">
  import Surface from '$lib/components/Surface.svelte';
  import Icon from '$lib/components/Icon.svelte';
  import ClockWidget from '$lib/widgets/ClockWidget.svelte';
  import CalendarWidget from '$lib/widgets/CalendarWidget.svelte';
  import WeatherWidget from '$lib/widgets/WeatherWidget.svelte';
  import RemindersWidget from '$lib/widgets/RemindersWidget.svelte';
  import BatteryWidget from '$lib/widgets/BatteryWidget.svelte';
  import { host } from '$lib/stores/host.svelte';
  import { send } from '$lib/bridge/bridge';
  import { sizeOf, widgetInfo } from '$lib/widgets/registry';
  import type { WidgetPlacement, WidgetType } from '$lib/bridge/messages';

  let editing = $state(false);
  let draft = $state<WidgetPlacement[]>([]);
  let placements = $derived(editing ? draft : (host.settings?.desktop.widgets ?? []));
  let drag: { id: string; startX: number; startY: number; origX: number; origY: number } | null = null;
  const types = Object.keys(widgetInfo) as WidgetType[];

  function startEdit() {
    draft = $state.snapshot(host.settings?.desktop.widgets ?? []);
    editing = true;
  }

  function finish() {
    if (host.settings) {
      const settings = $state.snapshot(host.settings);
      settings.desktop.widgets = $state.snapshot(draft);
      send('settings.update', { settings });
    }
    editing = false;
  }

  function add(type: WidgetType) {
    const offset = (draft.length % 6) * 24;
    draft = [...draft, { id: `${type}-${Date.now()}`, type, x: 420 + offset, y: 96 + offset }];
  }

  function remove(id: string) {
    draft = draft.filter((w) => w.id !== id);
  }

  const snap = (v: number) => Math.max(0, Math.round(v / 8) * 8);

  function down(e: PointerEvent, w: WidgetPlacement) {
    if (!editing || (e.target as HTMLElement).closest('button')) return;
    (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
    drag = { id: w.id, startX: e.clientX, startY: e.clientY, origX: w.x, origY: w.y };
  }

  function move(e: PointerEvent) {
    if (!drag) return;
    const d = drag;
    draft = draft.map((w) =>
      w.id === d.id
        ? {
            ...w,
            x: Math.min(snap(d.origX + e.clientX - d.startX), window.innerWidth - 80),
            y: Math.min(snap(d.origY + e.clientY - d.startY), window.innerHeight - 80),
          }
        : w,
    );
  }

  function up() {
    drag = null;
  }
</script>

<div class="toolbar">
  {#if editing}
    <span class="label">Přidat:</span>
    {#each types as t (t)}
      <button type="button" class="chip" onclick={() => add(t)}><Icon name="plus" size={11} /> {widgetInfo[t].title}</button>
    {/each}
    <button type="button" class="chip primary" onclick={finish}>Hotovo</button>
    <button type="button" class="chip" onclick={() => (editing = false)}>Zrušit</button>
  {:else}
    <span class="label">Widgety</span>
    <button type="button" class="chip" onclick={startEdit}><Icon name="pencil" size={11} /> Upravit widgety</button>
  {/if}
</div>

{#each placements as w (w.id)}
  {@const size = sizeOf(w.type)}
  <div
    class="widget"
    class:editing
    style:left="{w.x}px"
    style:top="{w.y}px"
    style:width="{size.width}px"
    style:height="{size.height}px"
    role="group"
    aria-label={widgetInfo[w.type].title}
    onpointerdown={(e) => down(e, w)}
    onpointermove={move}
    onpointerup={up}
    onpointercancel={up}
  >
    <Surface variant="glass" padding={w.type === 'clock' || w.type === 'calendar' ? 'sm' : 'md'}>
      <div class="inner" style:height="{size.height - (w.type === 'clock' || w.type === 'calendar' ? 18 : 34)}px">
        {#if w.type === 'clock'}
          <ClockWidget />
        {:else if w.type === 'calendar'}
          <CalendarWidget />
        {:else if w.type === 'weather'}
          <WeatherWidget />
        {:else if w.type === 'reminders'}
          <RemindersWidget />
        {:else if w.type === 'battery'}
          <BatteryWidget />
        {/if}
      </div>
    </Surface>
    {#if editing}
      <button type="button" class="remove" aria-label="Odebrat widget" onclick={() => remove(w.id)}><Icon name="close" size={10} strokeWidth={2.4} /></button>
    {/if}
  </div>
{/each}

<style>
  .toolbar {
    position: absolute;
    left: 24px;
    top: 40px;
    display: flex;
    align-items: center;
    gap: var(--bw-space-2);
    flex-wrap: wrap;
    max-width: 60vw;
    color: var(--bw-fg);
    font-size: var(--bw-text-xs);
  }
  .label {
    font-weight: var(--bw-weight-medium);
    margin-right: var(--bw-space-1);
    text-shadow: 0 1px 4px rgb(0 0 0 / 0.25);
  }
  .chip {
    display: inline-flex;
    align-items: center;
    gap: var(--bw-space-1);
    height: 24px;
    padding: 0 var(--bw-space-2);
    border-radius: var(--bw-radius-sm);
    border: 1px solid var(--bw-glass-border);
    background: var(--bw-glass-bg);
    backdrop-filter: blur(12px);
    color: inherit;
    font: inherit;
    font-size: var(--bw-text-xs);
  }
  .chip:hover {
    background: var(--bw-hover);
  }
  .chip.primary {
    background: var(--bw-accent);
    border-color: transparent;
    color: var(--bw-accent-fg);
  }
  .widget {
    position: absolute;
    color: var(--bw-fg);
  }
  .widget > :global(.surface) {
    height: 100%;
  }
  .inner {
    overflow: hidden;
  }
  .editing {
    cursor: grab;
    outline: 1px dashed var(--bw-border-strong);
    outline-offset: 4px;
    border-radius: var(--bw-radius-card);
  }
  .editing:active {
    cursor: grabbing;
  }
  .remove {
    position: absolute;
    left: -7px;
    top: -7px;
    display: grid;
    place-items: center;
    width: 20px;
    height: 20px;
    padding: 0;
    border-radius: 50%;
    border: 0;
    background: #232325;
    color: #ededed;
    box-shadow: var(--bw-shadow-sm);
  }
</style>
