namespace BetterWindows.Logic;

/// <summary>
/// Zápis souboru "naráz": nejdřív do dočasného souboru, pak přejmenování.
/// Když aplikace spadne uprostřed zápisu, původní soubor zůstane celý.
/// </summary>
public static class AtomicFile
{
    public static void WriteAllText(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var temp = path + ".tmp";
        File.WriteAllText(temp, content);
        File.Move(temp, path, overwrite: true);
    }
}
