# Testy na Windows (fáze 1)

> **Nic zatím neběželo na Windows.** Kód je sestavený (Debug i Release), logika má 56 automatických testů
> a všechny stránky rozhraní prošly UI harnessem v headless Chromiu. Okna, rozmazání, vložení do plochy, AppBar,
> ikony, zvuk atd. ale ověří až tyhle testy.

## Než začneš

Vývoj je na větvi `claude/serene-ritchie-7k4wyb`. Poprvé (nová složka):

```powershell
git clone -b claude/serene-ritchie-7k4wyb https://github.com/Zorvoan/windows_addon.git
cd windows_addon
dotnet run --project src/BetterWindows
```

Když už repozitář máš:

```powershell
git fetch origin claude/serene-ritchie-7k4wyb
git switch claude/serene-ritchie-7k4wyb
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
| 5.3 | Najeď myší nad dock | Ikony se plynule zvětší, nad ikonou popisek. **Notebook na baterii:** zvětšení je bez plynulého doběhu (režim výkonu) – připoj nabíječku nebo Nastavení › Výkon › „Vždy plné animace“ | – |
| 5.4 | Klikni vedle docku (nad ním, vedle něj) | Klik projde do okna/plochy pod ním (dock nepřekáží) | – |
| 5.5 | Pod dockem je rozmazání sedící přesně na pásu | Ano, se zaoblenými rohy. Když místo toho uvidíš čtverec jinde nebo nic: Nastavení › Vzhled › Rozmazání docku › **Žádné** | řádky `DockBackdropWindow` |
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
| 6.1 | Pusť hudbu a minimalizuj okna | Dole na ploše tečky reagují na hudbu (ne na mikrofon). **Notebook na baterii:** vizualizér je pozastavený (režim výkonu) – připoj nabíječku nebo Nastavení › Výkon › „Vždy plné animace“ | řádky `Vizualizér`, `Výkon:` |
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
- **Skrytí ikon plochy skryje celou vrstvu ikon Windows** – zmizí tím i nabídka po pravém kliknutí na plochu
  (Zobrazit, Seřadit, Nový…). Vrátí se vypnutím „Skrýt ikony plochy“ nebo ukončením aplikace.
- **Rozmazání pod dockem** dělá samostatné okno, které WPF „nezobrazuje“ běžnou cestou (aby po startu neproblikl čtverec).
  Kdyby se nevykreslilo správně, záloha je Rozmazání docku › Žádné (dock pak má jen poloprůhledné pozadí).

---

# Fáze 2

> Fáze 2 stojí na fázi 1, která zatím také neběžela na Windows. U každé sekce je napsané, na kterých testech z fáze 1 závisí –
> když selžou ty, nemá smysl zkoušet navazující.

## 10. Režim výkonu a motivy *(závisí na 1, 2, 5)*

| # | Udělej | Má se stát | Když ne, pošli |
|---|---|---|---|
| 10.1 | Nastavení › Motiv › předvolby (Sklo, Grafit, Oceán…) | Mění se widgety (sklo, zaoblení), tón lišty a docku, barva zvýraznění | – |
| 10.2 | „Vytvořit kód“, zkopíruj, změň motiv, vlož kód do „Použít kód motivu“ | Vrátí se původní motiv | řádky `Motiv importován` |
| 10.3 | Vlož nesmysl (např. `ahoj`) | Červená hláška, nic se nezmění | – |
| 10.4 | Notebook: odpoj nabíječku | Nastavení › Výkon › Teď: „Ztlumené animace – Napájení z baterie“; dock se zvětšuje bez doběhu, vizualizér stojí | řádky `Výkon:` |
| 10.5 | Pusť video přes celou obrazovku a vrať se | Animace se ztlumí a pak vrátí | totéž |

## 11. Náhledy oken v docku *(závisí na 5.1–5.3)*

| # | Udělej | Má se stát | Když ne, pošli |
|---|---|---|---|
| 11.1 | Otevři 2 okna Průzkumníka, najeď myší na jeho ikonu v docku a chvíli počkej | Nad ikonou se objeví panel se **živými** náhledy obou oken a jejich titulky | řádky `Náhledy`, `DWM` |
| 11.2 | Přejeď myší z ikony na náhled | Náhled zůstane otevřený | – |
| 11.3 | Klikni na náhled | Přepne se na to okno a náhled zmizí | – |
| 11.4 | Křížek u náhledu | Okno se zavře (případně se zeptá na uložení) | – |
| 11.5 | Minimalizované okno | Náhled může být prázdný s popiskem „Minimalizováno“ (Windows u minimalizovaných oken obraz často nedává) | – |

## 12. Dynamic Island *(závisí na 1.1, 3.1 a stejném principu jako dock 5.4)*

| # | Udělej | Má se stát | Když ne, pošli |
|---|---|---|---|
| 12.1 | Bez hudby | Nahoře uprostřed nic (lišta normálně klikatelná) | – |
| 12.2 | Pusť hudbu (Spotify, YouTube v prohlížeči…) | Černá pilulka s obalem alba, názvem a proužky | řádky `Média` |
| 12.3 | Najeď na pilulku | Rozbalí se: obal, interpret, průběh, ovládání, hlasitost | – |
| 12.4 | Přehrát/pozastavit, další, předchozí | Funguje (u aplikací, které to podporují) | `Média: příkaz … odmítla` |
| 12.5 | Změň hlasitost klávesami / posuvníkem | Na chvíli se ukáže ukazatel hlasitosti | řádky `Hlasitost` |
| 12.6 | Je pilulka nad lištou (ne pod ní)? | Ano, i po změně měřítka / restartu Průzkumníka | – |

## 13. Start *(závisí na 1.5, 5.2)*

| # | Udělej | Má se stát | Když ne, pošli |
|---|---|---|---|
| 13.1 | Klikni na logo vlevo v liště | Uprostřed se otevře Start s rozmazaným pozadím a polem hledání s kurzorem | řádky `Start` |
| 13.2 | Seznam aplikací | Všechny aplikace (i ze Storu) s ikonami, abecedně | `Start: načteno N aplikací` |
| 13.3 | Napiš „kalk“ / „poznam“ / „vsc“ | Najde Kalkulačku, Poznámkový blok, Visual Studio Code (počáteční písmena) | – |
| 13.4 | Šipky + Enter, Esc, klik mimo | Spustí vybranou aplikaci / zavře Start | – |
| 13.5 | Pravé tlačítko na aplikaci › Připnout do docku | Objeví se v docku | – |
| 13.6 | Nastavení › Start › „Otevírat klávesou Windows“ **zapnout** | – | – |
| 13.7 | Stiskni a pusť klávesu Windows | Otevře se Start Better Windows (ne Start Windows); znovu = zavře | řádky `Háček klávesy Windows` |
| 13.8 | Win+E, Win+D, Win+L, Win+šipky | Fungují jako dřív | – |
| 13.9 | Po několika stiscích Win piš normálně | Žádné „zaseknuté“ zkratky (jako by byla Win držená) | řádek `Vložení uvolnění klávesy Win selhalo` |
| 13.10 | Klávesu Win v okně spuštěném jako správce (např. Správce úloh) | Otevře se normální Start Windows (to je správně) | – |
| 13.11 | Přepínač **vypnout** | Klávesa Windows otevírá Start Windows | – |

**Kdyby se klávesnice chovala divně** (všechno jako zkratka s Win): stiskni a pusť jednou klávesu Win; pak vypni „Otevírat klávesou Windows“ a pošli log.

**Popsaná obezlička:** Windows nedovolí oknu otevřenému z klávesového háčku převzít fokus. Start si ho bere přes `AttachThreadInput`
(dočasné propojení vstupu s oknem v popředí). Když to Windows přesto odmítne, Start se zavře a otevře se Start Windows (log: `Start: nepodařilo se získat fokus`).

---

# Familiar: zamykací obrazovka, lišta, dock

## 14. Zamykací obrazovka *(závisí na 1.5 a 2.8 – tapeta)*

**Záchrana, kdyby zámek nešel zavřít:** stiskni znovu **Win+Shift+L**, nebo **Win+L** a odemkni Windows (zámek se po odemknutí sám zavře).

| # | Udělej | Má se stát | Když ne, pošli |
|---|---|---|---|
| 14.1 | Win+Shift+L (kdyby nereagovala: Start › Zamknout) | Přes celou obrazovku: tapeta, datum, velké hodiny, počasí, baterie, hudba (když hraje), tvoje jméno a obrázek účtu | řádky `Zámek:` (i `zkratku Win+Shift+L už používá jiný program`) |
| 14.2 | Klikni nebo stiskni klávesu (ne hlasitost/média) | Obrazovka zmizí (s animací, pokud nejsou ztlumené animace) a **zamkne se Windows** (heslo / PIN / Hello) | `LockWorkStation selhalo` |
| 14.3 | Odemkni Windows | Jsi zpátky na ploše, naše obrazovka už nesvítí | – |
| 14.4 | Start › Zamknout | Totéž jako 14.1 | – |
| 14.5 | Na zamykací obrazovce pozastav / přeskoč hudbu (tlačítky i klávesami médií) a změň hlasitost | Funguje a obrazovka se tím neodemkne | – |
| 14.5b | Na zamykací obrazovce stiskni znovu Win+Shift+L | Zavře se (jako odemknutí) | – |
| 14.6 | Nastavení › Zamykací obrazovka › Upravit | Zámek s panelem editoru vpravo; změny stylu hodin, umístění, rozmazání se hned ukazují; Hotovo uloží, Zrušit ne; v editoru klik neodemyká | – |
| 14.7 | „Po odemknutí zamknout Windows“ vypnout, Win+Shift+L, klik | Jen zmizí (bez zámku Windows) | – |
| 14.7b | Nastavení › Zamykací obrazovka › Náhled, klik | Zmizí, Windows se **nezamkne** | – |
| 14.8 | „Ukázat po nečinnosti“ = 1 min, nech počítač být | Po ~1 min se ukáže; **ne** když hraje hudba nebo video | – |

**Není to zabezpečení:** naši obrazovku jde obejít (Ctrl+Alt+Del, Alt+Tab). Chrání až zámek Windows, proto je „Po odemknutí zamknout Windows“ ve výchozím stavu zapnuté.
Win+L si Windows vyhrazuje (zamkne hned, naše obrazovka se neukáže) – proto Win+Shift+L. Na dalších monitorech je zatím vidět plocha.

## 15. Lišta jako Familiar *(závisí na 1.1, 12.2)*

| # | Udělej | Má se stát |
|---|---|---|
| 15.1 | Pusť hudbu | Vedle názvu aplikace mini přehrávač (obal, název, interpret, ⏸, ⏭) – tlačítka fungují |
| 15.2 | Přepni jazyk klávesnice (Win+mezerník / Alt+Shift) v nějaké aplikaci | V liště se změní kód (CES ↔ ENG) do ~1 s |
| 15.3 | Uživatel | Tvoje jméno (u účtu Microsoft celé jméno) a obrázek účtu |
| 15.4 | Lupa | Otevře Start |
| 15.5 | „Ukončit“ | Napoprvé „Opravdu ukončit?“ (červeně), napodruhé se aplikace ukončí a vše se vrátí (ikony, hlavní panel) |

## 16. Dock jako Familiar *(závisí na 5.1)*

| # | Udělej | Má se stát |
|---|---|---|
| 16.1 | První ikona (barevná mřížka) | Otevře Start (Launchpad) |
| 16.2 | Poslední ikona – Koš | Prázdný / s papíry podle obsahu (do ~10 s po smazání souboru); klik otevře Koš |
| 16.3 | Pravé tlačítko na Koš › Vysypat koš… | Windows se zeptá a koš vysype |

---

# Přístupnost a soulad s Apple HIG

## 17. Nastavení Windows, která musí shell respektovat *(závisí na 1, 2, 5)*

| # | Udělej | Má se stát |
|---|---|---|
| 17.1 | Nastavení Windows › Přizpůsobení › Barvy › **Efekty průhlednosti vypnout** | Lišta, dock a widgety bez rozmazání, s neprůhledným pozadím; text dobře čitelný |
| 17.2 | Totéž zapnout zpět | Rozmazání se vrátí |
| 17.3 | Usnadnění › **Kontrastní motivy** zapnout | Bez rozmazání, silnější okraje; nic nezmizí |
| 17.4 | Přizpůsobení › Barvy › **Barva zvýraznění** změnit (např. na červenou) | Do pár sekund se změní zvýraznění v Better Windows (dnešek v kalendáři, přepínače, vteřinová ručička) – pokud je v Nastavení › Motiv zapnuté „Barva zvýraznění podle Windows“ (výchozí) |
| 17.5 | Usnadnění › Vizuální efekty › **Efekty animace vypnout** | Animace se ztlumí (zvětšování docku bez doběhu, ostrůvek bez přechodů) |
| 17.6 | Tečky mikrofonu / kamery | Kamera je zaoblený čtvereček, mikrofon kolečko (liší se tvarem, ne jen barvou) |
| 17.7 | Pravé tlačítko na ikonu běžící aplikace v docku | Nabídka začíná seznamem otevřených oken; klik přepne na okno |

## 18. Animace *(závisí na 5, 12, 13)*

Ideálně vyzkoušej na 60Hz i na 144Hz (nebo jiném rychlém) monitoru – rychlost má být stejná.

| # | Udělej | Má se stát |
|---|---|---|
| 18.1 | Pusť hudbu, najeď myší na ostrůvek a zase odjeď; zkus to i rychle tam a zpět | Jako v referenční nahrávce (`docs/screenshots/animace/ostrovek-vs-reference.mp4`): pilulka se nafoukne, stáhne, roztáhne s rozmazaným obsahem, který se zaostří; obsah lišty se rozmaže. Při sbalení se pilulka krátce zúží a „zhoupne“. Při rychlé změně plynule otočí směr bez cuknutí |
| 18.2 | Sleduj okraj ostrůvku během roztahování | Okraj ani stín se **neořezávají** (rovná hrana uprostřed animace = chyba, napiš mi) |
| 18.3 | Během roztahování ostrůvku (první ~0,5 s) klikni na lištu těsně vedle něj | Klik může krátce „spolknout“ průhledná rezerva okolo ostrůvku – po dosednutí už ne. Po odjetí myší (sbalení) se lišta dá klikat hned |
| 18.4 | Otevři Start (Launchpad) | Obsah se lehce zvětší a objeví; rozmazané pozadí okna se objeví naráz (to jinak nejde – kreslí ho Windows) |
| 18.5 | Zavři Start: Esc, klik mimo, spuštění aplikace, ikona Launchpad znovu | Obsah zmizí a na ~0,17 s zůstane prázdný rozmazaný rám, pak se okno schová. **Posuď, jestli to nevypadá hůř než okamžité zmizení** – pak to přepnu na okamžité |
| 18.6 | Otevři Start a hned ho zavři a znovu otevři (rychle 2–3×) | Start se nezasekne zavřený ani prázdný; po posledním otevření je vidět a dá se psát |
| 18.7 | Spusť z docku nespuštěnou aplikaci | Ikona skáče (stejně vysoko) až do naběhnutí, pak dopadne – neskočí dolů naráz; vršek ikony se neořízne |
| 18.8 | Přejeď myší přes dock na 60Hz a na 144Hz monitoru | Zvětšování naběhne a dozní stejně rychle, bez trhání |
| 18.8b | Při přejíždění docku sleduj okraje pásu | Rozmazání pod pásem drží krok s pásem; když viditelně zaostává nebo bliká o snímek, napiš mi (C# ho posouvá každý snímek) |
| 18.8c | Najeď na dock a klikni do okna aplikace těsně nad dockem | Dokud je myš na docku, klik může spolknout průhledná rezerva pro zvětšené ikony; po odjetí z docku (~0,3 s) už ne |
| 18.9 | Vypni Efekty animace (viz 17.5) a zopakuj 18.1, 18.4, 18.7 | Vše bez animací: ostrůvek mění tvar hned, Start se objeví hned, ikona neskáče |

Světlý / tmavý režim už nejde přepnout v Better Windows – řídí se Windows (Apple: aplikace nemá mít vlastní přepínač vzhledu).
