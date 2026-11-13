using System.Globalization;

namespace BetterWindows.Logic.Localization;

/// <summary>
/// Texty pro uživatele v jazyce Windows: čeština (a slovenština) česky, ostatní jazyky anglicky.
/// Klíč = český text přímo ve zdrojáku, překlad je v <see cref="En"/> (stejně jako t('…') v UI).
/// Chybějící překlad se ukáže česky – test LocTests hlídá, že každé Loc.T("…") překlad má.
/// Logy zůstávají česky (čte je vývojář, ne uživatel).
/// </summary>
public static class Loc
{
    /// <summary>Jazyk rozhraní; výchozí podle jazyka zobrazení Windows (CurrentUICulture). Testy ho můžou přepnout.</summary>
    public static bool Czech { get; set; } = IsCzech(CultureInfo.CurrentUICulture);

    public static bool IsCzech(CultureInfo culture) => culture.TwoLetterISOLanguageName is "cs" or "sk";

    public static string T(string cs) => Translate(cs, Czech);

    /// <summary>Překlad bez globálního stavu (pro testy).</summary>
    public static string Translate(string cs, bool czech) => czech ? cs : En.GetValueOrDefault(cs, cs);

    /// <summary>Text s proměnnými: Loc.T("Město „{city}“ nenalezeno.", ("city", name)).</summary>
    public static string T(string cs, params (string Name, object? Value)[] vars)
    {
        var s = T(cs);
        foreach (var (name, value) in vars)
            s = s.Replace("{" + name + "}", Convert.ToString(value, CultureInfo.CurrentCulture));
        return s;
    }

    /// <summary>True, když existuje anglický překlad (pro test úplnosti).</summary>
    public static bool HasTranslation(string cs) => En.ContainsKey(cs);

    private static readonly Dictionary<string, string> En = new()
    {
        // Oznamovací oblast
        ["Nastavení"] = "Settings",
        ["Rozmazání lišty a docku"] = "Menu bar and Dock blur",
        ["Systémové (Windows 11 Acrylic)"] = "System (Windows 11 Acrylic)",
        ["Vlastní tón (API z Windows 10)"] = "Custom tint (Windows 10 API)",
        ["Žádné"] = "None",
        ["Obnovit ikony plochy a hlavní panel"] = "Restore desktop icons and taskbar",
        ["Ukončit"] = "Quit",

        // Dock
        ["{title} (minimalizováno)"] = "{title} (minimized)",
        ["Nové okno"] = "New Window",
        ["Odepnout z docku"] = "Remove from Dock",
        ["Připnout do docku"] = "Keep in Dock",
        ["Ukončit (zavřít {n} okna)"] = "Quit (close {n} windows)",
        ["Ukončit (zavřít {n} oken)"] = "Quit (close {n} windows)",
        ["Otevřít koš"] = "Open Trash",
        ["Vysypat koš…"] = "Empty Trash…",
        ["Minimalizováno"] = "Minimized",
        ["Zavřít okno"] = "Close window",
        ["Průzkumník souborů"] = "File Explorer",

        // Okna a dialogy
        ["Better Windows – Nastavení"] = "Better Windows Settings",
        ["Na počítači chybí Microsoft Edge WebView2 Runtime.\nStáhni ho z {url}"] =
            "Microsoft Edge WebView2 Runtime is missing on this PC.\nDownload it from {url}",
        ["{app} se nepodařilo spustit:\n{error}\n\nPodrobnosti v logu:\n{log}"] =
            "{app} couldn’t start:\n{error}\n\nDetails are in the log:\n{log}",
        ["{app} narazilo na chybu a ukončí se.\n\n{error}\n\nPodrobnosti jsou v logu:\n{log}"] =
            "{app} ran into an error and will quit.\n\n{error}\n\nDetails are in the log:\n{log}",
        ["Vyber tapetu"] = "Choose a wallpaper",
        ["Obrázky"] = "Images",
        ["Všechny soubory"] = "All files",

        // Výkon (důvod se ukazuje v Nastavení › Výkon)
        ["Omezit animace je zapnuto ve Vzhledu"] = "Reduce motion is on in Appearance",
        ["Režim výkonu: vždy úsporně"] = "Performance mode: always save power",
        ["Režim výkonu: vždy plné animace"] = "Performance mode: always full animations",
        ["Aplikace přes celou obrazovku"] = "An app is full screen",
        ["Úspora energie Windows"] = "Windows energy saver",
        ["Napájení z baterie"] = "Running on battery",
        ["Plné animace"] = "Full animations",

        // Motivy
        ["Kód je prázdný nebo moc dlouhý."] = "The code is empty or too long.",
        ["Tohle není kód motivu Better Windows (má začínat „BW1.“)."] = "This isn’t a Better Windows theme code (it should start with “BW1.”).",
        ["Kód motivu je poškozený."] = "The theme code is damaged.",

        // Počasí, jazyk klávesnice
        ["Město „{city}“ nenalezeno."] = "City “{city}” not found.",
        ["Počasí se nepodařilo načíst (bez připojení?)."] = "Couldn’t load the weather (no connection?).",
        ["Neznámý jazyk"] = "Unknown language",

        // Vrstva plochy (Nastavení › Plocha › Stav)
        ["Vypnuto v nastavení"] = "Turned off in Settings",
        ["Zastaveno"] = "Stopped",
        ["Okno vespod – vložená plocha nepropouštěla kliknutí k widgetům"] = "Bottom window – the embedded desktop didn’t pass clicks to the widgets",
        ["Opakované selhání (víc než 3 restarty za minutu)"] = "Repeated failure (more than 3 restarts per minute)",
        ["Okno zaniklo, čekám na Průzkumníka"] = "The window is gone, waiting for File Explorer",
        ["Vložení selhalo ({detail}), záloha HWND_BOTTOM"] = "Embedding failed ({detail}), falling back to HWND_BOTTOM",
        ["HWND_BOTTOM (zvoleno v nastavení)"] = "HWND_BOTTOM (chosen in Settings)",
        ["HWND_BOTTOM (plocha nenalezena)"] = "HWND_BOTTOM (desktop not found)",
        ["Nevhodné rozložení plochy: {snap}"] = "Unsuitable desktop layout: {snap}",
        ["SetParent selhalo (chyba {error}, předchozí rodič {parent}); {snap}"] = "SetParent failed (error {error}, previous parent {parent}); {snap}",
        ["24H2+: podokno WorkerW uvnitř Progman (WS_EX_LAYERED)"] = "24H2+: WorkerW child inside Progman (WS_EX_LAYERED)",
        ["24H2+: podokno Progman pod SHELLDLL_DefView (WS_EX_LAYERED)"] = "24H2+: Progman child below SHELLDLL_DefView (WS_EX_LAYERED)",
        ["24H2+: podokno Progman pod SHELLDLL_DefView (WorkerW je zakázané – nepropouští myš)"] =
            "24H2+: Progman child below SHELLDLL_DefView (WorkerW is disabled – it blocks the mouse)",
    };
}
