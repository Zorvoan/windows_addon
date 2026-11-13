<!--
  Zvukové proužky (6 kapslí) jako v ostrůvku na telefonu: výška podle hudby (spektrum z WASAPI
  loopbacku, ~30 snímků/s), barva podle obalu alba s jemným přechodem.
   - pozastaveno: nízké klidné kapsle,
   - hraje, ale spektrum nechodí (vizualizér nedostupný): klidné vlnění, ať je vidět, že hraje,
   - omezení pohybu: pevný tvar, nic se nehýbe.
  Kreslí se do canvasu ve vlastní smyčce requestAnimationFrame – kvůli proužkům se nepřekresluje UI.
-->
<script lang="ts">
  import { getBins, lastFrameAt, onFrame } from '$lib/stores/visualizer';
  import { motionReduced } from '$lib/motion.svelte';
  import { artPalette, DEFAULT_PALETTE, type ArtPalette } from '$lib/artColor';

  interface Props {
    playing: boolean;
    art?: string | null;
    /** Vrstva je vidět (jinak smyčka stojí). */
    active?: boolean;
    width?: number;
    height?: number;
  }

  let { playing, art = null, active = true, width = 26, height = 16 }: Props = $props();

  const BARS = 6;
  const MIN = 0.22; // klidová výška (podíl výšky)
  // Pásma ze 48 logaritmických pásem 40 Hz–16 kHz: basy vlevo, výšky vpravo; výšky jsou tišší -> zesílení.
  const RANGES: [number, number][] = [[0, 6], [6, 12], [12, 19], [19, 26], [26, 34], [34, 44]];
  const GAIN = [0.95, 1.05, 1.15, 1.3, 1.5, 1.8];
  const STATIC = [0.5, 0.78, 1, 0.72, 0.5, 0.36]; // tvar při omezení pohybu

  let canvas: HTMLCanvasElement | undefined = $state();
  let palette: ArtPalette = DEFAULT_PALETTE;
  const level = new Array<number>(BARS).fill(MIN);
  // Automatické vyrovnání: každý proužek se měří proti svému nedávnému maximu (pomalu klesá),
  // takže tancuje u tiché i hlasité hudby a nedrží se u stropu.
  const peak = new Array<number>(BARS).fill(0.2);
  let lastTargetAt = 0;
  let raf = 0;
  let last = 0;

  $effect(() => {
    const src = art;
    artPalette(src).then((p) => {
      if (src !== art) return;
      palette = p;
      draw();
    });
  });

  $effect(() => {
    void playing;
    void active;
    void canvas;
    start();
  });

  // Nový snímek spektra probudí smyčku (kdyby stála).
  $effect(() => onFrame(() => start()));

  function start() {
    if (!canvas) return;
    if (motionReduced()) {
      for (let i = 0; i < BARS; i++) level[i] = playing ? STATIC[i]! : MIN;
      draw();
      return;
    }
    if (!raf && active) {
      last = performance.now();
      raf = requestAnimationFrame(tick);
    }
  }

  function targets(now: number): number[] {
    if (!playing) return level.map(() => MIN);
    const bins = getBins();
    if (bins.length >= 44 && now - lastFrameAt() < 400) {
      const dt = Math.min(now - lastTargetAt, 100);
      lastTargetAt = now;
      const decay = Math.exp(-dt / 1500); // maximum klesá s poločasem ~1 s
      return RANGES.map(([a, b], i) => {
        let sum = 0;
        for (let k = a; k < b; k++) sum += bins[k] ?? 0;
        const v = (sum / (b - a)) * GAIN[i]!;
        peak[i] = Math.max(v, peak[i]! * decay, 0.12);
        const norm = Math.min(1, v / peak[i]!);
        return MIN + (1 - MIN) * Math.pow(norm, 1.6); // víc kontrastu: tiché části nízko, údery vysoko
      });
    }
    // Spektrum nechodí: klidné vlnění (různé rychlosti a fáze, ať to nepůsobí strojově).
    const t = now / 1000;
    return level.map((_, i) => 0.42 + 0.3 * Math.sin(t * (2.1 + i * 0.37) + i * 1.7) * Math.sin(t * (0.9 + i * 0.21) + i));
  }

  function tick(now: number) {
    raf = 0;
    const dt = Math.min(now - last, 50);
    last = now;
    const target = targets(now);
    let moving = false;
    for (let i = 0; i < BARS; i++) {
      // Nástup rychlý, pokles pomalejší (jako ručička VU metru); podle času, ne snímků.
      const t = target[i]!;
      const l = level[i]!;
      const tau = t > l ? 45 : 140;
      level[i] = l + (t - l) * (1 - Math.exp(-dt / tau));
      if (Math.abs(t - level[i]!) > 0.004) moving = true;
    }
    draw();
    if (active && (playing || moving)) raf = requestAnimationFrame(tick);
  }

  function draw() {
    if (!canvas) return;
    const dpr = window.devicePixelRatio || 1;
    const W = Math.round(width * dpr);
    const H = Math.round(height * dpr);
    if (canvas.width !== W || canvas.height !== H) {
      canvas.width = W;
      canvas.height = H;
    }
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    ctx.clearRect(0, 0, W, H);
    // Jemný přechod přes celou skupinu: převládající barva obalu -> druhá barva.
    const grad = ctx.createLinearGradient(0, 0, W, H);
    grad.addColorStop(0, palette.a);
    grad.addColorStop(1, palette.b);
    ctx.fillStyle = grad;
    ctx.globalAlpha = playing ? 1 : 0.7;
    const gap = W / BARS;
    const bw = gap * 0.55; // kapsle ~ polovina rozestupu (jako na fotkách)
    for (let i = 0; i < BARS; i++) {
      const h = Math.max(bw, level[i]! * H);
      const x = i * gap + (gap - bw) / 2;
      const y = (H - h) / 2;
      ctx.beginPath();
      ctx.roundRect(x, y, bw, h, bw / 2);
      ctx.fill();
    }
  }
</script>

<canvas bind:this={canvas} class="waveform" style:width="{width}px" style:height="{height}px" aria-hidden="true"></canvas>

<style>
  .waveform {
    display: block;
    flex: none;
  }
</style>
