<!--
  Vizualizér zvuku z PC (data z C#: WASAPI loopback + FFT). Kreslí na canvas.
  Scény: "dots" (sloupce teček dole), "wave" (vlna), "ring" (kruh uprostřed).
  Když je ticho, animace se zastaví (žádná zbytečná zátěž).
-->
<script lang="ts">
  import { onMount } from 'svelte';
  import { getBins, onFrame } from '$lib/stores/visualizer';
  import type { VisualizerScene } from '$lib/bridge/messages';

  interface Props {
    scene: VisualizerScene;
  }

  let { scene }: Props = $props();
  let canvas: HTMLCanvasElement | undefined = $state();
  let shown: number[] = [];
  let raf = 0;
  let color = '255 255 255';

  function readColor() {
    const dark = matchMedia('(prefers-color-scheme: dark)').matches;
    color = dark ? '245 245 247' : '25 25 30';
  }

  function wake() {
    if (!raf) raf = requestAnimationFrame(draw);
  }

  function sample(values: number[], t: number): number {
    // t 0..1 -> plynulá hodnota mezi pásmy
    if (values.length === 0) return 0;
    const x = t * (values.length - 1);
    const i = Math.floor(x);
    const f = x - i;
    return (values[i] ?? 0) * (1 - f) + (values[Math.min(i + 1, values.length - 1)] ?? 0) * f;
  }

  function draw() {
    raf = 0;
    if (!canvas) return;
    const target = getBins();
    if (shown.length !== target.length) shown = target.map(() => 0);
    let energy = 0;
    for (let i = 0; i < target.length; i++) {
      shown[i] = (shown[i] ?? 0) + ((target[i] ?? 0) - (shown[i] ?? 0)) * 0.35;
      energy += shown[i] ?? 0;
    }

    const dpr = window.devicePixelRatio || 1;
    const w = canvas.clientWidth;
    const h = canvas.clientHeight;
    if (canvas.width !== Math.round(w * dpr) || canvas.height !== Math.round(h * dpr)) {
      canvas.width = Math.round(w * dpr);
      canvas.height = Math.round(h * dpr);
    }
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.clearRect(0, 0, w, h);

    if (scene === 'dots') drawDots(ctx, w, h);
    else if (scene === 'wave') drawWave(ctx, w, h);
    else drawRing(ctx, w, h);

    // Dokud něco hraje nebo doznívá, kreslit dál.
    if (energy > 0.01 || target.some((v) => v > 0)) raf = requestAnimationFrame(draw);
  }

  /** Zrcadlově od středu: basy uprostřed, výšky po stranách. */
  function mirrored(t: number): number {
    return sample(shown, Math.abs(t - 0.5) * 2);
  }

  function drawDots(ctx: CanvasRenderingContext2D, w: number, h: number) {
    const cell = 14;
    const r = 2.6;
    const cols = Math.floor(w / cell);
    const rows = Math.floor(h / cell);
    const offsetX = (w - cols * cell) / 2 + cell / 2;
    for (let c = 0; c < cols; c++) {
      const v = mirrored(cols > 1 ? c / (cols - 1) : 0.5);
      const lit = Math.round(v * rows);
      for (let row = 0; row < rows; row++) {
        const y = h - row * cell - cell / 2;
        const on = row < lit;
        ctx.fillStyle = on ? `rgb(${color} / ${row === lit - 1 ? 0.95 : 0.7})` : `rgb(${color} / 0.07)`;
        ctx.beginPath();
        ctx.arc(offsetX + c * cell, y, on ? r : r * 0.8, 0, Math.PI * 2);
        ctx.fill();
      }
    }
  }

  function drawWave(ctx: CanvasRenderingContext2D, w: number, h: number) {
    const points = 96;
    const base = h * 0.75;
    ctx.beginPath();
    for (let i = 0; i <= points; i++) {
      const t = i / points;
      const x = t * w;
      const y = base - mirrored(t) * h * 0.65 * Math.sin(Math.PI * t) ** 0.5;
      if (i === 0) ctx.moveTo(x, y);
      else ctx.lineTo(x, y);
    }
    ctx.strokeStyle = `rgb(${color} / 0.85)`;
    ctx.lineWidth = 2;
    ctx.stroke();
    ctx.lineTo(w, h);
    ctx.lineTo(0, h);
    ctx.closePath();
    const g = ctx.createLinearGradient(0, base - h * 0.6, 0, h);
    g.addColorStop(0, `rgb(${color} / 0.22)`);
    g.addColorStop(1, `rgb(${color} / 0)`);
    ctx.fillStyle = g;
    ctx.fill();
  }

  function drawRing(ctx: CanvasRenderingContext2D, w: number, h: number) {
    const cx = w / 2;
    const cy = h / 2;
    const radius = Math.min(w, h) * 0.22;
    const bars = 120;
    ctx.lineCap = 'round';
    for (let i = 0; i < bars; i++) {
      const t = i / bars;
      const v = sample(shown, Math.abs(t - 0.5) * 2);
      const angle = t * Math.PI * 2 - Math.PI / 2;
      const len = 4 + v * radius * 0.7;
      ctx.strokeStyle = `rgb(${color} / ${0.25 + v * 0.7})`;
      ctx.lineWidth = 3;
      ctx.beginPath();
      ctx.moveTo(cx + Math.cos(angle) * radius, cy + Math.sin(angle) * radius);
      ctx.lineTo(cx + Math.cos(angle) * (radius + len), cy + Math.sin(angle) * (radius + len));
      ctx.stroke();
    }
  }

  onMount(() => {
    readColor();
    const media = matchMedia('(prefers-color-scheme: dark)');
    const onScheme = () => {
      readColor();
      wake();
    };
    media.addEventListener('change', onScheme);
    const off = onFrame(wake);
    const onResize = () => wake();
    window.addEventListener('resize', onResize);
    wake();
    return () => {
      off();
      media.removeEventListener('change', onScheme);
      window.removeEventListener('resize', onResize);
      cancelAnimationFrame(raf);
    };
  });

  $effect(() => {
    void scene;
    wake();
  });
</script>

<canvas bind:this={canvas} class="viz {scene}" aria-hidden="true"></canvas>

<style>
  .viz {
    position: absolute;
    left: 0;
    width: 100%; /* canvas se přes left/right neroztáhne, potřebuje šířku */
    pointer-events: none;
  }
  .dots {
    bottom: 0;
    height: 26vh;
  }
  .wave {
    bottom: 0;
    height: 30vh;
  }
  .ring {
    top: 0;
    bottom: 0;
    height: 100%;
  }
</style>
