using BetterWindows.Logic.SystemState;

namespace BetterWindows.Logic.Tests;

public class SystemStateTests
{
    [Fact]
    public void Nothing_to_restore_when_no_file()
    {
        Assert.True(RestorePlanner.Plan(null, corrupt: false).IsEmpty);
    }

    [Fact]
    public void Corrupt_file_restores_everything_visible()
    {
        var plan = RestorePlanner.Plan(null, corrupt: true);
        Assert.True(plan.ShowDesktopIcons);
        Assert.True(plan.ShowTaskbar);
        Assert.Null(plan.TaskbarStateToRestore); // původní stav neznáme -> automatické skrývání neměníme
    }

    [Fact]
    public void Restores_exactly_what_was_changed()
    {
        var plan = RestorePlanner.Plan(new SystemStateData { TaskbarHidden = true, TaskbarOriginalState = 2, AppBarHwnd = 42 }, false);
        Assert.False(plan.ShowDesktopIcons);
        Assert.True(plan.ShowTaskbar);
        Assert.Equal(2, plan.TaskbarStateToRestore);
        Assert.Equal(42, plan.AppBarToRemove);
    }

    [Fact]
    public void Clean_state_deletes_file_and_dirty_state_survives_reload()
    {
        using var dir = new TempDir();
        var file = new SystemStateFile(dir.File("system-state.json"));
        file.Save(new SystemStateData { DesktopIconsHidden = true, OwnerPid = 5 });
        var loaded = file.Load(out var corrupt);
        Assert.False(corrupt);
        Assert.True(loaded!.DesktopIconsHidden);

        file.Save(new SystemStateData());
        Assert.False(File.Exists(file.Path));
    }

    [Fact]
    public void Corrupt_file_is_reported()
    {
        using var dir = new TempDir();
        File.WriteAllText(dir.File("system-state.json"), "garbage{");
        var file = new SystemStateFile(dir.File("system-state.json"));
        Assert.Null(file.Load(out var corrupt));
        Assert.True(corrupt);
    }
}
