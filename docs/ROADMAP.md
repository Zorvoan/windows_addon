# Roadmapa

Architektura a vrstvy oken: viz [ARCHITECTURE.md](ARCHITECTURE.md).

## Fáze 1

Všechny kroky jsou **naprogramované a sestavené, ale zatím neotestované na Windows** – testovací seznam je v [TESTING.md](TESTING.md).

- [x] **0. Kostra** – řešení, Vite + Svelte, jedna instance, ikona v oznamovací oblasti, CrashGuard, log *(otestováno)*
- [ ] **1. Horní lišta** – okno nad aplikacemi, systémové rozmazání (3 režimy), designové tokeny, světlý/tmavý *(čeká na test)*
- [ ] **2. DesktopLayer** ⚠️ – vložení do plochy (klasicky / 24H2+), záloha HWND_BOTTOM, skrytí ikon s obnovou, tapeta, testovací karta *(čeká na test)*
- [ ] **3. AppBar** – rezervace horního okraje, skrytí při aplikaci přes celou obrazovku *(čeká na test)*
- [ ] **4. Most C# ↔ JS + aktivní aplikace** – typované zprávy, WindowTracker, identita aplikací (exe / AUMID) *(čeká na test)*
- [ ] **5. Baterie, počasí** (Open-Meteo) *(čeká na test)*
- [ ] **6. Mikrofon a kamera** (ConsentStore) *(čeká na test)*
- [ ] **7. UI docku** – zvětšování, region okna *(čeká na test)*
- [ ] **8. Spuštěné aplikace v docku** – seznam oken, ikony *(čeká na test)*
- [ ] **9. Připnuté aplikace, klik, kontextová nabídka** *(čeká na test)*
- [ ] **10. Systémové rozmazání docku** – samostatné okno pod pásem *(čeká na test)*
- [ ] **11. Vizualizér zvuku** – WASAPI loopback + FFT, scény tečky / vlna / kruh *(čeká na test)*
- [ ] **12. Widgety na ploše** – hodiny, kalendář, počasí, připomínky, baterie, režim úprav *(čeká na test)*
- [ ] **13. Okno nastavení** *(čeká na test)*
- [ ] **14. Skrytí hlavního panelu** – výchozí vypnuto, pojistka, watchdog *(čeká na test)*

### Konflikt: dock se systémovým rozmazáním × zvětšování ikon

Systémové rozmazání (DWM) se vždy kreslí pod **celým obdélníkem okna**. Zvětšené ikony ale potřebují okno vyšší než viditelný dock, takže by se rozmazávalo i prázdné místo nad ním. Řešení zůstává podle schváleného plánu:

- okno s ikonami je průhledné (bez rozmazání) a kliknutí mimo dock propouští (`SetWindowRgn`),
- rozmazané pozadí dělá **samostatné okno pod ním**, velké přesně jako viditelný pás docku.

Pokud by se pozadí při animaci šířky viditelně opožďovalo, dostane pevnou šířku. (Implementováno: DockBackdropWindow.)

## Fáze 2

Naprogramováno a sestaveno, **neotestováno na Windows** (testy 10–13 v [TESTING.md](TESTING.md)).

- [ ] **Náhledy oken v docku** – DwmRegisterThumbnail v nativním okně nad ikonou *(čeká na test)*
- [ ] **Start / Launchpad** – shell:AppsFolder, hledání; klávesa Windows volitelně (výchozí vypnuto) *(čeká na test)*
- [ ] **Dynamic Island** – hudba (GSMTC) a hlasitost *(čeká na test)*; oznámení cizích aplikací **nejsou možná** bez identity balíčku (MSIX)
- [ ] **Motivy se sdílením přes kód** – `BW1.…`, kontrolovaný import *(čeká na test)*
- [ ] **Režim výkonu** – ztlumení animací na baterii / při celé obrazovce, pauza vizualizéru *(čeká na test)*
- [ ] **Zamykací obrazovka s editorem** – vlastní obrazovka před zámkem Windows (Win+Shift+L, Start, volitelně nečinnost), po odemknutí zámek Windows *(čeká na test)*
- [ ] **Vzhled jako Familiar** – mini přehrávač, jazyk klávesnice, uživatel a Ukončit v liště; Launchpad a Koš v docku *(čeká na test)*

### Zamykací obrazovka – co reálně jde

- **Nejde:** nahradit zamykací ani přihlašovací obrazovku Windows. Ta běží na zabezpečené ploše (Winlogon), kam se běžná aplikace nedostane;
  od Windows Vista neexistuje podporovaný způsob, jak ji vyměnit. Vlastní přihlašovací obrazovka by znamenala Credential Provider (DLL v C++,
  práva správce) – a i ten mění jen dlaždici přihlášení, ne celý vzhled.
- **Jde (oficiálně):** obrázek zamykací obrazovky (API `LockScreen.SetImageFileAsync` – podle dokumentace jen pro aplikace s identitou balíčku, ověřit),
  případně stav aplikací na zamykací obrazovce.
- **Jde (vlastní):** celoobrazovkové okno Better Windows (hodiny, widgety, hudba), které se ukáže **před** systémovým zámkem:
  - na vlastní zkratku nebo po nečinnosti; po odemčení „klikem“ zavolat `LockWorkStation` → teprve pak se objeví skutečný zámek Windows (heslo/PIN/Hello);
  - nebo po odemčení Windows na chvíli (uvítací obrazovka).
  - **Není to zabezpečení** – okno jde obejít (Alt+Tab, Ctrl+Alt+Del, Správce úloh). Chrání jen skutečný zámek Windows.
  - Nezobrazí se na obrazovce po startu počítače ani při Win+L přímo (to je zabezpečená plocha).
- **Editor:** rozvržení prvků (hodiny, datum, hudba, widgety) – stejné komponenty jako widgety na ploše.

**Rozhodnuto (podle Familiar):** vlastní obrazovka před zámkem Windows – otevře ji Win+Shift+L, tlačítko Zamknout ve Startu
nebo volitelně nečinnost; „odemknutí“ zavolá zámek Windows (vypnutelné). Editor: styl a umístění hodin, prvky, rozmazání a ztmavení tapety.

**Záměrně ne:** globální nabídky aplikací v liště (Soubor, Zobrazit…) – většina dnešních aplikací ve Windows žádnou čitelnou
nabídku nemá (Chromium, Electron, UWP, WinUI), takže by to fungovalo jen u pár starších programů.

## Původní seznam dalších fází

1. **Náhledy oken v docku** při najetí myší (`DwmRegisterThumbnail`, nativní překryvné okno nad WebView).
2. **Nabídka Start** na klávesu Windows s vyhledáváním aplikací (low-level keyboard hook).
3. **Dynamic Island** – samostatné malé okno nahoře uprostřed (černá pilulka, obal alba, název, proužky). Právě hrající hudba (`GlobalSystemMediaTransportControlsSessionManager`), hlasitost, oznámení. Horní lišta pro něj už nechává volné místo (`--bw-island-width`).
4. **Motivy** se sdílením přes kód (export/import jako text) – přepisují designové tokeny.
5. **Nastavení výkonu** – ztlumit animace při hře přes celou obrazovku nebo na baterce (`data-motion="reduced"` už v tokenech je).
6. **Zamykací obrazovka s editorem.** Skutečnou zamykací obrazovku Windows nahradit nejde, reálná je jen vlastní obrazovka přes celou plochu zobrazená před tou systémovou. **Než se začne psát kód, nejdřív vysvětlit, co reálně jde a co ne.**
