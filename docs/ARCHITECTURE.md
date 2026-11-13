# Architektura

Jeden proces (`BetterWindows.exe`), víc oken. Každé okno = WPF okno s průhledným WebView2, ve kterém běží jedna stránka z `ui/`.

## Vrstvy oken (odspodu nahoru)

| Vrstva | Okna | Rozmazání | Stav |
|---|---|---|---|
| **DesktopLayer** – pod všemi aplikacemi | jedno okno přes celý monitor: tapeta, widgety, vizualizér | CSS `backdrop-filter` nad tapetou ve stejné stránce | `DesktopWindow` |
| Aplikace Windows | – | – | – |
| **Nad aplikacemi** (topmost, bez fokusu, mimo Alt+Tab) | horní lišta (AppBar), dock + okno pozadí docku, Dynamic Island | systémové (DWM) | `TopBarWindow`, `DockWindow`, `DockBackdropWindow`, `IslandWindow` |
| **Vyskakovací** | nabídka Start (bere fokus), náhledy oken (DWM thumbnail), kontextové nabídky | systémové | `StartWindow`, `PreviewWindow` |
| **Nad vším** | zamykací obrazovka Better Windows (před zámkem Windows) | CSS nad tapetou | `LockScreenWindow` |

Proč dva druhy rozmazání: CSS `backdrop-filter` vidí jen obsah vlastní stránky. Pro widgety je to v pořádku, protože tapetu kreslí stejná stránka. Lišta a dock leží nad cizími okny, ta umí rozmazat jen systém (DWM).

## C# (`src/BetterWindows` + `src/BetterWindows.Logic`)

**BetterWindows.Logic** (net8.0, bez WPF, testy v `tests/`): model nastavení a jeho ukládání, typy zpráv,
rozhodování docku (pořadí ikon, klik = spustit / přepnout / minimalizovat), FFT a pásma vizualizéru,
parsování Open-Meteo a ConsentStore, stav změn systému a plán obnovy, kodek motivů (nedůvěryhodný vstup),
stavový automat klávesy Windows, převod aplikací ze Startu na identitu, pravidla režimu výkonu.

**BetterWindows** (WPF):

| Složka | Co obsahuje |
|---|---|
| `Core/` | AppInfo, Log, CrashGuard, Safe (pojistka funkcí), SingleInstance, SystemGuard (pojistka změn systému), Watchdog, ShellController (propojení všeho) |
| `Bridge/` | MessageHub (ústředna zpráv, sticky stav), MessageBridge (jedno WebView2) |
| `Native/` | obaly nad Win32: Dwm, AccentPolicy, WindowStyles, ExplorerWindows, DesktopEmbedder, AppBarNative, ShellItems, WindowInfo, ShellMessageWindow |
| `Monitors/` | MonitorInfo (fyzické pixely + DPI), MonitorService |
| `Services/` | Settings, Theme, Wallpaper, DesktopLayerHost, WindowTracker, AppIdentityResolver, IconService, AppLauncher, DockController, WindowPreviewController, Battery, Weather, PrivacyIndicator, AudioVisualizer, Taskbar, Performance, Media, Volume, IslandController, StartApps, StartController, LockScreenController, User, KeyboardLayout, Trash |
| `ShellWindows/` | ShellWindowBase, TopBarWindow, DesktopWindow, DockWindow, DockBackdropWindow, SettingsWindow |
| `Themes/` | vzhled nativní kontextové nabídky (světlý/tmavý) |
| `Tray/` | ikona v oznamovací oblasti |

Zásady:
- **Okna se vytvářejí pro konkrétní monitor** (`MonitorInfo`). Víc monitorů = víc instancí oken.
- **Pozice ve fyzických pixelech** přes `SetWindowPos`, výšky/šířky v DIP převádí `MonitorInfo.ToPixels`.
- **Motiv má jeden zdroj**: `ThemeService` → tón DWM rozmazání + `PreferredColorScheme` ve WebView2 + nativní nabídky.
- **Změny systému se zapisují předem** (`SystemGuard.Record`) a vrací se při ukončení, pádu, zabití procesu (watchdog) i dalším startu.
- **Chyba jedné funkce nesmí shodit shell**: služby se spouští přes `Safe.Run`, obsluhy zpráv a háčky mají vlastní try/catch.
- **Cizí okna jen neblokujícími voláními** (`ShowWindowAsync`, `SendMessageTimeout`, `PostMessage`).
- **Jeden zdroj pravdy pro animace**: `PerformanceService` → `performance.state` → `data-motion="reduced"` (všechny stránky) + pauza vizualizéru.
- **Klávesa Windows**: háček ve vlastním vlákně, stisk nikdy nezahazuje, zahozené uvolnění vždy nahradí; výchozí vypnuto.

## Zprávy C# ↔ JS

Tvar `{ type, payload }`. Typy: `ui/src/lib/bridge/messages.ts` ⇄ `src/BetterWindows.Logic/Messaging/Messages.cs`.
Stav (nastavení, baterie, počasí…) je „sticky“: stránka po načtení pošle `ui.ready` a dostane poslední hodnoty.

## UI (`ui/`)

| Cesta | Co obsahuje |
|---|---|
| `<část>.html` | jedna stránka = jedno okno (`topbar`, `dock`, `desktop`, `settings`; později `island`, `start`) |
| `src/apps/<část>/` | kořenová komponenta dané části |
| `src/lib/components/` | sdílené komponenty (Surface, Icon, Clock, Visualizer, formulářové prvky v `forms/`) |
| `src/lib/widgets/` | widgety (hodiny, kalendář, počasí, připomínky, baterie) |
| `src/lib/bridge/` | typy zpráv a most do C# |
| `src/lib/styles/tokens.css` | **jediné místo** pro barvy, průhlednost, blur, zaoblení, stíny, písmo (světlý i tmavý motiv) |
| `src/lib/stores/` | sdílený stav: `host` (vše z C#), `clock`, `visualizer`, `kv` (data widgetů) |

Font Inter je přibalený lokálně (`@fontsource-variable/inter`), nic se nestahuje z internetu.

## Směr designu

- **Produkt:** shell ve stylu macOS nad Windows 11 (lišta, dock, ostrůvek, widgety nad tapetou) pro toho, kdo chce klidnější a osobnější plochu.
- **Charakter:** klidný, přesný, noční.
- **Svět:** vlastní plocha v noci – fotka tapety za matným sklem, ciferník hodinek, ukazatel hi-fi.
- **Momenty:** tapeta prosvítající sklem widgetů; tečkový vizualizér a hodiny na zamykací obrazovce. Jinde je chrom tichý.
- **Podpis:** tečková matice – vizualizér, Launchpad (3×3), indikátor přehrávání v ostrůvku, značka (lišta + tři tečky). Nikde jinde.

Pravidla: jedna grafitová škála (`tokens.css`), tři role textu, jeden akcent (výběr, hlavní akce, dnešek, vteřinová ručička),
významové barvy jen s významem, žádné barevné přechody ani záře, váhy písma 400–600, 4px mřížka, vnořené zaoblení,
jedna křivka pohybu `cubic-bezier(0.22, 1, 0.36, 1)` pro drobné přechody, žádné smyčky bez důvodu.

### Pohyb

- **Pružina** jen tam, kde se mění tvar nebo něco přilétá: ostrůvek (`SpringBox` v `src/lib/motion.svelte.ts`,
  response 0,42 s, tlumení 0,8 → překmit ~1 %) a otevření Startu (stejná křivka jako CSS `--bw-spring` = `linear()`).
  Zavírání je krátké, bez pružení (`--bw-ease-in`).
- **Nezávislé na obnovovací frekvenci:** pružina se počítá v pevných krocích 1/240 s, zvětšování docku exponenciálním
  doběhem podle skutečného času (`follow`, τ 67 ms). `Spring` ze `svelte/motion` se nepoužívá – na 144 Hz běží rychleji.
- **Ostrůvek a oblast okna:** při roztahování se do C# hned pošle cílová velikost + rezerva na překmit (rostoucí okraj
  se neořízne zpožděním zprávy), při zmenšování oblast sleduje pilulku (nepolyká kliknutí na lištu), po dosednutí přesný tvar.
- **Start:** animuje se jen obsah (97 % → 100 %, průhlednost). Systémové rozmazání a rám kreslí Windows pro celé okno,
  ty se objeví naráz. Okno schová C# až po zavírací animaci (170 ms, vlastní časovač; při celé obrazovce/vypnutí hned).
- **Poskakování v docku:** stejně vysoké skoky s gravitačním průběhem (skáče obrázek, ne tlačítko), po naběhnutí aplikace doběhne rozběhnutý skok.
- **Omezení pohybu** (Windows „Efekty animace“, režim výkonu): CSS délky → 0, pružiny `instant`, poskakování vypnuté.
- **Nejde:** animace cizích oken (minimalizace do docku apod.) kreslí Windows, shell je nemůže nahradit.

## Kontroly bez Windows

- `dotnet test` – logika (nastavení, dock, FFT, parsování, obnova systému) + vygeneruje `ui/harness/fixtures.json`.
- `npm run harness` (v `ui/`) – vykreslí všechny stránky v headless Chromiu se zprávami z C#, ve světlém i tmavém režimu,
  vyzkouší zvětšení docku a přepínače nastavení; selže při chybě v konzoli.

## Rizika

Viz [TESTING.md](TESTING.md) – sekce „Kde jsou známá rizika“.
