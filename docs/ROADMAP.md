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

## Další fáze (po dokončení fáze 1)

1. **Náhledy oken v docku** při najetí myší (`DwmRegisterThumbnail`, nativní překryvné okno nad WebView).
2. **Nabídka Start** na klávesu Windows s vyhledáváním aplikací (low-level keyboard hook).
3. **Dynamic Island** – samostatné malé okno nahoře uprostřed (černá pilulka, obal alba, název, proužky). Právě hrající hudba (`GlobalSystemMediaTransportControlsSessionManager`), hlasitost, oznámení. Horní lišta pro něj už nechává volné místo (`--bw-island-width`).
4. **Motivy** se sdílením přes kód (export/import jako text) – přepisují designové tokeny.
5. **Nastavení výkonu** – ztlumit animace při hře přes celou obrazovku nebo na baterce (`data-motion="reduced"` už v tokenech je).
6. **Zamykací obrazovka s editorem.** Skutečnou zamykací obrazovku Windows nahradit nejde, reálná je jen vlastní obrazovka přes celou plochu zobrazená před tou systémovou. **Než se začne psát kód, nejdřív vysvětlit, co reálně jde a co ne.**
