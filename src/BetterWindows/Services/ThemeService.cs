using BetterWindows.Core;
using BetterWindows.Logic.Settings;
using Microsoft.Win32;

namespace BetterWindows.Services;

/// <summary>
/// Světlý / tmavý režim podle Windows (Nastavení > Přizpůsobení > Barvy > "Režim aplikací").
/// Jediné místo v C#, odkud se motiv bere: řídí tón systémového rozmazání i CSS ve WebView2.
/// </summary>
public sealed class ThemeService : IDisposable
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private readonly System.Windows.Threading.Dispatcher _dispatcher;
    private ThemeMode _mode = ThemeMode.System;
    private bool _systemIsDark;

    /// <summary>Výsledný režim (podle systému).</summary>
    public bool IsDark { get; private set; }

    /// <summary>Windows: Přizpůsobení › Barvy › Efekty průhlednosti vypnuté.</summary>
    public bool ReduceTransparency { get; private set; }

    /// <summary>Windows: Usnadnění › Kontrastní motivy.</summary>
    public bool HighContrast { get; private set; }

    /// <summary>Barva zvýraznění Windows (#rrggbb).</summary>
    public string Accent { get; private set; } = "#0078d4";

    /// <summary>Změnila se průhlednost, kontrast nebo barva zvýraznění. Volá se v UI vlákně.</summary>
    public event Action? SystemAppearanceChanged;

    /// <summary>Režim se změnil. Volá se v UI vlákně.</summary>
    public event Action<bool>? Changed;

    public ThemeService(System.Windows.Threading.Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _systemIsDark = ReadIsDark();
        IsDark = _systemIsDark;
        ReadSystemAppearance();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        Log.Info($"Motiv Windows: {(IsDark ? "tmavý" : "světlý")}");
    }

    private static bool ReadIsDark()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        // AppsUseLightTheme: 1 = světlý, 0 = tmavý. Chybí-li hodnota, Windows bere světlý.
        return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        // Přepnutí motivu / barvy / průhlednosti Windows hlásí jako General ("ImmersiveColorSet"),
        // kontrastní motivy jako Accessibility.
        if (e.Category is not (UserPreferenceCategory.General or UserPreferenceCategory.Accessibility or UserPreferenceCategory.Color))
            return;

        _dispatcher.InvokeAsync(() =>
        {
            _systemIsDark = ReadIsDark();
            Recompute();
            if (ReadSystemAppearance())
                SystemAppearanceChanged?.Invoke();
        });
    }

    /// <summary>Načte průhlednost, kontrast a barvu zvýraznění. Vrací true, když se něco změnilo.</summary>
    private bool ReadSystemAppearance()
    {
        using var personalize = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        var reduce = personalize?.GetValue("EnableTransparency") is int t && t == 0;
        var contrast = System.Windows.SystemParameters.HighContrast;
        var accent = Accent;
        using (var dwm = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM"))
        {
            // AccentColor je DWORD ve tvaru 0xAABBGGRR.
            if (dwm?.GetValue("AccentColor") is int abgr)
            {
                var v = unchecked((uint)abgr);
                accent = $"#{v & 0xFF:x2}{(v >> 8) & 0xFF:x2}{(v >> 16) & 0xFF:x2}";
            }
        }
        if (reduce == ReduceTransparency && contrast == HighContrast && accent == Accent)
            return false;
        ReduceTransparency = reduce;
        HighContrast = contrast;
        Accent = accent;
        Log.Info($"Vzhled Windows: průhlednost {(reduce ? "vypnutá" : "zapnutá")}, vysoký kontrast {(contrast ? "ano" : "ne")}, zvýraznění {accent}");
        return true;
    }

    /// <summary>Ruční volba z nastavení: podle systému / vždy světlý / vždy tmavý.</summary>
    public void SetMode(ThemeMode mode)
    {
        _mode = mode;
        Recompute();
    }

    private void Recompute()
    {
        var dark = _mode switch
        {
            ThemeMode.Light => false,
            ThemeMode.Dark => true,
            _ => _systemIsDark,
        };
        if (dark == IsDark)
            return;
        IsDark = dark;
        Log.Info($"Motiv změněn na {(dark ? "tmavý" : "světlý")} (volba: {_mode})");
        Changed?.Invoke(dark);
    }

    public void Dispose() => SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
}
