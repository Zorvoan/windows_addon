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
  { name: 'lock', width: 1600, height: 900, backdrop: false, interact: useLock },
  { name: 'start', width: 820, height: 600, backdrop: true, interact: useStart },
  { name: 'island', width: 480, height: 176, backdrop: true, interact: hoverIsland },
  { name: 'settings', width: 900, height: 900, backdrop: false, fullPage: true, interact: toggleSetting },
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
  const hovers = (await page.evaluate(() => window.__sent)).filter((m) => m.type === 'dock.hover');
  report.notes.push(`dock.hover: ${hovers.map((h) => h.payload.appId ?? 'null').join(' → ')}`);
  if (!hovers.some((h) => h.payload.appId && h.payload.rect)) report.errors.push('dock: nad spuštěnou aplikací nepřišel dock.hover s obdélníkem');
}

async function useLock(page, report) {
  const time = await page.locator('.time').textContent();
  report.notes.push(`zámek: čas "${time}", uživatel "${await page.locator('.user > span:not(.avatar)').textContent()}"`);
  // Editor: otevřít, změnit styl, uložit
  await page.evaluate(() => window.__deliver({ type: 'lock.visibility', payload: { open: true, editing: true } }));
  await page.waitForTimeout(200);
  await page.screenshot({ path: path.join(outDir, `lock-${report.scheme}-editor.png`) });
  await page.getByRole('radio', { name: 'Tenké' }).click();
  await page.getByRole('button', { name: 'Hotovo' }).click();
  let sent = await page.evaluate(() => window.__sent);
  const style = sent.filter((m) => m.type === 'settings.update').at(-1)?.payload.settings.lock.clockStyle;
  if (style !== 'thin') report.errors.push(`zámek: editor neuložil styl hodin (${style})`);
  if (!sent.some((m) => m.type === 'lock.editDone')) report.errors.push('zámek: Hotovo neposlalo lock.editDone');
  if (sent.some((m) => m.type === 'lock.dismiss')) report.errors.push('zámek: v editoru se nesmí odemykat');
  // Běžný režim: klávesa = odemknout
  await page.evaluate(() => window.__deliver({ type: 'lock.visibility', payload: { open: true, editing: false } }));
  await page.waitForTimeout(100);
  await page.keyboard.press('Space');
  await page.waitForTimeout(500);
  sent = await page.evaluate(() => window.__sent);
  if (!sent.some((m) => m.type === 'lock.dismiss')) report.errors.push('zámek: klávesa neodemkla (lock.dismiss)');
}

async function useStart(page, report) {
  await page.keyboard.type('kalk');
  await page.waitForTimeout(150);
  const shown = await page.locator('.app .name').allTextContents();
  report.notes.push(`hledání "kalk": ${shown.join(', ')}`);
  if (shown[0] !== 'Kalkulačka') report.errors.push('Start: "kalk" nenašlo Kalkulačku jako první');
  await page.keyboard.press('Enter');
  await page.keyboard.press('Escape');
  const sent = await page.evaluate(() => window.__sent);
  const launch = sent.filter((m) => m.type === 'start.launch').at(-1);
  if (!launch?.payload.appId.includes('WindowsCalculator')) report.errors.push('Start: Enter nespustil Kalkulačku');
  if (!sent.some((m) => m.type === 'start.close')) report.errors.push('Start: Esc neposlal start.close');
  await page.keyboard.press('Control+A');
  await page.keyboard.type('vsc');
  report.notes.push(`hledání "vsc" (počáteční písmena): ${(await page.locator('.app .name').allTextContents()).join(', ') || '–'}`);
}

async function hoverIsland(page, report) {
  const sentBefore = (await page.evaluate(() => window.__sent)).filter((m) => m.type === 'island.layout');
  const compact = sentBefore.at(-1)?.payload.hit[0];
  await page.mouse.move(240, 18, { steps: 5 });
  await page.waitForTimeout(800);
  await page.screenshot({ path: path.join(outDir, `island-${report.scheme}-hover.png`) });
  const after = (await page.evaluate(() => window.__sent)).filter((m) => m.type === 'island.layout');
  const expanded = after.at(-1)?.payload.hit[0];
  report.notes.push(`island.layout kompaktní ${JSON.stringify(compact)} -> rozbalený ${JSON.stringify(expanded)}`);
  if (!compact || !expanded || expanded.height <= compact.height) report.errors.push('ostrůvek: tvar se po najetí nezvětšil');
  await page.getByRole('button', { name: 'Další' }).click();
  const cmd = (await page.evaluate(() => window.__sent)).filter((m) => m.type === 'media.command');
  if (cmd.at(-1)?.payload.command !== 'next') report.errors.push('ostrůvek: tlačítko Další neposlalo media.command');
}

async function toggleSetting(page, report) {
  await page.getByRole('switch', { name: 'Testovací karta' }).click();
  await page.getByRole('switch', { name: 'Skrýt hlavní panel' }).click(); // má se jen zeptat
  await page.waitForTimeout(200);
  const updates = (await page.evaluate(() => window.__sent)).filter((m) => m.type === 'settings.update');
  report.notes.push(`settings.update zpráv: ${updates.length}; showTestCard=${updates.at(-1)?.payload.settings.desktop.showTestCard}; taskbar.hide=${updates.at(-1)?.payload.settings.taskbar.hide}`);
  if (updates.length !== 1) report.errors.push('nastavení: čekal jsem právě 1 settings.update (hlavní panel se má nejdřív potvrdit)');
  if (!(await page.getByText('Opravdu skrýt?').isVisible())) report.errors.push('nastavení: chybí potvrzení skrytí hlavního panelu');
  // Motiv: předvolba -> settings.update s novým motivem; import -> theme.import
  await page.getByRole('button', { name: 'Oceán' }).click();
  await page.getByLabel('Kód motivu k importu').fill('BW1.abc');
  await page.getByRole('button', { name: 'Použít', exact: true }).click();
  await page.waitForTimeout(150);
  const after = await page.evaluate(() => window.__sent);
  const style = after.filter((m) => m.type === 'settings.update').at(-1)?.payload.settings.appearance.style;
  report.notes.push(`motiv po kliknutí: ${style?.name} ${style?.accent}; theme.import: ${after.some((m) => m.type === 'theme.import')}`);
  if (style?.name !== 'Oceán') report.errors.push('nastavení: předvolba motivu se neposlala');
  if (!after.some((m) => m.type === 'theme.import')) report.errors.push('nastavení: import motivu neposlal theme.import');
  // Zámek: tlačítko Upravit otevře editor
  await page.getByRole('button', { name: 'Upravit', exact: true }).click();
  const lockOpen = (await page.evaluate(() => window.__sent)).filter((m) => m.type === 'lock.open').at(-1);
  if (lockOpen?.payload.editing !== true) report.errors.push('nastavení: Upravit zamykací obrazovku neposlalo lock.open {editing:true}');
  // Klávesa Win je ve výchozím stavu vypnutá
  if (await page.getByRole('switch', { name: 'Otevírat klávesou Windows' }).getAttribute('aria-checked') !== 'false') report.errors.push('nastavení: klávesa Windows není ve výchozím stavu vypnutá');
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
