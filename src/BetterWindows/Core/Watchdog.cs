using System.Diagnostics;
using System.IO;

namespace BetterWindows.Core;

/// <summary>
/// Hlídací proces: stejné exe spuštěné s argumentem "--watchdog &lt;PID&gt;".
/// Čeká, až hlavní proces skončí (i zabitím ve Správci úloh), a pak podle
/// system-state.json vrátí, co hlavní proces nestihl (ikony plochy, hlavní panel).
/// Při běžném ukončení hlavní proces soubor smaže, takže watchdog nic nedělá.
/// </summary>
internal static class Watchdog
{
    public const string Argument = "--watchdog";

    public static void Launch()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (exe is null)
                return;

            var info = new ProcessStartInfo { UseShellExecute = false, CreateNoWindow = true };
            if (Path.GetFileNameWithoutExtension(exe).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            {
                // Spuštěno přes "dotnet BetterWindows.dll" -> watchdog stejně.
                info.FileName = exe;
                info.ArgumentList.Add(typeof(Watchdog).Assembly.Location);
            }
            else
            {
                info.FileName = exe;
            }
            info.ArgumentList.Add(Argument);
            info.ArgumentList.Add(Environment.ProcessId.ToString());

            using var process = Process.Start(info);
            Log.Info($"Watchdog spuštěn (PID {process?.Id}).");
        }
        catch (Exception ex)
        {
            Log.Error("Spuštění watchdogu selhalo – při zabití procesu se systém obnoví až při dalším spuštění.", ex);
        }
    }

    /// <summary>Běh v režimu watchdogu. Žádná okna, žádné UI.</summary>
    public static void Run(string[] args)
    {
        Log.Init("watchdog");
        var index = Array.IndexOf(args, Argument);
        if (index < 0 || index + 1 >= args.Length || !int.TryParse(args[index + 1], out var pid))
        {
            Log.Warn("Watchdog: chybí PID hlavního procesu.");
            return;
        }

        Log.Info($"Watchdog hlídá PID {pid}.");
        try
        {
            using var parent = Process.GetProcessById(pid);
            parent.WaitForExit();
        }
        catch (ArgumentException)
        {
            // Proces už neběží.
        }

        // Hlavní proces při běžném ukončení soubor maže těsně před koncem – chvilku počkáme.
        Thread.Sleep(750);
        var restored = SystemGuard.RestoreFromFile($"watchdog: proces {pid} skončil");
        Log.Info(restored ? "Watchdog obnovil systém." : "Watchdog: nebylo co obnovovat.");
    }
}
