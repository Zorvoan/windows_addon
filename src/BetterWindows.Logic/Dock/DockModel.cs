using BetterWindows.Logic.Apps;
using BetterWindows.Logic.Messaging;
using BetterWindows.Logic.Settings;

namespace BetterWindows.Logic.Dock;

/// <summary>Jedno "skutečné" okno aplikace, jak ho vidí WindowTracker.</summary>
/// <param name="ZOrder">0 = nejvýš (naposledy aktivní).</param>
/// <param name="FirstSeen">Pořadové číslo, kdy se okno poprvé objevilo (stabilní pořadí v docku).</param>
public sealed record AppWindowInfo(long Hwnd, string AppId, string AppName, string Title, bool IsMinimized, int ZOrder, long FirstSeen);

/// <summary>Co udělat po kliknutí na ikonu v docku.</summary>
public abstract record DockAction
{
    public sealed record Launch(string AppId) : DockAction;
    public sealed record Activate(long Hwnd) : DockAction;
    public sealed record Minimize(IReadOnlyList<long> Hwnds) : DockAction;
}

public static class DockModel
{
    /// <summary>
    /// Seznam ikon v docku: nejdřív připnuté (v pořadí připnutí), pak spuštěné nepřipnuté
    /// (v pořadí, jak se poprvé objevily – aby ikony neposkakovaly při přepínání oken).
    /// </summary>
    public static IReadOnlyList<DockItem> BuildItems(
        IReadOnlyList<PinnedApp> pinned, IReadOnlyList<AppWindowInfo> windows, string? foregroundAppId)
    {
        var byApp = windows
            .GroupBy(w => w.AppId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var items = new List<DockItem>();
        foreach (var p in pinned)
        {
            byApp.TryGetValue(p.AppId, out var ws);
            var name = ws?.FirstOrDefault()?.AppName is { Length: > 0 } running ? running : p.Name;
            items.Add(new DockItem(p.AppId, string.IsNullOrWhiteSpace(p.Name) ? name : p.Name,
                Pinned: true, Running: ws is { Count: > 0 }, Active: AppId.Equal(p.AppId, foregroundAppId),
                WindowCount: ws?.Count ?? 0));
        }

        var pinnedIds = new HashSet<string>(pinned.Select(p => p.AppId), StringComparer.OrdinalIgnoreCase);
        foreach (var (appId, ws) in byApp.Where(kv => !pinnedIds.Contains(kv.Key)).OrderBy(kv => kv.Value.Min(w => w.FirstSeen)))
        {
            items.Add(new DockItem(appId, ws[0].AppName, Pinned: false, Running: true,
                Active: AppId.Equal(appId, foregroundAppId), WindowCount: ws.Count));
        }
        return items;
    }

    /// <summary>
    /// Klik na ikonu: neběží -> spustit; běží a je v popředí -> minimalizovat; jinak -> přepnout na ni.
    /// </summary>
    public static DockAction DecideClick(string appId, IReadOnlyList<AppWindowInfo> windows, string? foregroundAppId)
    {
        var appWindows = windows.Where(w => AppId.Equal(w.AppId, appId)).OrderBy(w => w.ZOrder).ToList();
        if (appWindows.Count == 0)
            return new DockAction.Launch(appId);

        var visible = appWindows.Where(w => !w.IsMinimized).ToList();
        if (AppId.Equal(appId, foregroundAppId) && visible.Count > 0)
            return new DockAction.Minimize(visible.Select(w => w.Hwnd).ToList());

        var target = visible.FirstOrDefault() ?? appWindows[0];
        return new DockAction.Activate(target.Hwnd);
    }
}
