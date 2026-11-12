# Architektura

Jeden proces (`BetterWindows.exe`), víc oken. Každé okno = WPF okno s průhledným WebView2, ve kterém běží jedna stránka z `ui/`.

## Vrstvy oken (odspodu nahoru)

| Vrstva | Okna | Rozmazání | Stav |
|---|---|---|---|
| **DesktopLayer** – pod všemi aplikacemi | jedno okno přes celý monitor: tapeta, widgety, scény vizualizéru | CSS `backdrop-filter` nad tapetou ve stejné stránce | krok 2 |
| Aplikace Windows | – | – | – |
| **Nad aplikacemi** (topmost, bez fokusu, mimo Alt+Tab) | horní lišta, dock (+ okno pozadí docku), Dynamic Island | systémové (DWM) | lišta: krok 1 |
| **Vyskakovací** | nabídka Start, náhledy oken, kontextové nabídky | dle potřeby | další fáze |

Proč dva druhy rozmazání: CSS `backdrop-filter` vidí jen obsah vlastní stránky. Pro widgety je to v pořádku, protože tapetu kreslí stejná stránka. Lišta a dock leží nad cizími okny, ta umí rozmazat jen systém (DWM).

## C# (`src/BetterWindows`)

| Složka | Co obsahuje |
|---|---|
| `Core/` | AppInfo (cesty), Log, CrashGuard (pády + nouzový úklid), SingleInstance, AppIcons |
| `Native/` | tenké obaly nad Win32: Dwm, AccentPolicy, WindowStyles, BackdropKind |
| `Monitors/` | MonitorInfo (fyzické pixely + DPI), MonitorService |
| `Services/` | ThemeService; později WindowTracker, IconService, PrivacyIndicatorService, SettingsService, WeatherService, … |
| `ShellWindows/` | ShellWindowBase (společný základ oken nad aplikacemi), TopBarWindow, SettingsWindow |
| `Web/` | WebViewHost – sdílené prostředí WebView2, virtuální host `https://app.betterwindows/`, světlý/tmavý režim |
| `Tray/` | ikona v oznamovací oblasti |

Zásady:
- **Okna se vytvářejí pro konkrétní monitor** (`MonitorInfo`). Víc monitorů = víc instancí oken.
- **Pozice ve fyzických pixelech** přes `SetWindowPos`, výšky/šířky v DIP převádí `MonitorInfo.ToPixels`.
- **Motiv má jeden zdroj**: `ThemeService` → tón DWM rozmazání + `PreferredColorScheme` ve WebView2.
- **Změny systému musí jít vrátit** i při pádu: `CrashGuard.RegisterEmergencyCleanup`.

## UI (`ui/`)

| Cesta | Co obsahuje |
|---|---|
| `<část>.html` | jedna stránka = jedno okno (`settings`, `topbar`, později `desktop`, `dock`, `island`, `start`) |
| `src/apps/<část>/` | kořenová komponenta dané části |
| `src/lib/components/` | sdílené komponenty – použijí je lišta, dock, Start, widgety i nastavení |
| `src/lib/styles/tokens.css` | **jediné místo** pro barvy, průhlednost, blur, zaoblení, stíny, písmo (světlý i tmavý motiv) |
| `src/lib/stores/` | sdílený stav (např. `clock`) |

Font Inter je přibalený lokálně (`@fontsource-variable/inter`), nic se nestahuje z internetu.

## Rizika podle vrstev

- **Nad aplikacemi:** systémový Acrylic na okně, které nikdy není aktivní – ověřuje krok 1 (testovací přepínač v oznamovací oblasti).
- **DesktopLayer:** Windows 11 24H2 změnil hierarchii oken plochy (WorkerW už není samostatné okno nejvyšší úrovně, ale potomek Progman). Krok 2 vyzkouší obě varianty, záložně HWND_BOTTOM, a zaloguje, co na daném systému funguje. Otevřená otázka: jestli okno vložené do plochy dostává kliknutí myší (pro widgety).
