using System.IO;
using System.Windows.Threading;
using BetterWindows.Core;
using BetterWindows.Logic.Messaging;
using Microsoft.Win32;

namespace BetterWindows.Services;

/// <summary>
/// Tapeta pro vrstvu plochy: vlastní obrázek z nastavení, jinak aktuální tapeta Windows.
/// Obrázek se zkopíruje do %LocalAppData%\BetterWindows\wallpaper a stránka ho načte
/// z virtuální domény https://wallpaper.betterwindows/ (WebView2 nesmí číst libovolné soubory z disku).
/// </summary>
internal sealed class WallpaperService : IDisposable
{
    public const string HostName = "wallpaper.betterwindows";
    public static string CacheDir { get; } = Path.Combine(AppInfo.LocalDataDir, "wallpaper");

    private readonly SettingsService _settings;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _timer;
    private string? _lastStamp;

    public WallpaperStatePayload Current { get; private set; } = new(null, "#1e2430");

    /// <summary>Tapeta se změnila. Volá se v UI vlákně.</summary>
    public event Action<WallpaperStatePayload>? Changed;

    public WallpaperService(SettingsService settings, Dispatcher dispatcher)
    {
        _settings = settings;
        _dispatcher = dispatcher;
        Directory.CreateDirectory(CacheDir);

        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        _settings.Changed += (old, now) =>
        {
            if (old.Desktop.WallpaperPath != now.Desktop.WallpaperPath)
                Refresh(force: true);
        };
        // Prezentace / Windows Spotlight mění tapetu bez události -> kontrola jednou za minutu.
        _timer = new DispatcherTimer(TimeSpan.FromSeconds(60), DispatcherPriority.Background, (_, _) => Refresh(), dispatcher);
        _timer.Start();
        Refresh(force: true);
    }

    public void Refresh(bool force = false) => Safe.Run("Tapeta", () =>
    {
        var color = ReadBackgroundColor();
        var source = _settings.Current.Desktop.WallpaperPath;
        if (source is not null && !File.Exists(source))
        {
            Log.Warn($"Vlastní tapeta neexistuje: {source} – použiji tapetu Windows.");
            source = null;
        }
        source ??= FindWindowsWallpaper();

        if (source is null)
        {
            Publish(new WallpaperStatePayload(null, color), "jen barva");
            return;
        }

        var info = new FileInfo(source);
        var stamp = $"{source}|{info.Length}|{info.LastWriteTimeUtc.Ticks}";
        if (!force && stamp == _lastStamp)
            return;
        _lastStamp = stamp;

        var ext = Path.GetExtension(source).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp" or ".gif"))
            ext = ".jpg"; // TranscodedWallpaper nemá příponu; prohlížeč pozná formát z obsahu
        var fileName = $"wallpaper-{DateTime.UtcNow.Ticks}{ext}";
        File.Copy(source, Path.Combine(CacheDir, fileName), overwrite: true);
        foreach (var old in Directory.EnumerateFiles(CacheDir).Where(f => Path.GetFileName(f) != fileName))
            Safe.Run("Mazání staré tapety", () => File.Delete(old));

        Publish(new WallpaperStatePayload($"https://{HostName}/{fileName}", color), source);
    });

    private void Publish(WallpaperStatePayload state, string what)
    {
        if (state == Current && _lastStamp is not null)
            return;
        Current = state;
        Log.Info($"Tapeta: {what}");
        Changed?.Invoke(state);
    }

    /// <summary>Soubor, který Windows právě zobrazuje jako tapetu (nebo null = jednobarevné pozadí).</summary>
    private static string? FindWindowsWallpaper()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
        var registered = key?.GetValue("WallPaper") as string;
        if (string.IsNullOrWhiteSpace(registered))
            return null; // uživatel má nastavenou jen barvu

        // TranscodedWallpaper = kopie, kterou Windows skutečně kreslí (funguje i když originál smažeš).
        var transcoded = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft", "Windows", "Themes", "TranscodedWallpaper");
        if (File.Exists(transcoded))
            return transcoded;
        return File.Exists(registered) ? registered : null;
    }

    private static string ReadBackgroundColor()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Colors");
        var parts = (key?.GetValue("Background") as string)?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts is { Length: 3 } && parts.All(p => byte.TryParse(p, out _)))
            return $"#{byte.Parse(parts[0]):x2}{byte.Parse(parts[1]):x2}{byte.Parse(parts[2]):x2}";
        return "#1e2430";
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.Desktop)
            _dispatcher.InvokeAsync(() => Refresh(force: true));
    }

    public void Dispose()
    {
        _timer.Stop();
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
    }
}
