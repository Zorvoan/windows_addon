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

    /// <summary>Výsledný režim (systém nebo ruční volba z nastavení).</summary>
    public bool IsDark { get; private set; }

    /// <summary>Režim se změnil. Volá se v UI vlákně.</summary>
    public event Action<bool>? Changed;

    public ThemeService(System.Windows.Threading.Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _systemIsDark = ReadIsDark();
        IsDark = _systemIsDark;
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
        // Přepnutí motivu Windows hlásí jako kategorii General (zpráva "ImmersiveColorSet").
        if (e.Category != UserPreferenceCategory.General)
            return;

        _dispatcher.InvokeAsync(() =>
        {
            _systemIsDark = ReadIsDark();
            Recompute();
        });
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
