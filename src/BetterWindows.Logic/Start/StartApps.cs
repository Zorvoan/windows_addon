using System.Text.RegularExpressions;
using BetterWindows.Logic.Apps;

namespace BetterWindows.Logic.Start;

/// <summary>Aplikace v nabídce Start (z shell:AppsFolder).</summary>
public sealed record StartApp(string AppId, string Name);

public static partial class StartApps
{
    /// <summary>
    /// Převod "parsing name" položky z shell:AppsFolder na identitu aplikace:
    ///  - "{GUID známé složky}\cesta\app.exe" -> exe identita (spáruje se se spuštěnými okny),
    ///  - ostatní (AUMID aplikace z balíčku, AUMID zástupce) -> aumid identita (spustí se přes shell:AppsFolder).
    /// </summary>
    public static string ToAppId(string parsingName, Func<Guid, string?> knownFolderPath)
    {
        var m = KnownFolderPath().Match(parsingName);
        if (m.Success && Guid.TryParse(m.Groups[1].Value, out var folderId) &&
            m.Groups[2].Value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            var folder = knownFolderPath(folderId);
            if (!string.IsNullOrEmpty(folder))
                return AppId.FromExePath(folder.TrimEnd('\\') + "\\" + m.Groups[2].Value);
        }
        if (parsingName.Length >= 3 && parsingName[1] == ':' && parsingName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            return AppId.FromExePath(parsingName);
        return AppId.FromAumid(parsingName);
    }

    /// <summary>Položky, které do Startu nepatří (odinstalace, nápověda, dokumenty).</summary>
    public static bool ShouldHide(string name, string parsingName)
    {
        if (string.IsNullOrWhiteSpace(name))
            return true;
        var n = name.ToLowerInvariant();
        if (n.StartsWith("uninstall") || n.StartsWith("odinstal") || n.Contains("readme") || n.StartsWith("help"))
            return true;
        var p = parsingName.ToLowerInvariant();
        return p.EndsWith(".url") || p.EndsWith(".chm") || p.EndsWith(".txt") || p.EndsWith(".pdf") ||
               p.EndsWith(".htm") || p.EndsWith(".html") || p.StartsWith("http");
    }

    /// <summary>Seřadí podle jména (česky) a odstraní duplicity se stejnou identitou.</summary>
    public static IReadOnlyList<StartApp> Normalize(IEnumerable<StartApp> apps)
    {
        var culture = new System.Globalization.CultureInfo("cs-CZ");
        return apps
            .GroupBy(a => a.AppId, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(a => a.Name, StringComparer.Create(culture, ignoreCase: true))
            .ToList();
    }

    [GeneratedRegex(@"^\{([0-9A-Fa-f\-]{36})\}\\(.+)$")]
    private static partial Regex KnownFolderPath();
}
