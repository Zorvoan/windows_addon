# Roadmapa

## Fáze 1

- [x] **0. Kostra** – řešení, Vite + Svelte, jedna instance, ikona v oznamovací oblasti, CrashGuard, log
- [ ] **1. Okno horní lišty** – bezrámečkové, topmost, mimo Alt+Tab, průhledné WebView2 + DWM Acrylic, světlý/tmavý režim
- [ ] **2. AppBar** – registrace přes `SHAppBarMessage`, DPI
- [ ] **3. Most C# ↔ JS + aktivní aplikace** – typované zprávy, `WindowTracker` (okno v popředí)
- [ ] **4. Hodiny, datum, baterie, počasí** (Open-Meteo)
- [ ] **5. Indikátory mikrofonu a kamery** (`CapabilityAccessManager\ConsentStore`)
- [ ] **6. UI docku** – zvětšování ikon, propustnost kliknutí mimo dock (`SetWindowRgn`)
- [ ] **7. Spuštěné aplikace v docku** – seznam oken, identita aplikace (exe / AUMID), ikony
- [ ] **8. Připnuté aplikace, klik, kontextová nabídka**
- [ ] **9. Rozmazané pozadí docku** (pokus, viz rizika v plánu)
- [ ] **10. Okno nastavení** – město, připnuté aplikace, přepínač hlavního panelu
- [ ] **11. Skrytí hlavního panelu Windows** – watchdog, obnova při pádu i dalším startu

## Další fáze (po dokončení kroků 0–11)

1. **Náhledy oken v docku** při najetí myší (`DwmRegisterThumbnail`, nativní překryvné okno nad WebView).
2. **Nabídka Start** na klávesu Windows s vyhledáváním aplikací (low-level keyboard hook).
3. **Dynamic Island** – právě hrající hudba (`GlobalSystemMediaTransportControlsSessionManager`), hlasitost, oznámení.
4. **Vizualizér zvuku** se scénami reagujícími na zvuk z PC (WASAPI loopback, ne mikrofon).
5. **Widgety na plochu** – počasí, kalendář, připomínky.
6. **Motivy** se sdílením přes kód (export/import jako text).
7. **Nastavení výkonu** – ztlumit animace při hře přes celou obrazovku nebo na baterce.
8. **Zamykací obrazovka s editorem.** Skutečnou zamykací obrazovku Windows nahradit nejde, reálná je jen vlastní obrazovka přes celou plochu zobrazená před tou systémovou. **Než se začne psát kód, nejdřív vysvětlit, co reálně jde a co ne.**
