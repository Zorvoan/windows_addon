<!-- Posuvník s hodnotou. onchange se volá při puštění (ne při každém pohybu). -->
<script lang="ts">
  interface Props {
    value: number;
    min: number;
    max: number;
    step?: number;
    label: string;
    format?: (v: number) => string;
    onchange: (value: number) => void;
  }
  let { value, min, max, step = 1, label, format = (v) => String(v), onchange }: Props = $props();
  let live = $state(0);
  $effect(() => {
    live = value;
  });
</script>

<span class="slider">
  <input
    type="range"
    {min}
    {max}
    {step}
    aria-label={label}
    value={live}
    oninput={(e) => (live = Number(e.currentTarget.value))}
    onchange={(e) => onchange(Number(e.currentTarget.value))}
  />
  <span class="value">{format(live)}</span>
</span>

<style>
  .slider {
    display: inline-flex;
    align-items: center;
    gap: var(--bw-space-3);
  }
  input {
    width: 180px;
    accent-color: var(--bw-accent);
  }
  .value {
    min-width: 44px;
    text-align: right;
    font-variant-numeric: tabular-nums;
    color: var(--bw-fg-muted);
  }
</style>
