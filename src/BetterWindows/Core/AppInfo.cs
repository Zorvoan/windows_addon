using System.IO;

namespace BetterWindows.Core;

/// <summary>Název aplikace a cesty k jejím datovým složkám.</summary>
public static class AppInfo
{
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

    /// <summary>Složka se sestaveným webovým rozhraním (kopie ui/dist vedle exe).</summary>
    public static string UiDir { get; } = Path.Combine(AppContext.BaseDirectory, "ui");

    private static string EnsureDir(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
