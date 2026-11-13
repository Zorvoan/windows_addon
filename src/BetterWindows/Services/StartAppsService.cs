using System.Runtime.InteropServices;
using System.Threading;
using BetterWindows.Core;
using BetterWindows.Logic.Start;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace BetterWindows.Services;

/// <summary>
/// Seznam aplikací pro Start: složka Shellu "shell:AppsFolder" = přesně to, co ukazuje
/// "Všechny aplikace" v nabídce Start Windows (klasické aplikace i aplikace ze Storu).
/// Procházení COM objektů Shellu chce STA vlákno a trvá – proto vlastní vlákno.
/// </summary>
internal sealed class StartAppsService
{
    private readonly object _gate = new();
    private DateTime _loadedAt = DateTime.MinValue;
    private bool _loading;

    public IReadOnlyList<StartApp> Apps { get; private set; } = [];

    /// <summary>Seznam je načtený. Volá se z pracovního vlákna!</summary>
    public event Action<IReadOnlyList<StartApp>>? Loaded;

    /// <summary>Načte seznam, pokud je starší než 10 minut (nebo ještě nebyl).</summary>
    public void RefreshIfStale()
    {
        lock (_gate)
        {
            if (_loading || DateTime.UtcNow - _loadedAt < TimeSpan.FromMinutes(10))
                return;
            _loading = true;
        }
        var thread = new Thread(Load) { IsBackground = true, Name = "StartApps" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    private void Load()
    {
        try
        {
            var apps = StartApps.Normalize(Enumerate());
            Apps = apps;
            _loadedAt = DateTime.UtcNow;
            Log.Info($"Start: načteno {apps.Count} aplikací.");
            Loaded?.Invoke(apps);
        }
        catch (Exception ex)
        {
            Log.Error("Start: seznam aplikací se nepodařilo načíst", ex);
        }
        finally
        {
            lock (_gate)
                _loading = false;
        }
    }

    private static unsafe List<StartApp> Enumerate()
    {
        var result = new List<StartApp>();
        var folderId = PInvoke.FOLDERID_AppsFolder;
        var iidItem = typeof(IShellItem).GUID;
        PInvoke.SHGetKnownFolderItem(&folderId, KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, HANDLE.Null, &iidItem, out var folderObj).ThrowOnFailure();
        var folder = (IShellItem)folderObj;
        try
        {
            folder.BindToHandler<IEnumShellItems>(null, PInvoke.BHID_EnumItems, out var items);
            try
            {
                var batch = new IShellItem[1];
                while (true)
                {
                    // Na konci seznamu vrátí 0 položek (S_FALSE), chyby vyhodí výjimku.
                    items.Next(batch, out var fetched);
                    if (fetched == 0)
                        break;
                    var item = batch[0];
                    try
                    {
                        var name = DisplayName(item, SIGDN.SIGDN_NORMALDISPLAY);
                        var parsing = DisplayName(item, SIGDN.SIGDN_PARENTRELATIVEPARSING);
                        if (name is null || parsing is null || StartApps.ShouldHide(name, parsing))
                            continue;
                        result.Add(new StartApp(StartApps.ToAppId(parsing, KnownFolderPath), name));
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(item);
                    }
                }
            }
            finally
            {
                Marshal.ReleaseComObject(items);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(folder);
        }
        return result;
    }

    private static unsafe string? DisplayName(IShellItem item, SIGDN kind)
    {
        try
        {
            item.GetDisplayName(kind, out var name);
            try { return name.ToString(); }
            finally { PInvoke.CoTaskMemFree(name.Value); }
        }
        catch (COMException)
        {
            return null;
        }
    }

    private static readonly Dictionary<Guid, string?> FolderCache = [];

    /// <summary>Cesta známé složky (Program Files apod.) podle jejího GUID.</summary>
    private static unsafe string? KnownFolderPath(Guid id)
    {
        lock (FolderCache)
        {
            if (FolderCache.TryGetValue(id, out var cached))
                return cached;
            string? path = null;
            if (PInvoke.SHGetKnownFolderPath(id, KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, null, out var handle).Succeeded)
            {
                using (handle)
                    path = new PWSTR((char*)handle.DangerousGetHandle()).ToString();
            }
            FolderCache[id] = path;
            return path;
        }
    }
}
