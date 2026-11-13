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
