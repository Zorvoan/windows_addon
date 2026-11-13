import { defineConfig } from 'vite';
import { svelte } from '@sveltejs/vite-plugin-svelte';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';

const root = fileURLToPath(new URL('.', import.meta.url));

// Každá část shellu (horní lišta, dock, nastavení, později Start, widgety...)
// je samostatná HTML stránka načtená do vlastního WebView2 okna.
// Novou část přidáš tak, že sem dopíšeš jméno a vytvoříš <jméno>.html.
const pages = ['settings', 'topbar', 'desktop', 'dock', 'island'];

export default defineConfig({
  plugins: [svelte()],
  // Relativní cesty: stránky se načítají z https://app.betterwindows/ (virtuální host
  // namapovaný na složku), ne z vývojového serveru.
  base: './',
  resolve: {
    alias: { $lib: resolve(root, 'src/lib') },
  },
  build: {
    outDir: 'dist',
    emptyOutDir: true,
    target: 'chrome120', // WebView2 je vždy aktuální Chromium
    rollupOptions: {
      input: Object.fromEntries(pages.map((p) => [p, resolve(root, `${p}.html`)])),
    },
  },
});
