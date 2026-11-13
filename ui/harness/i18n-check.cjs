// Kontrola překladů: každý t('…') ve zdrojácích musí mít anglický překlad v src/lib/i18n/en.ts
// (jinak se anglickému uživateli ukáže česky). Spouští se v rámci `npm run build`.
const fs = require('fs');
const path = require('path');

const src = path.join(__dirname, '..', 'src');
const files = [];
(function walk(dir) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, e.name);
    if (e.isDirectory()) walk(p);
    else if (/\.(svelte|ts)$/.test(e.name) && !p.includes(`${path.sep}i18n${path.sep}`)) files.push(p);
  }
})(src);

const enSource = fs.readFileSync(path.join(src, 'lib', 'i18n', 'en.ts'), 'utf8');
const known = new Set([...enSource.matchAll(/^\s*'((?:[^'\\]|\\.)*)':/gm)].map((m) => m[1]));

// Klíče, které se překládají dynamicky (t(proměnná)): názvy motivů.
const dynamic = [...fs.readFileSync(path.join(src, 'lib', 'themes.ts'), 'utf8').matchAll(/name: '([^']+)'/g)].map((m) => m[1]);
dynamic.push('Vlastní');

const missing = [];
for (const f of files) {
  const text = fs.readFileSync(f, 'utf8');
  for (const m of text.matchAll(/\bt\('((?:[^'\\]|\\.)*)'/g))
    if (!known.has(m[1])) missing.push(`${path.relative(src, f)}: ${m[1]}`);
}
for (const k of dynamic) if (!known.has(k)) missing.push(`(dynamický klíč): ${k}`);

if (missing.length) {
  console.error(`Chybí anglický překlad (${missing.length}):\n  ` + [...new Set(missing)].join('\n  '));
  process.exit(1);
}
console.log(`i18n: všech ${known.size} textů má anglický překlad.`);
