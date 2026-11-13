using System.IO;

namespace BetterWindows.Core;

/// <summary>Název aplikace a cesty k jejím datovým složkám.</summary>
public static class AppInfo
{
    /// <summary>
    /// Aplikace se ukončuje (WPF při ukončení zavírá okna ještě před OnExit). Obsluhy „okno zaniklo“
    /// podle toho poznají, že nejde o pád Průzkumníka a nemají nic obnovovat ani vytvářet znovu.
    /// </summary>
    public static volatile bool Exiting;

    /// <summary>Jméno, které vidí uživatel.</summary>
    public const string DisplayName = "Better Windows";

    /// <summary>Technický identifikátor (složky, mutex, ...). Bez mezer.</summary>
    public const string Id = "BetterWindows";

    /// <summary>%AppData%\BetterWindows – nastavení a logy (putuje s profilem uživatele).</summary>
    public static string DataDir { get; } = EnsureDir(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Id));

    /// <summary>%LocalAppData%\BetterWindows – cache (WebView2), nemá smysl ji přenášet.</summary>
    public static string LocalDataDir { get; } = EnsureDir(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Id));

    public static string LogDir { get; } = EnsureDir(Path.Combine(DataDir, "logs"));

    /// <summary>
    /// Složka se sestaveným webovým rozhraním: normálně kopie ui/dist vedle exe.
    /// V Debug buildu spuštěném z repozitáře se čte přímo ui/dist, takže stačí
    /// `npm run watch` v ui/ a Ctrl+R v okně, bez přestavění C#.
    /// </summary>
    public static string UiDir { get; } = ResolveUiDir();

    private static string ResolveUiDir()
    {
#if DEBUG
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ui", "vite.config.ts")))
                return Path.Combine(dir.FullName, "ui", "dist");
        }
#endif
        return Path.Combine(AppContext.BaseDirectory, "ui");
    }

    private static string EnsureDir(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
