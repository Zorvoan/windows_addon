namespace BetterWindows.Logic.Apps;

public enum AppIdKind { Exe, Aumid, Unknown }

/// <summary>
/// Identita aplikace (podle ní se připíná, seskupují okna, spouští a hledají ikony):
///  - "exe:c:\program files\app\app.exe" pro klasické aplikace (cesta malými písmeny),
///  - "aumid:Microsoft.WindowsCalculator_8wekyb3d8bbwe!App" pro aplikace z balíčku (UWP / MSIX).
/// </summary>
public static class AppId
{
    private const string ExePrefix = "exe:";
    private const string AumidPrefix = "aumid:";

    public static string FromExePath(string path) => ExePrefix + path.Trim().ToLowerInvariant();

    public static string FromAumid(string aumid) => AumidPrefix + aumid.Trim();

    public static (AppIdKind Kind, string Value) Parse(string appId)
    {
        if (appId.StartsWith(ExePrefix, StringComparison.OrdinalIgnoreCase))
            return (AppIdKind.Exe, appId[ExePrefix.Length..]);
        if (appId.StartsWith(AumidPrefix, StringComparison.OrdinalIgnoreCase))
            return (AppIdKind.Aumid, appId[AumidPrefix.Length..]);
        return (AppIdKind.Unknown, appId);
    }

    public static bool Equal(string? a, string? b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
