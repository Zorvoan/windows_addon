namespace BetterWindows.Logic.Privacy;

/// <summary>
/// Jeden záznam z registru HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\{microphone|webcam}.
/// Podklíč = aplikace z balíčku (např. "Microsoft.WindowsCamera_8wekyb3d8bbwe"),
/// v podklíči NonPackaged = klasické aplikace s cestou, kde je "\" nahrazené "#".
/// </summary>
public sealed record ConsentEntry(string KeyName, bool NonPackaged, long? LastUsedTimeStart, long? LastUsedTimeStop);

public static class ConsentStore
{
    /// <summary>Aplikace zařízení právě používá: začátek použití je zapsaný, konec je 0.</summary>
    public static bool IsInUse(ConsentEntry e) => e.LastUsedTimeStop == 0 && e.LastUsedTimeStart is > 0;

    /// <summary>Čitelné jméno aplikace pro bublinu u tečky.</summary>
    public static string DisplayName(ConsentEntry e)
    {
        if (e.NonPackaged)
        {
            // "C:#Program Files#Zoom#bin#Zoom.exe" -> "Zoom"
            var file = e.KeyName[(e.KeyName.LastIndexOf('#') + 1)..];
            return file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? file[..^4] : file;
        }

        // "Microsoft.WindowsCamera_8wekyb3d8bbwe" -> "WindowsCamera"
        var name = e.KeyName;
        var underscore = name.IndexOf('_');
        if (underscore > 0) name = name[..underscore];
        var dot = name.LastIndexOf('.');
        return dot >= 0 && dot < name.Length - 1 ? name[(dot + 1)..] : name;
    }

    public static IReadOnlyList<string> InUseApps(IEnumerable<ConsentEntry> entries) =>
        entries.Where(IsInUse).Select(DisplayName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList();
}
