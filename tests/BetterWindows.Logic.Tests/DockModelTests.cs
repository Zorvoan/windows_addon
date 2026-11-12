using BetterWindows.Logic.Apps;
using BetterWindows.Logic.Dock;
using BetterWindows.Logic.Settings;

namespace BetterWindows.Logic.Tests;

public class DockModelTests
{
    private static AppWindowInfo W(long h, string app, int z, long seen, bool min = false) =>
        new(h, app, app.ToUpperInvariant(), "t", min, z, seen);

    [Fact]
    public void Pinned_first_then_running_in_first_seen_order()
    {
        var pinned = new List<PinnedApp> { new() { AppId = "exe:p", Name = "P" }, new() { AppId = "exe:a", Name = "A" } };
        var windows = new[] { W(1, "exe:b", 0, 5), W(2, "exe:a", 1, 3), W(3, "exe:c", 2, 1) };
        var items = DockModel.BuildItems(pinned, windows, "exe:a");

        Assert.Equal(["exe:p", "exe:a", "exe:c", "exe:b"], items.Select(i => i.AppId));
        Assert.False(items[0].Running);
        Assert.True(items[1].Running && items[1].Active && items[1].Pinned);
        Assert.False(items[2].Pinned);
    }

    [Fact]
    public void Click_not_running_launches()
    {
        Assert.Equal(new DockAction.Launch("exe:x"), DockModel.DecideClick("exe:x", [], null));
    }

    [Fact]
    public void Click_running_in_background_activates_topmost_visible_window()
    {
        var windows = new[] { W(10, "exe:a", 3, 1, min: true), W(11, "exe:a", 5, 2), W(12, "exe:a", 4, 3) };
        Assert.Equal(new DockAction.Activate(12), DockModel.DecideClick("exe:a", windows, "exe:other"));
    }

    [Fact]
    public void Click_foreground_app_minimizes_its_visible_windows()
    {
        var windows = new[] { W(10, "exe:a", 0, 1), W(11, "exe:a", 1, 2), W(12, "exe:a", 2, 3, min: true) };
        var action = Assert.IsType<DockAction.Minimize>(DockModel.DecideClick("exe:a", windows, "EXE:A"));
        Assert.Equal([10L, 11L], action.Hwnds);
    }

    [Fact]
    public void Click_foreground_app_with_all_minimized_restores()
    {
        var windows = new[] { W(10, "exe:a", 0, 1, min: true) };
        Assert.Equal(new DockAction.Activate(10), DockModel.DecideClick("exe:a", windows, "exe:a"));
    }

    [Fact]
    public void AppId_parsing()
    {
        Assert.Equal((AppIdKind.Exe, "c:\\a b\\x.exe"), AppId.Parse(AppId.FromExePath(@"C:\A B\x.exe")));
        Assert.Equal((AppIdKind.Aumid, "Foo_123!App"), AppId.Parse(AppId.FromAumid("Foo_123!App")));
        Assert.Equal(AppIdKind.Unknown, AppId.Parse("weird").Kind);
    }
}
