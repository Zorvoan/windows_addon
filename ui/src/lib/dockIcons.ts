/** Vlastní ikony speciálních položek docku (Launchpad, Koš) jako SVG data URL. */
const svg = (body: string) => `data:image/svg+xml;utf8,${encodeURIComponent(`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64">${body}</svg>`)}`;

// Launchpad = tečková matice 3×3 (podpis shellu) na grafitové dlaždici, bez barevných přechodů.
const dots = [20, 32, 44]
  .flatMap((y) => [20, 32, 44].map((x) => `<circle cx="${x}" cy="${y}" r="4.5" fill="#ededed"/>`))
  .join('');

export const launchpadIcon = svg(
  `<rect x="2" y="2" width="60" height="60" rx="14" fill="#232325"/>` +
    `<rect x="2.5" y="2.5" width="59" height="59" rx="13.5" fill="none" stroke="#ededed" stroke-opacity="0.12"/>${dots}`,
);

const bin = (papers: boolean) =>
  svg(
    `<defs><linearGradient id="b" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#f4f6fa" stop-opacity="0.95"/><stop offset="1" stop-color="#c9ced8" stop-opacity="0.9"/></linearGradient></defs>` +
      '' +
      `<path d="M14 18h36l-3.5 38a4 4 0 0 1-4 3.6H21.5a4 4 0 0 1-4-3.6z" fill="url(#b)" stroke="#8d96a6" stroke-width="1.2"/>` +
      `<rect x="11" y="14" width="42" height="6" rx="3" fill="#e9edf3" stroke="#8d96a6" stroke-width="1.2"/>` +
      `<path d="M24 26l1.5 26M32 26v26M40 26l-1.5 26" stroke="#9aa3b2" stroke-width="1.6" stroke-linecap="round"/>` +
      // Plný koš: zmačkané papíry vyčnívající nad víko (kreslí se až nakonec, ať je víko nezakryje).
      (papers
        ? `<rect x="19" y="3" width="15" height="12" rx="1.5" fill="#fff" stroke="#8d96a6" transform="rotate(-14 26 9)"/>` +
          `<rect x="31" y="2" width="14" height="12" rx="1.5" fill="#eef1f6" stroke="#8d96a6" transform="rotate(10 38 8)"/>` +
          `<path d="M22 8h8M33 7h8" stroke="#b5bcc8" stroke-width="1.2"/>`
        : ''),
  );

export const trashEmptyIcon = bin(false);
export const trashFullIcon = bin(true);
