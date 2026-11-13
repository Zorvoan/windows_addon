/**
 * Jazyk rozhraní podle Windows: čeština / slovenština -> česky, cokoli jiného -> anglicky.
 * (WebView2 dostává jazyk Windows z C#, takže navigator.language odpovídá systému.)
 *
 * Klíčem překladu je český text: t('Hledat') vrátí v angličtině 'Search'. Proměnné ve složených
 * závorkách: t('Test myši: {n}×', { n: 3 }). Kontrola úplnosti: harness (chybějící anglický překlad = chyba).
 */
import { en } from './i18n/en';

export type Lang = 'cs' | 'en';

export const lang: Lang = /^(cs|sk)\b/i.test(globalThis.navigator?.language ?? 'cs') ? 'cs' : 'en';

if (typeof document !== 'undefined') document.documentElement.lang = lang;

export function t(cs: string, vars?: Record<string, string | number>): string {
  let s = lang === 'cs' ? cs : (en[cs] ?? cs);
  if (vars) for (const k in vars) s = s.replaceAll(`{${k}}`, String(vars[k]));
  return s;
}

/** Pro kontrolu úplnosti (harness): má text anglický překlad? */
export function hasTranslation(cs: string): boolean {
  return cs in en;
}
