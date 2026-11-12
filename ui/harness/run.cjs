/**
 * UI harness: vykreslí všechny stránky v headless Chromiu bez Windows a bez C#.
 *  - podvrhne window.chrome.webview (zachytává odeslané zprávy),
 *  - pošle stránce zprávy z harness/fixtures.json (generuje je C# test ContractTests -> stejný tvar jako v aplikaci),
 *  - udělá snímky ve světlém i tmavém režimu do harness/out/,
 *  - selže, když stránka vyhodí chybu nebo zaloguje chybu.
 *
 * Spuštění:  dotnet test (vygeneruje fixtures)  &&  npm run build  &&  npm run harness
 * Playwright musí být k dispozici (require('playwright')).
 */
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('playwright');

const root = path.resolve(__dirname, '..', 'dist');
const outDir = path.join(__dirname, 'out');
const fixturesPath = path.join(__dirname, 'fixtures.json');

const pages = [
  { name: 'topbar', width: 1600, height: 36, backdrop: true },
  { name: 'dock', width: 1200, height: 170, backdrop: true, interact: hoverDock },
  { name: 'desktop', width: 1600, height: 900, backdrop: false },
  { name: 'settings', width: 900, height: 900, backdrop: false, fullPage: true },
].filter((p) => fs.existsSync(path.join(root, `${p.name}.html`)));

const mime = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.woff2': 'font/woff2', '.svg': 'image/svg+xml', '.png': 'image/png' };

function serve() {
  return new Promise((resolve) => {
    const server = http.createServer((req, res) => {
      const file = path.join(root, decodeURIComponent(new URL(req.url, 'http://x').pathname));
      if (!file.startsWith(root) || !fs.existsSync(file) || fs.statSync(file).isDirectory()) {
        res.writeHead(404).end();
        return;
      }
      res.writeHead(200, { 'Content-Type': mime[path.extname(file)] ?? 'application/octet-stream' });
      fs.createReadStream(file).pipe(res);
    });
    server.listen(0, '127.0.0.1', () => resolve(server));
  });
}

async function hoverDock(page, report) {
  const icons = await page.locator('[data-dock-icon]').all();
  if (icons.length === 0) {
    report.errors.push('dock: žádné ikony');
    return;
  }
  const box = await icons[Math.floor(icons.length / 2)].boundingBox();
  await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2, { steps: 8 });
  await page.waitForTimeout(400);
  await page.screenshot({ path: path.join(outDir, `dock-${report.scheme}-hover.png`) });
  await page.mouse.move(5, 5, { steps: 4 });
  await page.waitForTimeout(400);
  const layouts = (await page.evaluate(() => window.__sent)).filter((m) => m.type === 'dock.layout');
  report.notes.push(`dock.layout zpráv: ${layouts.length}; první: ${JSON.stringify(layouts[0]?.payload)}; poslední: ${JSON.stringify(layouts.at(-1)?.payload)}`);
  if (layouts.length === 0) report.errors.push('dock: nepřišla žádná zpráva dock.layout');
}

(async () => {
  if (!fs.existsSync(fixturesPath)) {
    console.error('Chybí harness/fixtures.json – spusť nejdřív "dotnet test" v kořeni repozitáře.');
    process.exit(2);
  }
  const fixtures = JSON.parse(fs.readFileSync(fixturesPath, 'utf8'));
  fs.mkdirSync(outDir, { recursive: true });

  const server = await serve();
  const base = `http://127.0.0.1:${server.address().port}`;
  const browser = await chromium.launch();
  let failed = false;

  for (const def of pages) {
    for (const scheme of ['dark', 'light']) {
      const report = { scheme, errors: [], notes: [] };
      const page = await browser.newPage({ viewport: { width: def.width, height: def.height }, colorScheme: scheme, locale: 'cs-CZ', timezoneId: 'Europe/Prague' });
      page.on('pageerror', (e) => report.errors.push(`pageerror: ${e.message}`));
      page.on('console', (m) => m.type() === 'error' && report.errors.push(`console: ${m.text()}`));
      await page.addInitScript(() => {
        const listeners = [];
        window.__sent = [];
        window.chrome = {
          webview: {
            postMessage: (m) => window.__sent.push(m),
            addEventListener: (_t, cb) => listeners.push(cb),
          },
        };
        window.__deliver = (msg) => listeners.forEach((cb) => cb({ data: msg }));
      });

      await page.goto(`${base}/${def.name}.html`);
      await page.waitForFunction(() => window.__sent.some((m) => m.type === 'ui.ready'), null, { timeout: 5000 }).catch(() => report.errors.push('stránka neposlala ui.ready'));
      if (def.backdrop) {
        await page.addStyleTag({ content: `html{background:${scheme === 'dark' ? 'linear-gradient(90deg,#2d3b55,#11151d)' : 'linear-gradient(90deg,#c9d6e8,#f1e9e0)'}}` });
      }
      for (const msg of fixtures) await page.evaluate((m) => window.__deliver(m), msg);
      if (def.name === 'desktop') {
        // Barevná "tapeta" (vygenerovaná v prohlížeči), ať je vidět rozmazání skleněných karet.
        await page.evaluate(() => {
          const c = document.createElement('canvas');
          c.width = 320; c.height = 180;
          const g = c.getContext('2d');
          const grad = g.createLinearGradient(0, 0, 320, 180);
          grad.addColorStop(0, '#1d4e89'); grad.addColorStop(0.5, '#00b2ca'); grad.addColorStop(1, '#f79256');
          g.fillStyle = grad; g.fillRect(0, 0, 320, 180);
          for (let i = 0; i < 40; i++) { g.fillStyle = `hsl(${i * 37} 70% 60% / 0.8)`; g.beginPath(); g.arc((i * 53) % 320, (i * 29) % 180, 6 + (i % 5) * 4, 0, 7); g.fill(); }
          window.__deliver({ type: 'wallpaper.state', payload: { url: c.toDataURL('image/png'), color: '#000' } });
        });
      }
      await page.waitForTimeout(500);
      await page.screenshot({ path: path.join(outDir, `${def.name}-${scheme}.png`), fullPage: !!def.fullPage });
      if (def.interact) await def.interact(page, report);

      const sent = await page.evaluate(() => window.__sent);
      for (const m of sent) if (m.type === 'ui.log' && m.payload.level === 'error') report.errors.push(`ui.log: ${m.payload.message}`);
      const types = [...new Set(sent.map((m) => m.type))].join(', ');

      const status = report.errors.length ? 'CHYBA' : 'ok';
      console.log(`[${status}] ${def.name} (${scheme}) – odeslané zprávy: ${types}`);
      report.notes.forEach((n) => console.log('       ' + n));
      report.errors.forEach((e) => console.log('       ! ' + e));
      if (report.errors.length) failed = true;
      await page.close();
    }
  }

  await browser.close();
  server.close();
  console.log(failed ? '\nHarness: SELHALO' : `\nHarness: vše v pořádku, snímky v ${outDir}`);
  process.exit(failed ? 1 : 0);
})();
