using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using BetterWindows.Core;
using BetterWindows.Logic.Apps;
using BetterWindows.Native;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.PropertiesSystem;

namespace BetterWindows.Services;

internal sealed record AppIdentity(string AppId, string Name);

/// <summary>
/// Zjistí, ke které aplikaci okno patří:
///  - aplikace z balíčku (UWP/MSIX, např. Kalkulačka, nový Poznámkový blok, Terminal) -> AUMID,
///  - UWP okna patří procesu ApplicationFrameHost.exe ("rámeček"); skutečnou aplikaci najdeme
///    v jeho podokně (CoreWindow jiného procesu), případně z vlastností okna,
///  - ostatní -> cesta k exe.
/// Výsledky se cachují podle procesu (a u ApplicationFrameHost podle okna).
/// </summary>
internal sealed class AppIdentityResolver
{
    private readonly Dictionary<(uint Pid, DateTime Start), AppIdentity> _byProcess = [];
    private readonly Dictionary<nint, AppIdentity> _byFrameWindow = [];
    private readonly Dictionary<string, string> _names = new(StringComparer.OrdinalIgnoreCase);

    public AppIdentity? Resolve(HWND hwnd)
    {
        var pid = WindowInfo.GetProcessId(hwnd);
        if (pid == 0)
            return null;

        var path = GetProcessPath(pid);
        if (path is not null && Path.GetFileName(path).Equals("ApplicationFrameHost.exe", StringComparison.OrdinalIgnoreCase))
        {
            if (_byFrameWindow.TryGetValue((nint)hwnd, out var cachedFrame))
                return cachedFrame;
            var frameId = ResolveFrameHostWindow(hwnd, pid);
            if (frameId is not null)
                _byFrameWindow[(nint)hwnd] = frameId;
            return frameId ?? new AppIdentity(AppId.FromExePath(path), WindowInfo.GetTitle(hwnd));
        }

        var key = (pid, GetStartTime(pid));
        if (_byProcess.TryGetValue(key, out var cached))
            return cached;

        AppIdentity identity;
        var aumid = GetPackagedAumid(pid);
        if (aumid is not null)
            identity = new AppIdentity(AppId.FromAumid(aumid), GetAumidName(aumid));
        else if (path is not null)
            identity = new AppIdentity(AppId.FromExePath(path), GetExeName(path));
        else
            identity = new AppIdentity("proc:" + SafeProcessName(pid), SafeProcessName(pid));

        _byProcess[key] = identity;
        return identity;
    }

    /// <summary>Okno zaniklo – zapomenout cache rámečku UWP.</summary>
    public void Forget(nint hwnd) => _byFrameWindow.Remove(hwnd);

    /// <summary>Čitelné jméno aplikace pro připnutou ikonu (bez běžícího okna).</summary>
    public string GetName(string appId)
    {
        var (kind, value) = AppId.Parse(appId);
        return kind switch
        {
            AppIdKind.Aumid => GetAumidName(value),
            AppIdKind.Exe => GetExeName(value),
            _ => value,
        };
    }

    // ---------- UWP rámeček ----------

    private AppIdentity? ResolveFrameHostWindow(HWND frame, uint framePid)
    {
        // Podokno "Windows.UI.Core.CoreWindow" patří procesu skutečné aplikace.
        uint appPid = 0;
        PInvoke.EnumChildWindows(frame, (child, _) =>
        {
            var childPid = WindowInfo.GetProcessId(child);
            if (childPid != framePid && childPid != 0)
            {
                appPid = childPid;
                return false;
            }
            return true;
        }, 0);

        var aumid = appPid != 0 ? GetPackagedAumid(appPid) : null;
        // Minimalizovaná UWP aplikace CoreWindow odpojí -> zkusit vlastnost okna AppUserModelID.
        aumid ??= GetWindowAumid(frame);
        return aumid is null ? null : new AppIdentity(AppId.FromAumid(aumid), GetAumidName(aumid));
    }

    private static unsafe string? GetWindowAumid(HWND hwnd)
    {
        try
        {
            var iid = typeof(IPropertyStore).GUID;
            if (PInvoke.SHGetPropertyStoreForWindow(hwnd, &iid, out var obj).Failed || obj is not IPropertyStore store)
                return null;
            try
            {
                var key = PInvoke.PKEY_AppUserModel_ID;
                store.GetValue(&key, out var value);
                try
                {
                    if (PInvoke.PropVariantToStringAlloc(value, out var text).Failed)
                        return null;
                    try { return text.ToString(); }
                    finally { PInvoke.CoTaskMemFree(text.Value); }
                }
                finally
                {
                    PInvoke.PropVariantClear(ref value);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(store);
            }
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException)
        {
            return null;
        }
    }

    // ---------- Procesy ----------

    /// <summary>AUMID procesu z balíčku (UWP/MSIX), nebo null pro klasickou aplikaci.</summary>
    private static string? GetPackagedAumid(uint pid)
    {
        using var process = OpenProcess(pid);
        if (process is null)
            return null;
        // AUMID má max. 130 znaků (APPLICATION_USER_MODEL_ID_MAX_LENGTH).
        Span<char> buffer = stackalloc char[256];
        uint length = (uint)buffer.Length;
        var result = PInvoke.GetApplicationUserModelId(process, ref length, buffer);
        if (result != WIN32_ERROR.ERROR_SUCCESS || length <= 1)
            return null; // APPMODEL_ERROR_NO_APPLICATION = klasická aplikace bez balíčku
        return new string(buffer[..((int)length - 1)]);
    }

    public static string? GetProcessPath(uint pid)
    {
        using var process = OpenProcess(pid);
        if (process is null)
            return null;
        Span<char> buffer = stackalloc char[1024];
        uint size = (uint)buffer.Length;
        if (!PInvoke.QueryFullProcessImageName(process, PROCESS_NAME_FORMAT.PROCESS_NAME_WIN32, buffer, ref size))
            return null;
        return new string(buffer[..(int)size]);
    }

    /// <summary>PROCESS_QUERY_LIMITED_INFORMATION = nejmenší oprávnění; funguje i na většinu cizích procesů.</summary>
    private static Microsoft.Win32.SafeHandles.SafeFileHandle? OpenProcess(uint pid)
    {
        var handle = PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (handle.IsInvalid)
        {
            handle.Dispose();
            return null;
        }
        return handle;
    }

    private static DateTime GetStartTime(uint pid)
    {
        try
        {
            using var p = Process.GetProcessById((int)pid);
            return p.StartTime;
        }
        catch
        {
            return DateTime.MinValue; // přístup odepřen (proces správce) – PID stačí
        }
    }

    private static string SafeProcessName(uint pid)
    {
        try
        {
            using var p = Process.GetProcessById((int)pid);
            return p.ProcessName;
        }
        catch
        {
            return $"PID {pid}";
        }
    }

    // ---------- Jména ----------

    private string GetExeName(string path)
    {
        if (_names.TryGetValue(path, out var cached))
            return cached;

        var file = Path.GetFileName(path);
        string name;
        if (file.Equals("explorer.exe", StringComparison.OrdinalIgnoreCase))
        {
            name = "Průzkumník souborů";
        }
        else
        {
            name = Safe.Run("Jméno aplikace", () =>
            {
                var info = FileVersionInfo.GetVersionInfo(path);
                var desc = info.FileDescription?.Trim();
                if (!string.IsNullOrEmpty(desc) && desc.Length < 60)
                    return desc;
                return string.IsNullOrWhiteSpace(info.ProductName) ? null : info.ProductName.Trim();
            }) ?? Path.GetFileNameWithoutExtension(path);
        }
        _names[path] = name;
        return name;
    }

    /// <summary>Jméno aplikace z balíčku podle Shellu (to, co vidíš v nabídce Start).</summary>
    private string GetAumidName(string aumid)
    {
        if (_names.TryGetValue(aumid, out var cached))
            return cached;
        var name = Safe.Run("Jméno aplikace z balíčku", () => ShellItems.GetDisplayName("shell:AppsFolder\\" + aumid)) ?? aumid;
        _names[aumid] = name;
        return name;
    }
}
