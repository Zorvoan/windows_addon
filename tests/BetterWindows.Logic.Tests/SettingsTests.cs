using BetterWindows.Logic.Settings;

namespace BetterWindows.Logic.Tests;

public class SettingsTests
{
    [Fact]
    public void Missing_file_gives_defaults()
    {
        using var dir = new TempDir();
        var s = new SettingsStore(dir.File("settings.json")).Load(out var warning);
        Assert.Null(warning);
        Assert.False(s.Taskbar.Hide); // hlavní panel se nikdy neskryje sám od sebe
        Assert.Equal("Praha", s.Weather.City);
        Assert.Equal(DesktopMode.Auto, s.Desktop.Mode);
        Assert.NotEmpty(s.Desktop.Widgets);
    }

    [Fact]
    public void Round_trip_preserves_values()
    {
        using var dir = new TempDir();
        var store = new SettingsStore(dir.File("settings.json"));
        var s = new AppSettings();
        s.Weather.City = "Brno";
        s.Dock.Pinned.Add(new PinnedApp { AppId = "exe:c:\\x\\a.exe", Name = "A" });
        s.Appearance.TopBarBackdrop = BackdropMode.AccentAcrylic;
        store.Save(s);

        var loaded = store.Load(out var warning);
        Assert.Null(warning);
        Assert.Equal("Brno", loaded.Weather.City);
        Assert.Contains(loaded.Dock.Pinned, p => p.AppId == "exe:c:\\x\\a.exe");
        Assert.Equal(BackdropMode.AccentAcrylic, loaded.Appearance.TopBarBackdrop);
        Assert.Contains("\"accentAcrylic\"", File.ReadAllText(store.Path));
    }

    [Fact]
    public void Corrupt_file_is_backed_up_and_defaults_used()
    {
        using var dir = new TempDir();
        var path = dir.File("settings.json");
        File.WriteAllText(path, "{ this is not json");
        var s = new SettingsStore(path).Load(out var warning);
        Assert.NotNull(warning);
        Assert.Equal("Praha", s.Weather.City);
        Assert.False(File.Exists(path));
        Assert.Single(Directory.GetFiles(dir.Path, "settings.corrupt-*.json"));
    }

    [Fact]
    public void Normalizer_clamps_and_dedupes()
    {
        var s = new AppSettings();
        s.Dock.IconSize = 500;
        s.Dock.MaxScale = double.NaN;
        s.Dock.Pinned = [new() { AppId = "exe:a" }, new() { AppId = "EXE:A" }, new() { AppId = "" }];
        s.Weather.City = "  ";
        s.Desktop.Widgets = [new() { Id = "x", Type = "unknown" }, new() { Id = "c", Type = WidgetTypes.Clock, X = -5 }];
        SettingsNormalizer.Normalize(s);
        Assert.Equal(96, s.Dock.IconSize);
        Assert.Equal(1.7, s.Dock.MaxScale);
        Assert.Single(s.Dock.Pinned);
        Assert.Equal("Praha", s.Weather.City);
        Assert.Single(s.Desktop.Widgets);
        Assert.Equal(0, s.Desktop.Widgets[0].X);
    }

    [Fact]
    public void Changing_city_drops_resolved_location()
    {
        var s = new AppSettings();
        s.Weather.Resolved = new ResolvedLocation { Query = "Praha", Name = "Praha" };
        s.Weather.City = "Ostrava";
        SettingsNormalizer.Normalize(s);
        Assert.Null(s.Weather.Resolved);
    }

    [Fact]
    public void Unknown_enum_value_counts_as_corrupt_not_crash()
    {
        using var dir = new TempDir();
        var path = dir.File("settings.json");
        File.WriteAllText(path, """{ "appearance": { "theme": "purple" } }""");
        var s = new SettingsStore(path).Load(out var warning);
        Assert.NotNull(warning);
        Assert.Equal(ThemeMode.System, s.Appearance.Theme);
    }
}
