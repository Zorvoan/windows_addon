/** Vyhledávání aplikací: bez ohledu na velikost písmen a diakritiku. */
export function normalize(text: string): string {
  return text
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .trim();
}

/**
 * Seřadí shody: začátek názvu > začátek slova > kdekoli > počáteční písmena slov (např. "vsc" = Visual Studio Code).
 * Vrací jen shody; prázdný dotaz = vše beze změny pořadí.
 */
export function rank<T>(items: T[], query: string, name: (item: T) => string): T[] {
  const q = normalize(query);
  if (!q) return items;
  const scored: { item: T; score: number; index: number }[] = [];
  items.forEach((item, index) => {
    const n = normalize(name(item));
    let score = -1;
    if (n.startsWith(q)) score = 0;
    else if (n.split(/[\s\-_.]+/).some((w) => w.startsWith(q))) score = 1;
    else if (n.includes(q)) score = 2;
    else {
      const initials = n
        .split(/[\s\-_.]+/)
        .map((w) => w[0] ?? '')
        .join('');
      if (initials.startsWith(q)) score = 3;
    }
    if (score >= 0) scored.push({ item, score, index });
  });
  return scored.sort((a, b) => a.score - b.score || a.index - b.index).map((s) => s.item);
}
