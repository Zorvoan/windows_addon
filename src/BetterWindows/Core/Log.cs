using System.Diagnostics;
using System.IO;
using System.Text;

namespace BetterWindows.Core;

/// <summary>
/// Jednoduchý logger do souboru %AppData%\BetterWindows\logs\yyyy-MM-dd.log.
/// Záměrně bez knihoven: musí fungovat i ve chvíli, kdy aplikace padá.
/// </summary>
public static class Log
{
    private const int KeepDays = 7;
    private static readonly object Gate = new();

    private static string _suffix = "";

    public static string CurrentFile => Path.Combine(AppInfo.LogDir, $"{DateTime.Now:yyyy-MM-dd}{_suffix}.log");

    /// <param name="suffix">Watchdog píše do vlastního souboru (…watchdog.log), ať si procesy nepřekážejí.</param>
    public static void Init(string? suffix = null)
    {
        _suffix = string.IsNullOrEmpty(suffix) ? "" : "." + suffix;
        // Smaž staré logy, ať složka neroste donekonečna.
        try
        {
            foreach (var file in Directory.EnumerateFiles(AppInfo.LogDir, "*.log"))
            {
                if (File.GetLastWriteTime(file) < DateTime.Now.AddDays(-KeepDays))
                    File.Delete(file);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Mazání starých logů selhalo: {ex}");
        }
    }

    public static void Info(string message) => Write("INFO", message, null);
    public static void Warn(string message, Exception? ex = null) => Write("WARN", message, ex);
    public static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

    private static void Write(string level, string message, Exception? ex)
    {
        var line = new StringBuilder()
            .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
            .Append(" [").Append(level).Append("] ")
            .Append("[T").Append(Environment.CurrentManagedThreadId).Append("] ")
            .Append(message);
        if (ex is not null)
            line.AppendLine().Append(ex);

        var text = line.ToString();
        Debug.WriteLine(text);

        try
        {
            lock (Gate)
                File.AppendAllText(CurrentFile, text + Environment.NewLine);
        }
        catch
        {
            // Log nesmí nikdy shodit aplikaci.
        }
    }
}
