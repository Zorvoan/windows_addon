using BetterWindows.Core;
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

    public bool IsDark { get; private set; }

    /// <summary>Režim se změnil. Volá se v UI vlákně.</summary>
    public event Action<bool>? Changed;

    public ThemeService(System.Windows.Threading.Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        IsDark = ReadIsDark();
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
            var dark = ReadIsDark();
            if (dark == IsDark)
                return;
            IsDark = dark;
            Log.Info($"Motiv Windows změněn na {(dark ? "tmavý" : "světlý")}");
            Changed?.Invoke(dark);
        });
    }

    public void Dispose() => SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
}
