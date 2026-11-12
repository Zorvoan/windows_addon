/**
 * Zvětšování ikon jako v docku: ikony blízko kurzoru se zvětší, dál od něj méně.
 * Vzdálenost se měří od středů ikon v KLIDOVÉM rozložení (stabilní, nekmitá).
 */
export interface MagnifyInput {
  /** Středy ikon v klidu (px od levého okraje pásu). */
  centers: number[];
  /** Pozice kurzoru (px od levého okraje pásu v klidu) nebo null. */
  cursor: number | null;
  base: number;
  maxScale: number;
  /** 0..1 – plynulé zapnutí/vypnutí efektu. */
  strength: number;
}

export interface MagnifyResult {
  sizes: number[];
  /** O kolik posunout pás, aby obsah pod kurzorem zůstal na místě. */
  shift: number;
}

export function magnify({ centers, cursor, base, maxScale, strength }: MagnifyInput): MagnifyResult {
  const radius = base * 2.6;
  if (cursor === null || strength <= 0 || maxScale <= 1) {
    return { sizes: centers.map(() => base), shift: 0 };
  }

  let extraLeft = 0;
  let extraRight = 0;
  const sizes = centers.map((c) => {
    const d = Math.abs(cursor - c);
    const f = d < radius ? (Math.cos((Math.PI * d) / radius) + 1) / 2 : 0;
    const size = base * (1 + (maxScale - 1) * f * strength);
    const extra = size - base;
    // Ikona pod kurzorem roste na obě strany, ostatní jen od kurzoru pryč.
    if (Math.abs(cursor - c) < base / 2) {
      const leftPart = 0.5 + (cursor - c) / base;
      extraLeft += extra * leftPart;
      extraRight += extra * (1 - leftPart);
    } else if (c < cursor) extraLeft += extra;
    else extraRight += extra;
    return size;
  });

  return { sizes, shift: -(extraLeft - extraRight) / 2 };
}
