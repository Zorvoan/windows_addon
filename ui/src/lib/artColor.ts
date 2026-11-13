/**
 * Barvy z obalu alba pro zvukové proužky v ostrůvku (jako na telefonu: černobílý obal = šedé
 * proužky, hnědý obal = béžové). Vrací dvě ztlumené barvy pro jemný přechod:
 *  a = převládající barva obalu (světlejší), b = druhá nejvýraznější (tmavší).
 * Ztlumené proto, aby proužky na černé pilulce nerušily – jen naznačí atmosféru obalu.
 */
export interface ArtPalette {
  a: string;
  b: string;
}

export const DEFAULT_PALETTE: ArtPalette = { a: 'hsl(0 0% 62%)', b: 'hsl(0 0% 46%)' };

const cache = new Map<string, Promise<ArtPalette>>();

export function artPalette(src: string | null | undefined): Promise<ArtPalette> {
  if (!src) return Promise.resolve(DEFAULT_PALETTE);
  let p = cache.get(src);
  if (!p) {
    p = compute(src).catch(() => DEFAULT_PALETTE);
    cache.set(src, p);
    if (cache.size > 24) cache.delete(cache.keys().next().value as string);
  }
  return p;
}

function load(src: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.onload = () => resolve(img);
    img.onerror = reject;
    img.src = src;
  });
}

type Rgb = [number, number, number];

async function compute(src: string): Promise<ArtPalette> {
  const img = await load(src);
  const N = 24;
  const canvas = document.createElement('canvas');
  canvas.width = canvas.height = N;
  const ctx = canvas.getContext('2d', { willReadFrequently: true });
  if (!ctx) return DEFAULT_PALETTE;
  ctx.drawImage(img, 0, 0, N, N);
  const data = ctx.getImageData(0, 0, N, N).data; // obal z jiného původu by tu vyhodil výjimku -> výchozí barvy

  const px: { c: Rgb; w: number }[] = [];
  for (let i = 0; i < data.length; i += 4) {
    const c: Rgb = [data[i] ?? 0, data[i + 1] ?? 0, data[i + 2] ?? 0];
    const [, s, v] = hsv(c);
    // Váha: barevné a středně světlé pixely; skoro černé a vypálené bílé skoro nic.
    const w = (0.15 + s) * (v < 0.1 || v > 0.97 ? 0.05 : 0.4 + v);
    px.push({ c, w });
  }
  const c1 = mean(px);
  // Druhá barva: pixely výrazně odlišné od první.
  const far = px.filter((p) => dist(p.c, c1) > 70);
  const c2 = far.length > N ? mean(far) : c1;
  return { a: muted(c1, 0.6), b: muted(c2, 0.45) };
}

function mean(px: { c: Rgb; w: number }[]): Rgb {
  let r = 0, g = 0, b = 0, t = 0;
  for (const p of px) {
    r += p.c[0] * p.w;
    g += p.c[1] * p.w;
    b += p.c[2] * p.w;
    t += p.w;
  }
  return t > 0 ? [r / t, g / t, b / t] : [128, 128, 128];
}

const dist = (x: Rgb, y: Rgb) => Math.hypot(x[0] - y[0], x[1] - y[1], x[2] - y[2]);

function hsv([r, g, b]: Rgb): [number, number, number] {
  const R = r / 255, G = g / 255, B = b / 255;
  const max = Math.max(R, G, B), min = Math.min(R, G, B), d = max - min;
  let h = 0;
  if (d > 0) {
    if (max === R) h = ((G - B) / d) % 6;
    else if (max === G) h = (B - R) / d + 2;
    else h = (R - G) / d + 4;
    h *= 60;
    if (h < 0) h += 360;
  }
  return [h, max === 0 ? 0 : d / max, max];
}

/** Odstín z obalu, sytost ztlumená (max 42 %), pevná světlost -> čitelné na černé. */
function muted(c: Rgb, lightness: number): string {
  const [h, s] = hsv(c);
  const sat = s < 0.08 ? 0 : Math.min(s * 0.75, 0.42);
  return `hsl(${Math.round(h)} ${Math.round(sat * 100)}% ${Math.round(lightness * 100)}%)`;
}
