# Testy na Windows (fáze 1)

> **Nic z fáze 1 zatím neběželo na Windows.** Kód je sestavený (Debug i Release), logika má 28 automatických testů
> a všechny stránky rozhraní prošly UI harnessem v headless Chromiu. Okna, rozmazání, vložení do plochy, AppBar,
> ikony, zvuk atd. ale ověří až tyhle testy.

## Než začneš

```powershell
git pull
dotnet run --project src/BetterWindows
```

- Log: `%AppData%\BetterWindows\logs\<datum>.log` (watchdog má vlastní `<datum>.watchdog.log`).
- Nastavení: `%AppData%\BetterWindows\settings.json`. Když se něco hodně pokazí, smaž ho – použijí se výchozí hodnoty.
- **Kdyby zmizely ikony plochy nebo hlavní panel:** pravé tlačítko na ikonu v oznamovací oblasti › „Obnovit ikony plochy a hlavní panel“,
  nebo aplikaci ukonči (ikona › Ukončit). Pomůže i restart Průzkumníka (Správce úloh › Průzkumník Windows › Restartovat).
- Když test selže, pošli mi **číslo testu, co se stalo a příslušné řádky z logu** (u každého testu je napsané, které).

Testy jsou seřazené tak, aby každý stavěl na předchozích. Když selže 2.x, ostatní klidně zkoušej dál.

---

## 1. Horní lišta

| # | Udělej | Má se stát | Když ne, pošli |
|---|---|---|---|
| 1.1 | Spusť aplikaci | Nahoře lišta přes celou šířku: vlevo logo a název aplikace v popředí, vpravo počasí, (baterie), ikona nastavení, datum a čas | řádky `Horní lišta`, `WebView2 'topbar'` |
| 1.2 | Pracuj v jiné aplikaci a dívej se na lištu | Lišta je **rozmazaná** i když je aktivní jiné okno | řádky `pozadí SystemAcrylic` a `DWM: … selhalo` |
| 1.3 | Ikona v oznamovací oblasti › Rozmazání lišty a docku › vyzkoušej všechny 3 | Systémové / Vlastní tón (průhlednější) / Žádné. Napiš, které se ti líbí | totéž |
| 1.4 | V režimu „Žádné“ | Ukazují průhledná místa lišty plochu pod ní, nebo jednolitou barvu? (jen informace) | – |
| 1.5 | Otevři Poznámkový blok, klikni na lištu, piš | Text jde dál do Poznámkového bloku | – |
| 1.6 | Alt+Tab | Lišta ani dock tam nejsou | – |
| 1.7 | Přepni Windows světlý/tmavý režim | Lišta i text se přizpůsobí | řádky `Motiv` |
| 1.8 | Klikni na různá okna (Průzkumník, Nastavení Windows, Kalkulačka, Edge) | Vlevo se mění název aplikace; u Nastavení/Kalkulačky správné jméno (ne „ApplicationFrameHost“) | co lišta ukázala |

## 2. Vrstva plochy (rizikový bod)

| # | Udělej | Má se stát | Když ne, pošli |
|---|---|---|---|
| 2.1 | Minimalizuj všechna okna (Win+D nebo ručně) | Vidíš svou tapetu, vlevo widgety, vpravo skleněnou **testovací kartu** | řádky `Plocha:` (celý blok po startu) |
| 2.2 | Podívej se na kartu › Režim | Napiš mi, co tam je (Vloženo do plochy 24H2+ / klasicky / Okno vespod / Selhalo) | – |
| 2.3 | Jsou ikony plochy schované? | Ano, pokud je režim „Vloženo“ | řádky `Ikony plochy` |
| 2.4 | Klikni na „Test myši“ | Počítadlo roste | – |
| 2.5 | Klikni do pole „Test klávesnice“ a piš | Text se píše (v režimu „Okno vespod“ to **nejde** – to je očekávané) | – |
| 2.6 | Karty widgetů | Jsou skleněné – rozmazaná tapeta pod nimi | – |
| 2.7 | Je vrstva pod všemi okny? Otevři pár aplikací | Ano, okna jsou vždy nad ní | – |
| 2.8 | Změň tapetu ve Windows | Do minuty se změní i na vrstvě | řádky `Tapeta` |
| 2.9 | Nastavení › Plocha › Tapeta › Vybrat obrázek… | Tapeta se změní; „Použít tapetu Windows“ ji vrátí | – |
| 2.10 | Restartuj Průzkumníka (Správce úloh) | Do ~5 s je vrstva zpět, ikony zase skryté | řádky `Průzkumník byl restartován`, `Plocha: restart` |

**Když vrstva není vidět vůbec** (vidíš normální plochu Windows s ikonami): Nastavení › Plocha › Způsob umístění › **Okno vespod** a pošli mi log.
**Když je vidět, ale nad okny nebo nad ikonami**, pošli snímek a log.

## 3. AppBar

| # | Udělej | Má se stát |
|---|---|---|
| 3.1 | Maximalizuj okno | Horní okraj okna je pod lištou (lišta nic nepřekrývá) |
| 3.2 | Pusť video na YouTube přes celou obrazovku (F) nebo hru | Lišta i dock zmizí, po ukončení se vrátí |

## 4. Data v liště

| # | Udělej | Má se stát | Když ne, pošli |
|---|---|---|---|
| 4.1 | Počasí | Ikona a teplota pro Prahu; po najetí myší detail | řádky `Počasí` |
| 4.2 | Nastavení › Počasí › Město: napiš jiné město + Enter | Do pár sekund nové počasí, pod polem „Nalezeno: …“ | totéž |
| 4.3 | Baterie (jen notebook) | Procenta a ikona; při nabíjení blesk | – |
| 4.4 | Otevři aplikaci Kamera / zapni mikrofon (Diktafon, Teams…) | Zelená tečka (kamera), oranžová (mikrofon); po najetí jméno aplikace; po ukončení zmizí do ~5 s | řádky `Mikrofon:` |

## 5. Dock

| # | Udělej | Má se stát | Když ne, pošli |
|---|---|---|---|
| 5.1 | Dole uprostřed | Dock s Průzkumníkem a Nastavením + spuštěné aplikace s tečkou | řádky `Dock`, `WebView2 'dock'` |
| 5.2 | Ikony | Skutečné ikony aplikací (i u Kalkulačky/Nastavení – aplikace z Microsoft Store) | řádky `Ikona` |
| 5.3 | Najeď myší nad dock | Ikony se plynule zvětší, nad ikonou popisek | – |
| 5.4 | Klikni vedle docku (nad ním, vedle něj) | Klik projde do okna/plochy pod ním (dock nepřekáží) | – |
| 5.5 | Pod docken je rozmazání sedící přesně na pásu | Ano, se zaoblenými rohy | – |
| 5.6 | Klik na neběžící připnutou aplikaci | Spustí se (ikona poskakuje, pak tečka) | řádky `Dock:` / `Spouštím` |
| 5.7 | Klik na běžící aplikaci v pozadí | Přepne se na ni (i když byla minimalizovaná) | řádky `SetForegroundWindow` |
| 5.8 | Klik na aplikaci, která je v popředí | Minimalizuje se | – |
| 5.9 | Pravé tlačítko na ikonu | Nabídka: Připnout/Odepnout, Nové okno, Ukončit. Klik jinam ji zavře a fokus se vrátí aplikaci | – |
| 5.10 | Připni a odepni aplikaci | Dock se změní; po restartu aplikace zůstane | – |
| 5.11 | Ukončit z nabídky | Aplikace se zavře (zeptá se na uložení, pokud je co) | – |
| 5.12 | Nastavení › Dock: velikost ikon, zvětšení | Dock se změní | – |

## 6. Vizualizér a widgety

| # | Udělej | Má se stát | Když ne, pošli |
|---|---|---|---|
| 6.1 | Pusť hudbu a minimalizuj okna | Dole na ploše tečky reagují na hudbu (ne na mikrofon) | řádky `Vizualizér` |
| 6.2 | Nastavení › Vizualizér › Scéna Vlna / Kruh, citlivost | Mění se | – |
| 6.3 | Ztiš / zastav hudbu | Tečky zhasnou | – |
| 6.4 | Přepni výstup (sluchátka ↔ reproduktory) | Vizualizér funguje dál | totéž |
| 6.5 | Widget Připomínky: napiš text + Enter, odškrtni, smaž | Funguje a po restartu aplikace připomínky zůstanou | – |
| 6.6 | „Upravit widgety“: přetáhni widget, přidej, odeber, Hotovo | Pozice zůstanou i po restartu | – |
| 6.7 | Kalendář: šipky mezi měsíci | Fungují, dnešek zvýrazněný | – |

## 7. Hlavní panel Windows (až nakonec)

| # | Udělej | Má se stát |
|---|---|---|
| 7.1 | Nastavení › Hlavní panel › Skrýt (potvrdit) | Hlavní panel zmizí, dock sjede dolů, maximalizovaná okna sahají až dolů |
| 7.2 | Stiskni klávesu Win | Otevře se nabídka Start; hlavní panel se může na chvilku ukázat, pak zase zmizí |
| 7.3 | Vypni přepínač | Panel se vrátí (i se svým původním nastavením automatického skrývání) |
| 7.4 | Znovu skryj, pak ikona › Ukončit | Panel i ikony plochy se vrátí |

**Pozor:** skrytí přepíná hlavní panel na „automaticky skrývat“. To si Windows pamatuje i po restartu počítače –
aplikace to vrací při ukončení, při pádu i po zabití procesu. Kdyby ne: Nastavení Windows › Přizpůsobení › Hlavní panel ›
Chování hlavního panelu › „Automaticky skrývat hlavní panel“ vypnout.

## 8. Bezpečnost (obnova systému)

Skryj ikony plochy (výchozí) a pro test i hlavní panel (7.1). Pak:

| # | Udělej | Má se stát | Když ne, pošli |
|---|---|---|---|
| 8.1 | Ikona › Ukončit | Ikony plochy i hlavní panel se vrátí | hlavní log |
| 8.2 | (Debug) Ikona › „Test: chyba v UI“ | Aplikace **běží dál**, v logu chyba | – |
| 8.3 | (Debug) Ikona › „Test: pád procesu“ | Aplikace skončí; do ~1 s se ikony i panel vrátí (dělá to watchdog) | `…watchdog.log` |
| 8.4 | Spusť znovu, Správce úloh › BetterWindows › Ukončit úlohu | Ikony i panel se vrátí (watchdog) | `…watchdog.log` |
| 8.5 | Spusť aplikaci podruhé | Nevznikne druhá instance, otevře se nastavení | – |

## 9. Různé

| # | Udělej | Má se stát |
|---|---|---|
| 9.1 | Změň měřítko obrazovky (např. 100 % → 125 %) | Lišta, dock i plocha se přizpůsobí, text je ostrý |
| 9.2 | Nechej běžet a uspi / probuď počítač | Vše běží, počasí se obnoví |
| 9.3 | Kontrola Správce úloh po pár minutách | Rozumné CPU v klidu (bez hudby by vizualizér neměl nic dělat) |

---

## Kde jsou známá rizika (ať víš, co čekat)

- **Vložení do plochy na 25H2** – nedokumentované chování Windows. Záloha: režim „Okno vespod“ (bez psaní do widgetů,
  a klávesa Win+D ho může překrýt plochou).
- **Rozmazání okna, které nikdy není aktivní** – Windows 11 ho u některých druhů pozadí vypíná. Proto 3 režimy.
- **Klik na dock → přepnutí aplikace**: Windows má přísná pravidla, kdo smí přenést okno do popředí. Když to odmítne,
  v logu bude `SetForegroundWindow … odmítl`.
- **Aplikace spuštěné jako správce**: dock je neumí zavřít ani vždy přepnout (ochrana Windows – UIPI).
- **Klávesnice ve widgetech** funguje jen ve vloženém režimu a jen když jsou skryté ikony plochy.
