# Roadmapa

Architektura a vrstvy oken: viz [ARCHITECTURE.md](ARCHITECTURE.md).

## Fáze 1

- [x] **0. Kostra** – řešení, Vite + Svelte, jedna instance, ikona v oznamovací oblasti, CrashGuard, log
- [ ] **1. Horní lišta** _(hotovo, čeká na test na Windows)_ – bezrámečkové okno nad aplikacemi, mimo Alt+Tab, bez kradení fokusu, systémové rozmazání (Win11 Acrylic / API z Win10 / žádné – testovací přepínač), designové tokeny (Inter, 22px, světlý/tmavý), statické rozvržení s volným místem pro Dynamic Island
- [ ] **2. DesktopLayer** ⚠️ rizikový bod – okno přes celý monitor POD aplikacemi (WorkerW trik, záložně HWND_BOTTOM), skrytí ikon plochy (vratně: obnova při ukončení, pádu i zabití procesu), tapeta (výchozí = aktuální tapeta Windows), jedna testovací skleněná karta s CSS `backdrop-filter`
- [ ] **3. AppBar** – horní lišta registrovaná přes `SHAppBarMessage`, maximalizovaná okna pod ni nezajedou
- [ ] **4. Most C# ↔ JS + aktivní aplikace** – typované zprávy, `WindowTracker` (okno v popředí)
- [ ] **5. Baterie, počasí** (Open-Meteo); hodiny a datum už běží v JS od kroku 1
- [ ] **6. Indikátory mikrofonu a kamery** (`CapabilityAccessManager\ConsentStore`)
- [ ] **7. UI docku** – zvětšování ikon, propustnost kliknutí mimo dock (`SetWindowRgn`)
- [ ] **8. Spuštěné aplikace v docku** – seznam oken, identita aplikace (exe / AUMID), ikony
- [ ] **9. Připnuté aplikace, klik, kontextová nabídka**
- [ ] **10. Systémové rozmazání docku** – viz konflikt níže
- [ ] **11. Vizualizér zvuku** – WASAPI loopback + FFT v C#, tečky na canvasu ve DesktopLayer
- [ ] **12. Widgety na ploše** – počasí, kalendář, připomínky, hodiny (sklo nad tapetou)
- [ ] **13. Okno nastavení** – tapeta, město, připnuté aplikace, přepínač hlavního panelu
- [ ] **14. Skrytí hlavního panelu Windows** – watchdog, obnova při pádu i dalším startu

### Konflikt: dock se systémovým rozmazáním × zvětšování ikon

Systémové rozmazání (DWM) se vždy kreslí pod **celým obdélníkem okna**. Zvětšené ikony ale potřebují okno vyšší než viditelný dock, takže by se rozmazávalo i prázdné místo nad ním. Řešení zůstává podle schváleného plánu:

- okno s ikonami je průhledné (bez rozmazání) a kliknutí mimo dock propouští (`SetWindowRgn`),
- rozmazané pozadí dělá **samostatné okno pod ním**, velké přesně jako viditelný pás docku.

Pokud by se pozadí při animaci šířky viditelně opožďovalo, dostane pevnou šířku.

## Další fáze (po dokončení fáze 1)

1. **Náhledy oken v docku** při najetí myší (`DwmRegisterThumbnail`, nativní překryvné okno nad WebView).
2. **Nabídka Start** na klávesu Windows s vyhledáváním aplikací (low-level keyboard hook).
3. **Dynamic Island** – samostatné malé okno nahoře uprostřed (černá pilulka, obal alba, název, proužky). Právě hrající hudba (`GlobalSystemMediaTransportControlsSessionManager`), hlasitost, oznámení. Horní lišta pro něj už nechává volné místo (`--bw-island-width`).
4. **Motivy** se sdílením přes kód (export/import jako text) – přepisují designové tokeny.
5. **Nastavení výkonu** – ztlumit animace při hře přes celou obrazovku nebo na baterce (`data-motion="reduced"` už v tokenech je).
6. **Zamykací obrazovka s editorem.** Skutečnou zamykací obrazovku Windows nahradit nejde, reálná je jen vlastní obrazovka přes celou plochu zobrazená před tou systémovou. **Než se začne psát kód, nejdřív vysvětlit, co reálně jde a co ne.**
