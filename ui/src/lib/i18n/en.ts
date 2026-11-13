/**
 * Anglické texty. Klíč = český text tak, jak je ve zdrojáku (t('…')), hodnota = překlad.
 * Chybějící klíč se ukáže česky – harness (npm run harness) proto hlídá, že tu je každý t('…').
 * {jméno} = proměnná, doplní ji t(text, { jméno: hodnota }).
 */
export const en: Record<string, string> = {
  // Společné
  'Zapnuto': 'On',
  'Vypnuto': 'Off',
  'Zrušit': 'Cancel',
  'Hotovo': 'Done',
  'Upravit': 'Edit',
  'Otevřít': 'Open',
  'Zavřít': 'Close',
  'Použít': 'Apply',
  'Obnovit': 'Refresh',
  'Skrýt': 'Hide',
  'Kopírovat': 'Copy',
  'Náhled': 'Preview',
  'Automaticky': 'Automatic',
  'Stav': 'Status',
  'Režim': 'Mode',
  'Detail': 'Details',
  'Data': 'Data',
  'Verze': 'Version',
  'Teď': 'Now',
  'nikdy': 'never',
  'chyba': 'error',
  'nedostupné': 'unavailable',
  'Selhalo': 'Failed',

  // Hudba a hlasitost
  'Předchozí': 'Previous',
  'Další': 'Next',
  'Pozastavit': 'Pause',
  'Přehrát': 'Play',
  'Hlasitost': 'Volume',
  'Ztlumit': 'Mute',
  'Zapnout zvuk': 'Unmute',
  'Ztlumeno': 'Muted',
  'Hudba': 'Music',
  'Právě hraje': 'Now Playing',

  // Horní lišta, Start, dock
  'Plocha': 'Desktop',
  'Ukončit': 'Quit',
  'Opravdu ukončit?': 'Really quit?',
  'Hledat': 'Search',
  'Hledat (Start)': 'Search (Start)',
  'Hledat aplikace': 'Search apps',
  'Nastavení': 'Settings',
  'Nastavení Better Windows': 'Better Windows Settings',
  'Zamknout': 'Lock',
  'Načítám aplikace…': 'Loading apps…',
  'Nic nenalezeno pro „{q}“': 'No results for “{q}”',
  'Připnout do docku': 'Keep in Dock',
  'Koš': 'Trash',
  'Launchpad': 'Launchpad',
  'Dock': 'Dock',
  'Start': 'Start',
  'Dynamic Island': 'Dynamic Island',
  'Kameru používá: {apps}': 'Camera in use by: {apps}',
  'Mikrofon používá: {apps}': 'Microphone in use by: {apps}',
  'neznámá aplikace': 'unknown app',

  // Baterie a počasí
  'Baterie': 'Battery',
  'Baterie {p} %': 'Battery {p}%',
  'nabíjí se': 'charging',
  'napájení ze sítě': 'on AC power',
  'Nabíjí se': 'Charging',
  'Napájení ze sítě': 'On AC power',
  'Na baterii': 'On battery',
  'Tento počítač': 'This PC',
  'Tento počítač nemá baterii': 'This PC has no battery',
  'Počasí': 'Weather',
  'Počasí: {e}': 'Weather: {e}',
  'Počasí je vypnuté v nastavení': 'Weather is turned off in Settings',
  'Počasí se nepodařilo načíst.': 'Couldn’t load the weather.',
  'Načítám počasí…': 'Loading weather…',
  'Max {hi} · Min {lo}': 'H {hi} · L {lo}',
  'Neznámé': 'Unknown',
  'Jasno': 'Clear',
  'Skoro jasno': 'Mostly clear',
  'Polojasno': 'Partly cloudy',
  'Zataženo': 'Overcast',
  'Oblačno': 'Cloudy',
  'Mlha': 'Fog',
  'Mrholení': 'Drizzle',
  'Déšť': 'Rain',
  'Sněžení': 'Snow',
  'Bouřka': 'Thunderstorm',

  // Widgety
  'Widgety': 'Widgets',
  'Upravit widgety': 'Edit Widgets',
  'Přidat:': 'Add:',
  'Odebrat widget': 'Remove widget',
  'Hodiny': 'Clock',
  'Kalendář': 'Calendar',
  'Připomínky': 'Reminders',
  'Předchozí měsíc': 'Previous month',
  'Další měsíc': 'Next month',
  'Přidat připomínku…': 'Add a reminder…',
  'Zatím nic. Napiš první připomínku.': 'Nothing yet. Write your first reminder.',
  'Označit jako splněné': 'Mark as done',
  'Označit jako nesplněné': 'Mark as not done',

  // Testovací karta a vrstva plochy
  'Vrstva plochy': 'Desktop layer',
  'Ikony plochy': 'Desktop icons',
  'skryté': 'hidden',
  'viditelné': 'visible',
  'Test myši: {n}×': 'Mouse test: {n}×',
  'Test klávesnice – zkus psát': 'Keyboard test – try typing',
  'Kartu vypneš v Nastavení › Plocha': 'Turn this card off in Settings › Desktop',
  'Čekám na údaje…': 'Waiting for data…',
  'Vloženo do plochy (Windows 11 24H2+)': 'Embedded in the desktop (Windows 11 24H2+)',
  'Vloženo do plochy (klasicky)': 'Embedded in the desktop (classic)',
  'Samostatné okno vespod (HWND_BOTTOM)': 'Separate window at the bottom (HWND_BOTTOM)',

  // Zamykací obrazovka
  'Zamykací obrazovka': 'Lock Screen',
  'Klikni nebo stiskni libovolnou klávesu': 'Click or press any key',
  'Upravit zamykací obrazovku': 'Customize Lock Screen',
  'Styl hodin': 'Clock style',
  'Velké': 'Large',
  'Tenké': 'Thin',
  'Tučné': 'Bold',
  'Ručičky': 'Analog',
  'Umístění': 'Position',
  'Umístění hodin': 'Clock position',
  'Nahoře': 'Top',
  'Uprostřed': 'Center',
  'Sekundy': 'Seconds',
  'Uživatel': 'User',
  'Rozmazání tapety': 'Wallpaper blur',
  'Ztmavení tapety': 'Wallpaper dimming',

  // Nastavení – sekce
  'Načítám nastavení…': 'Loading settings…',
  'Vzhled': 'Appearance',
  'Motiv': 'Theme',
  'Výkon': 'Performance',
  'Vizualizér': 'Visualizer',
  'Horní lišta': 'Menu Bar',
  'Hlavní panel': 'Taskbar',
  'Hlavní panel Windows': 'Windows Taskbar',
  'O aplikaci': 'About',

  // Nastavení – vzhled
  'Světlý a tmavý režim se řídí nastavením Windows': 'Light and dark mode follow your Windows settings',
  'Rozmazání horní lišty': 'Menu bar blur',
  'Systémové je rozmazání Windows 11, vlastní tón je průhlednější': 'System uses the Windows 11 blur; custom tint is more transparent',
  'Rozmazání docku': 'Dock blur',
  'Systémové': 'System',
  'Vlastní tón': 'Custom tint',
  'Žádné': 'None',
  'Omezit animace': 'Reduce motion',
  'Vypne zvětšování s doběhem a další plynulé přechody': 'Turns off springy magnification and other smooth transitions',

  // Nastavení – motiv
  'Barvy, průhlednost, rozmazání a zaoblení. Nemění tón systémového rozmazání (ten určuje Windows) ani nativní kontextovou nabídku.':
    'Colors, transparency, blur and corner radius. Doesn’t change the tint of the system blur (Windows controls that) or native context menus.',
  'Výchozí': 'Default',
  'Sklo': 'Glass',
  'Grafit': 'Graphite',
  'Oceán': 'Ocean',
  'Západ slunce': 'Sunset',
  'Máta': 'Mint',
  'Vlastní': 'Custom',
  'Barva zvýraznění podle Windows': 'Use Windows accent color',
  'Přizpůsobení › Barvy › Barva zvýraznění': 'Personalization › Colors › Accent color',
  'Vlastní barva zvýraznění': 'Custom accent color',
  'Barva zvýraznění': 'Accent color',
  'Zaoblení karet': 'Card corner radius',
  'Rozmazání widgetů': 'Widget blur',
  'Neprůhlednost widgetů': 'Widget opacity',
  'Tón horní lišty': 'Menu bar tint',
  'Přidává barvu přes systémové rozmazání': 'Adds color over the system blur',
  'Tón docku': 'Dock tint',
  'Sdílet motiv': 'Share theme',
  'Kód obsahuje motiv a velikost ikon docku. Pošli ho komukoli s Better Windows.':
    'The code contains the theme and the Dock icon size. Send it to anyone with Better Windows.',
  'Vytvořit kód': 'Create code',
  'Kód motivu': 'Theme code',
  'Použít kód motivu': 'Apply a theme code',
  'Kód motivu k importu': 'Theme code to import',
  'Motiv použit': 'Theme applied',
  'Kód nejde použít.': 'This code can’t be used.',

  // Nastavení – výkon
  'Ztlumí animace a pozastaví vizualizér, když na nich nezáleží nebo když šetříš baterii':
    'Tones down animations and pauses the visualizer when they don’t matter or when you’re saving battery',
  'Režim výkonu': 'Performance mode',
  'Vždy úsporně': 'Always save power',
  'Vždy plné animace': 'Always full animations',
  'Ztlumit na baterii': 'Reduce on battery',
  'Ztlumit při aplikaci přes celou obrazovku': 'Reduce while an app is full screen',
  'Ztlumit při celé obrazovce': 'Reduce in full screen',
  'Hry, videa, prezentace': 'Games, videos, presentations',
  'Ztlumené animace': 'Reduced animations',
  'Plné animace': 'Full animations',

  // Nastavení – plocha
  'Vrstva pod všemi aplikacemi: tapeta, widgety a vizualizér': 'The layer below all apps: wallpaper, widgets and visualizer',
  'Způsob umístění': 'Placement',
  'Automaticky zkusí vložení do plochy Windows, při neúspěchu okno vespod. Okno vespod nedostává klávesnici.':
    'Automatic tries to embed into the Windows desktop and falls back to a bottom window. The bottom window doesn’t receive keyboard input.',
  'Vložit do plochy': 'Embed in desktop',
  'Okno vespod': 'Bottom window',
  'Skrýt ikony plochy': 'Hide desktop icons',
  'Jen když je vrstva vložená do plochy. Bez skrytí ikon nereagují widgety na myš.':
    'Only when the layer is embedded in the desktop. Widgets don’t respond to the mouse unless the icons are hidden.',
  'Tapeta': 'Wallpaper',
  'Aktuální tapeta Windows': 'Current Windows wallpaper',
  'Vybrat obrázek…': 'Choose image…',
  'Použít tapetu Windows': 'Use Windows wallpaper',
  'Testovací karta': 'Test card',
  'Diagnostika vrstvy plochy (režim, test myši a klávesnice)': 'Desktop layer diagnostics (mode, mouse and keyboard test)',

  // Nastavení – vizualizér
  'Reaguje na zvuk, který hraje z počítače (ne na mikrofon)': 'Reacts to sound playing on this PC (not the microphone)',
  'Scéna': 'Scene',
  'Tečky': 'Dots',
  'Vlna': 'Wave',
  'Kruh': 'Ring',
  'Citlivost': 'Sensitivity',

  // Nastavení – dock
  'Velikost ikon': 'Icon size',
  'Zvětšování při najetí': 'Magnify on hover',
  'Zvětšování': 'Magnification',
  'Náhledy oken': 'Window previews',
  'Po najetí na ikonu spuštěné aplikace': 'When hovering over a running app’s icon',
  'Launchpad a Koš': 'Launchpad and Trash',
  'Ikona Startu na začátku docku a koš na konci': 'Start icon at the beginning of the Dock and Trash at the end',
  'Největší zvětšení': 'Maximum magnification',
  'Připnuté aplikace': 'Pinned apps',
  'Připnout jde pravým tlačítkem na ikonu v docku': 'Right-click an icon in the Dock to pin it',
  'Odepnout': 'Unpin',
  'Nic není připnuté.': 'Nothing is pinned.',

  // Nastavení – Start
  'Vyhledávání a všechny aplikace. Otevřeš ho kliknutím na logo v horní liště.':
    'Search and all your apps. Open it by clicking the logo in the menu bar.',
  'Otevírat klávesou Windows': 'Open with the Windows key',
  'Samotný stisk klávesy Windows otevře tento Start místo Startu Windows. Zkratky (Win+E, Win+L…) fungují dál. Když je v popředí aplikace spuštěná jako správce, otevře se Start Windows.':
    'Pressing the Windows key on its own opens this Start instead of the Windows Start. Shortcuts (Win+E, Win+L…) keep working. When an app running as administrator is in the foreground, the Windows Start opens.',

  // Nastavení – Dynamic Island
  'Právě hrající hudba a hlasitost nahoře uprostřed': 'Now playing and volume at the top center',
  'Ukazovat změnu hlasitosti': 'Show volume changes',
  'Oznámení': 'Notifications',
  'Číst oznámení ostatních aplikací Windows dovolí jen aplikacím nainstalovaným jako balíček (MSIX). Better Windows zatím balíček není, proto oznámení v ostrůvku nejsou.':
    'Windows only lets apps installed as a package (MSIX) read other apps’ notifications. Better Windows isn’t packaged yet, so the island can’t show notifications.',

  // Nastavení – zamykací obrazovka
  'Vlastní obrazovka s hodinami, hudbou a počasím, která se ukáže PŘED zámkem Windows. Otevřeš ji zkratkou Win+Shift+L nebo tlačítkem Zamknout ve Startu.':
    'A custom screen with the clock, music and weather that appears BEFORE the Windows lock screen. Open it with Win+Shift+L or the Lock button in Start.',
  'Po odemknutí zamknout Windows': 'Lock Windows after unlocking',
  'Doporučeno. Naše obrazovka nic nechrání (jde obejít Ctrl+Alt+Del); heslo, PIN nebo Windows Hello chce až zámek Windows.':
    'Recommended. This screen doesn’t protect anything (Ctrl+Alt+Del gets past it); your password, PIN or Windows Hello is asked for by the Windows lock screen.',
  'Ukázat po nečinnosti': 'Show after inactivity',
  'Ne během hudby, videa a her přes celou obrazovku': 'Not during music, video or full-screen games',
  'Styl hodin, umístění, co se ukazuje, rozmazání tapety – s živým náhledem. Náhled Windows nezamkne.':
    'Clock style, position, what’s shown, wallpaper blur – with a live preview. The preview doesn’t lock Windows.',

  // Nastavení – horní lišta
  'Mini přehrávač vedle názvu aplikace': 'Mini player next to the app name',
  'Jazyk klávesnice': 'Keyboard language',
  'Tlačítko Ukončit': 'Quit button',

  // Nastavení – počasí
  'Open-Meteo, bez účtu a API klíče': 'Open-Meteo, no account or API key needed',
  'Město': 'City',
  'např. Praha': 'e.g. London',
  'Nalezeno: {place}': 'Found: {place}',
  'Uloží se Enterem': 'Press Enter to save',

  // Nastavení – hlavní panel
  'Skrýt hlavní panel': 'Hide taskbar',
  'Vrátí se při ukončení aplikace, při pádu i po zabití procesu. Windows si přitom zapamatuje „automaticky skrývat“ – to aplikace při ukončení také vrací.':
    'It comes back when the app quits, crashes or is killed. Windows remembers “automatically hide” meanwhile – the app restores that on exit too.',
  'Opravdu skrýt?': 'Really hide?',
  'Nouzová obnova': 'Emergency restore',
  'Ukáže ikony plochy i hlavní panel a vypne jejich skrývání. Totéž je v nabídce ikony v oznamovací oblasti.':
    'Shows the desktop icons and the taskbar and turns off hiding them. The same option is in the notification area icon menu.',
  'Obnovit ikony plochy a hlavní panel': 'Restore desktop icons and taskbar',

  // Nastavení – o aplikaci
  'Otevřít logy': 'Open logs',
  'Složka s daty': 'Data folder',
};
