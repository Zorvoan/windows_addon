namespace BetterWindows.Logic.Apps;

/// <summary>
/// Patří okno aplikaci, která právě přehrává (systémové ovládání médií)? Zdroj médií Windows hlásí
/// jako AUMID, ale v různých podobách:
///  - aplikace z balíčku: celé AUMID ("SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify"),
///  - klasické aplikace: jméno exe ("Spotify.exe") nebo jen jméno ("Chrome", "MSEdge").
/// </summary>
public static class MediaSourceMatch
{
    public static bool Matches(string? sourceAumid, string? windowAppId)
    {
        if (string.IsNullOrWhiteSpace(sourceAumid) || string.IsNullOrWhiteSpace(windowAppId))
            return false;
        var source = sourceAumid.Trim();
        var (kind, value) = AppId.Parse(windowAppId);

        if (kind == AppIdKind.Aumid)
            return string.Equals(value, source, StringComparison.OrdinalIgnoreCase);
        if (kind != AppIdKind.Exe)
            return false;

        // Klasická aplikace: porovnat jméno exe (s příponou i bez). Oddělovače ručně – cesty jsou
        // vždy windowsové, i když logika běží v testech na Linuxu.
        var file = FileName(value);
        var stem = file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? file[..^4] : file;
        var sourceFile = FileName(source);
        return string.Equals(file, sourceFile, StringComparison.OrdinalIgnoreCase)
            || string.Equals(stem, sourceFile, StringComparison.OrdinalIgnoreCase);
    }

    private static string FileName(string path)
    {
        var i = path.LastIndexOfAny(['\\', '/']);
        return i >= 0 ? path[(i + 1)..] : path;
    }
}
