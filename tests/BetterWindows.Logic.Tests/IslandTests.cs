using BetterWindows.Logic.Apps;
using Xunit;

namespace BetterWindows.Logic.Tests;

public class IslandTests
{
    [Theory]
    // Klasická aplikace: zdroj = jméno exe
    [InlineData("Spotify.exe", @"exe:c:\users\jan\appdata\roaming\spotify\spotify.exe", true)]
    // Prohlížeče hlásí jen jméno bez přípony
    [InlineData("Chrome", @"exe:c:\program files\google\chrome\application\chrome.exe", true)]
    [InlineData("MSEdge", @"exe:c:\program files (x86)\microsoft\edge\application\msedge.exe", true)]
    // Aplikace z balíčku: celé AUMID (velikost písmen nehraje roli)
    [InlineData("SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify", "aumid:spotifyab.spotifymusic_zpdnekdrzrea0!Spotify", true)]
    // Jiná aplikace
    [InlineData("Spotify.exe", @"exe:c:\windows\notepad.exe", false)]
    [InlineData("SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify", @"exe:c:\program files\spotify\spotify.exe", false)]
    [InlineData("Chrome", "aumid:Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", false)]
    // Chybějící údaje
    [InlineData(null, @"exe:c:\windows\notepad.exe", false)]
    [InlineData("Spotify.exe", null, false)]
    public void Media_source_matches_window_app(string? source, string? appId, bool expected)
    {
        Assert.Equal(expected, MediaSourceMatch.Matches(source, appId));
    }
}

public class SettingsMigrationTests
{
    [Fact]
    public void V1_default_dock_magnification_is_lowered_to_1_1()
    {
        var s = new BetterWindows.Logic.Settings.AppSettings { Version = 1 };
        s.Dock.MaxScale = 1.7;
        BetterWindows.Logic.Settings.SettingsNormalizer.Normalize(s);
        Assert.Equal(1.1, s.Dock.MaxScale, 3);
        Assert.Equal(BetterWindows.Logic.Settings.SettingsNormalizer.CurrentVersion, s.Version);
    }

    [Fact]
    public void Custom_dock_magnification_is_kept()
    {
        var s = new BetterWindows.Logic.Settings.AppSettings { Version = 1 };
        s.Dock.MaxScale = 1.4;
        BetterWindows.Logic.Settings.SettingsNormalizer.Normalize(s);
        Assert.Equal(1.4, s.Dock.MaxScale, 3);
    }

    [Fact]
    public void Current_version_is_not_migrated_again()
    {
        var s = new BetterWindows.Logic.Settings.AppSettings();
        s.Dock.MaxScale = 1.7; // vědomá volba v nové verzi
        BetterWindows.Logic.Settings.SettingsNormalizer.Normalize(s);
        Assert.Equal(1.7, s.Dock.MaxScale, 3);
    }
}
