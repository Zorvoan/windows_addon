<!-- Přepínač zap/vyp. -->
<script lang="ts">
  interface Props {
    checked: boolean;
    label: string;
    disabled?: boolean;
    onchange: (checked: boolean) => void;
  }
  let { checked, label, disabled = false, onchange }: Props = $props();
</script>

<button
  type="button"
  role="switch"
  aria-checked={checked}
  aria-label={label}
  class:on={checked}
  {disabled}
  onclick={() => onchange(!checked)}
>
  <span class="knob"></span>
</button>

<style>
  button {
    position: relative;
    flex: none;
    width: 40px;
    height: 22px;
    padding: 0;
    border-radius: var(--bw-radius-pill);
    border: 1px solid var(--bw-border);
    background: var(--bw-hover);
    transition: background-color var(--bw-duration) var(--bw-ease);
  }
  button.on {
    background: var(--bw-accent);
    border-color: transparent;
  }
  button:disabled {
    opacity: 0.45;
  }
  .knob {
    position: absolute;
    top: 2px;
    left: 2px;
    width: 16px;
    height: 16px;
    border-radius: 50%;
    background: #fff;
    box-shadow: var(--bw-shadow-sm);
    transition: transform var(--bw-duration) var(--bw-ease);
  }
  .on .knob {
    transform: translateX(18px);
  }
</style>
