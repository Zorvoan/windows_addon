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
  { name: 'topbar', width: 1280, height: 36, backdrop: true, suffix: '-1280' },
  { name: 'topbar', width: 1100, height: 36, backdrop: true, suffix: '-1100' },
  { name: 'dock', width: 1200, height: 170, backdrop: true, interact: hoverDock },
  { name: 'desktop', width: 1600, height: 900, backdrop: false, interact: typeOnDesktop },
  { name: 'lock', width: 1600, height: 900, backdrop: false, interact: useLock },
  { name: 'start', width: 820, height: 600, backdrop: true, interact: useStart },
  { name: 'island', width: 480, height: 200, backdrop: true, interact: hoverIsland },
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

async function hoverDock(page, report, fixtures) {
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

  // Spouštění: ikona poskakuje, oblast okna má rezervu na celý skok, po naběhnutí aplikace skok doběhne.
  const icon = page.getByRole('button', { name: 'Nastavení', exact: true });
  await icon.click();
  await page.mouse.move(5, 5);
  await page.waitForTimeout(500);
  if ((await icon.getAttribute('data-dock-bouncing')) === null) report.errors.push('dock: po kliknutí na nespuštěnou aplikaci ikona neposkakuje');
  const slot = await icon.evaluate((el) => el.parentElement.getBoundingClientRect().toJSON());
  const size = await icon.evaluate((el) => el.offsetHeight);
  const hit = (await page.evaluate(() => window.__sent)).filter((m) => m.type === 'dock.layout').at(-1)?.payload.hit ?? [];
  const top = Math.min(...hit.map((r) => r.y));
  report.notes.push(`poskakování: horní okraj oblasti ${top}, klidová ikona ${Math.round(slot.y)}, skok ${size / 2}`);
  if (top > slot.y - size / 2) report.errors.push('dock: oblast okna nepokrývá dráhu skoku (ikona by se ořízla)');
  const state = fixtures.find((m) => m.type === 'dock.state');
  const running = JSON.parse(JSON.stringify(state));
  running.payload.items.find((i) => i.name === 'Nastavení').running = true;
  await page.evaluate((m) => window.__deliver(m), running);
  await page.waitForTimeout(900);
  const after = await icon.evaluate((el) => [el.hasAttribute('data-dock-bouncing'), getComputedStyle(el).transform]);
  if (after[0] || (after[1] !== 'none' && after[1] !== 'matrix(1, 0, 0, 1, 0, 0)')) report.errors.push(`dock: poskakování po naběhnutí neskončilo (${after[1]})`);
}

// Psaní do widgetu: klik do pole pošle desktop.textInput (C# ukáže vstupní okno), výsledek se vrátí
// zprávou desktop.textResult a Enter (submit) připomínku přidá.
async function typeOnDesktop(page, report) {
  const input = page.getByPlaceholder('Přidat připomínku…');
  if (!(await input.count())) return;
  await input.click();
  const req = (await page.evaluate(() => window.__sent)).filter((m) => m.type === 'desktop.textInput').at(-1);
  if (!req || !(req.payload.rect.width > 0)) {
    report.errors.push('plocha: klik do připomínky neposlal desktop.textInput s polohou pole');
    return;
  }
  await page.evaluate((key) => window.__deliver({ type: 'desktop.textResult', payload: { key, value: 'Zavolat mámě', submit: true } }), req.payload.key);
  await page.waitForTimeout(100);
  const saved = (await page.evaluate(() => window.__sent)).filter((m) => m.type === 'store.set' && m.payload.key === 'reminders').at(-1);
  report.notes.push(`psaní do připomínky: ${JSON.stringify(saved?.payload.value?.map((r) => r.text))}`);
  if (!saved?.payload.value?.some((r) => r.text === 'Zavolat mámě')) report.errors.push('plocha: text z vstupního okna se nepřidal do připomínek');
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
  await page.keyboard.press('Space'); // hned po otevření: ignorovat
  await page.waitForTimeout(800);
  await page.keyboard.press('AudioVolumeUp'); // hlasitost: ignorovat
  await page.keyboard.press('Shift'); // modifikátor: ignorovat
  await page.waitForTimeout(100);
  if ((await page.evaluate(() => window.__sent)).some((m) => m.type === 'lock.dismiss')) report.errors.push('zámek: odemkl se klávesou, která měla být ignorována');
  await page.keyboard.press('Space');
  await page.waitForTimeout(500);
  sent = await page.evaluate(() => window.__sent);
  if (!sent.some((m) => m.type === 'lock.dismiss')) report.errors.push('zámek: klávesa neodemkla (lock.dismiss)');
}

async function useStart(page, report) {
  // C# po zobrazení okna pošle start.visibility – teprve pak obsah naběhne (animace otevření).
  await page.evaluate(() => window.__deliver({ type: 'start.visibility', payload: { open: true } }));
  await page.waitForTimeout(700);
  const opacity = await page.evaluate(() => getComputedStyle(document.querySelector('.start')).opacity);
  if (opacity !== '1') report.errors.push(`Start: po otevření není obsah vidět (opacity ${opacity})`);
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
  await page.waitForTimeout(800); // náběh kompaktní pilulky (pružina) a přesný tvar po dosednutí
  const sentBefore = (await page.evaluate(() => window.__sent)).filter((m) => m.type === 'island.layout');
  const compact = sentBefore.at(-1)?.payload.hit[0];
  await page.mouse.move(240, 18, { steps: 5 });
  await page.waitForTimeout(1500); // předfáze 370 ms + pružina ~0,8 s
  await page.screenshot({ path: path.join(outDir, `island-${report.scheme}-hover.png`) });
  const after = (await page.evaluate(() => window.__sent)).filter((m) => m.type === 'island.layout');
  const expanded = after.at(-1)?.payload.hit[0];
  const expandedMsgs = (await page.evaluate(() => window.__sent)).filter((m) => m.type === 'island.expanded').map((m) => m.payload.expanded);
  if (expandedMsgs.at(-1) !== true) report.errors.push('ostrůvek: po rozbalení neposlal island.expanded {expanded:true} (lišta se nerozmaže)');
  report.notes.push(`island.layout kompaktní ${JSON.stringify(compact)} -> rozbalený ${JSON.stringify(expanded)}`);
  if (!compact || !expanded || expanded.height <= compact.height) report.errors.push('ostrůvek: tvar se po najetí nezvětšil');
  const pillAfter = await page.evaluate(() => document.querySelector('.pill').getBoundingClientRect().toJSON());
  if (Math.round(pillAfter.width) !== 400 || Math.round(pillAfter.height) !== 156) report.errors.push(`ostrůvek: po dosednutí má ${pillAfter.width}×${pillAfter.height}, čekal jsem 400×156`);
  // Přesný tvar rozbalené pilulky = 400 + stín 2×26 = 452 (obálka během animace je širší).
  if (expanded && expanded.width > 460) report.errors.push('ostrůvek: po dosednutí zůstala v oblasti okna obálka místo přesného tvaru');
  await page.getByRole('button', { name: 'Další' }).click();
  // Sbalení: oblast okna se má zmenšovat s pilulkou (ne držet velkou obálku a polykat kliknutí na lištu).
  await page.mouse.move(240, 196, { steps: 2 });
  await page.waitForTimeout(300 + 250); // zpoždění odjetí (300 ms) + půlka animace
  const collapsing = (await page.evaluate(() => window.__sent)).filter((m) => m.type === 'island.layout').at(-1)?.payload.hit[0];
  report.notes.push(`island.layout během sbalování ${JSON.stringify(collapsing)}`);
  if (!collapsing || collapsing.height > 100) report.errors.push('ostrůvek: oblast okna se při sbalování nezmenšuje s pilulkou');
  await page.mouse.move(240, 18, { steps: 2 });
  await page.waitForTimeout(1500);
  const cmd = (await page.evaluate(() => window.__sent)).filter((m) => m.type === 'media.command');
  if (cmd.at(-1)?.payload.command !== 'next') report.errors.push('ostrůvek: tlačítko Další neposlalo media.command');

  // Omezené animace: tvar se změní hned (bez pružiny).
  await page.evaluate(() => window.__deliver({ type: 'performance.state', payload: { reduced: true, reason: 'test' } }));
  await page.mouse.move(240, 196, { steps: 2 });
  await page.waitForTimeout(450); // zpoždění odjetí myší (300 ms) + snímek
  const reducedSize = await page.evaluate(() => document.querySelector('.pill').getBoundingClientRect().height);
  report.notes.push(`omezené animace: výška pilulky po odjetí myší ${reducedSize}`);
  if (Math.round(reducedSize) !== 28) report.errors.push(`ostrůvek: s omezenými animacemi se nezmenšil hned (výška ${reducedSize})`);
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
      if (def.name === 'desktop' && scheme === 'dark') {
        // Snížená průhlednost z Windows -> sklo se vypne (neprůhledné plochy)
        const before = await page.evaluate(() => getComputedStyle(document.documentElement).getPropertyValue('--bw-glass-blur').trim());
        await page.evaluate(() => window.__deliver({ type: 'system.appearance', payload: { reduceTransparency: true, highContrast: false, accent: '#c42b1c' } }));
        await page.waitForTimeout(100);
        const after = await page.evaluate(() => [getComputedStyle(document.documentElement).getPropertyValue('--bw-glass-blur').trim(), getComputedStyle(document.documentElement).getPropertyValue('--bw-accent').trim()]);
        report.notes.push(`snížená průhlednost: blur ${before} -> ${after[0]}, zvýraznění Windows -> ${after[1]}`);
        if (after[0] !== '0px') report.errors.push('plocha: snížená průhlednost nevypnula sklo');
        if (after[1] !== '#c42b1c') report.errors.push('plocha: nepoužila se barva zvýraznění Windows');
        await page.evaluate(() => window.__deliver({ type: 'system.appearance', payload: { reduceTransparency: false, highContrast: false, accent: '#0078d4' } }));
      }
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
      await page.screenshot({ path: path.join(outDir, `${def.name}${def.suffix ?? ''}-${scheme}.png`), fullPage: !!def.fullPage });
      if (def.name === 'topbar') {
        // Nic nesmí přetékat přes okraj lišty.
        const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
        if (overflow > 0) report.errors.push(`lišta přetéká o ${overflow}px při šířce ${def.width}`);
      }
      if (def.interact) await def.interact(page, report, fixtures);

      const sent = await page.evaluate(() => window.__sent);
      for (const m of sent) if (m.type === 'ui.log' && m.payload.level === 'error') report.errors.push(`ui.log: ${m.payload.message}`);
      const types = [...new Set(sent.map((m) => m.type))].join(', ');

      const status = report.errors.length ? 'CHYBA' : 'ok';
      console.log(`[${status}] ${def.name}${def.suffix ?? ''} (${scheme}) – odeslané zprávy: ${types}`);
      report.notes.forEach((n) => console.log('       ' + n));
      report.errors.forEach((e) => console.log('       ! ' + e));
      if (report.errors.length) failed = true;
      await page.close();
    }
  }

  // Angličtina: Windows v jiném jazyce než češtině -> stránky anglicky (navigator.language z WebView2).
  // Na stránce nesmí zůstat žádný český text ze slovníku (= zapomenutý překlad).
  const enSource = fs.readFileSync(path.join(__dirname, '..', 'src', 'lib', 'i18n', 'en.ts'), 'utf8');
  const czechKeys = [...enSource.matchAll(/^\s*'((?:[^'\\]|\\.)*)':\s*\n?\s*'((?:[^'\\]|\\.)*)'/gm)]
    .filter((m) => m[1] !== m[2] && m[1].length >= 4 && !m[1].includes('{'))
    .map((m) => m[1])
    // Data z C# (názvy aplikací Windows, texty stavu) jsou ve fixtures česky – překládá je C# (Loc), ne stránka.
    .filter((k) => !JSON.stringify(fixtures).includes(k));
  const expectEn = { topbar: 'Quit', settings: 'Appearance', start: 'Search', desktop: 'Edit Widgets', lock: 'Click or press any key' };
  for (const def of pages.filter((p) => !p.suffix && p.name in expectEn)) {
    const errors = [];
    const page = await browser.newPage({ viewport: { width: def.width, height: def.height }, colorScheme: 'dark', locale: 'en-US', timezoneId: 'Europe/London' });
    page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`));
    await page.addInitScript(() => {
      const listeners = [];
      window.__sent = [];
      window.chrome = { webview: { postMessage: (m) => window.__sent.push(m), addEventListener: (_t, cb) => listeners.push(cb) } };
      window.__deliver = (msg) => listeners.forEach((cb) => cb({ data: msg }));
    });
    await page.goto(`${base}/${def.name}.html`);
    await page.waitForFunction(() => window.__sent.some((m) => m.type === 'ui.ready'), null, { timeout: 5000 }).catch(() => errors.push('stránka neposlala ui.ready'));
    for (const msg of fixtures) await page.evaluate((m) => window.__deliver(m), msg);
    if (def.name === 'start') await page.evaluate(() => window.__deliver({ type: 'start.visibility', payload: { open: true } }));
    await page.waitForTimeout(500);
    await page.screenshot({ path: path.join(outDir, `${def.name}-en.png`), fullPage: !!def.fullPage });
    const { lang, text } = await page.evaluate(() => {
      const attrs = [...document.querySelectorAll('[aria-label],[title],[placeholder]')]
        .flatMap((e) => ['aria-label', 'title', 'placeholder'].map((a) => e.getAttribute(a) ?? ''));
      return { lang: document.documentElement.lang, text: document.body.innerText + '\n' + attrs.join('\n') };
    });
    if (lang !== 'en') errors.push(`<html lang> je "${lang}", čekal jsem "en"`);
    if (!text.includes(expectEn[def.name])) errors.push(`chybí anglický text "${expectEn[def.name]}"`);
    const word = (k) => new RegExp(`(^|[^\\p{L}])${k.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}($|[^\\p{L}])`, 'u');
    const leftovers = czechKeys.filter((k) => word(k).test(text));
    if (leftovers.length) errors.push(`český text v anglické verzi: ${leftovers.join(' | ')}`);
    console.log(`[${errors.length ? 'CHYBA' : 'ok'}] ${def.name} (en-US)`);
    errors.forEach((e) => console.log('       ! ' + e));
    if (errors.length) failed = true;
    await page.close();
  }

  await browser.close();
  server.close();
  console.log(failed ? '\nHarness: SELHALO' : `\nHarness: vše v pořádku, snímky v ${outDir}`);
  process.exit(failed ? 1 : 0);
})();
