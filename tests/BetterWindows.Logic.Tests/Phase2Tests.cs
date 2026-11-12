using BetterWindows.Logic.Input;
using BetterWindows.Logic.Performance;
using BetterWindows.Logic.Settings;
using BetterWindows.Logic.Start;
using BetterWindows.Logic.Themes;

namespace BetterWindows.Logic.Tests;

public class WinKeyTapTests
{
    private const int E = 0x45;
    private static KeyDecision Press(WinKeyTap t, int vk, bool down, bool enabled = true, bool injected = false) =>
        t.OnKey(vk, down, injected, enabled);

    [Fact]
    public void Lone_tap_toggles_start_and_never_swallows_key_down()
    {
        var t = new WinKeyTap();
        Assert.Equal(KeyDecision.Pass, Press(t, WinKeyTap.VkLWin, true));
        Assert.Equal(KeyDecision.SwallowAndToggleStart, Press(t, WinKeyTap.VkLWin, false));
    }

    [Fact]
    public void Shortcut_win_e_passes_everything()
    {
        var t = new WinKeyTap();
        Assert.Equal(KeyDecision.Pass, Press(t, WinKeyTap.VkLWin, true));
        Assert.Equal(KeyDecision.Pass, Press(t, E, true));
        Assert.Equal(KeyDecision.Pass, Press(t, E, false));
        Assert.Equal(KeyDecision.Pass, Press(t, WinKeyTap.VkLWin, false));
        // další osamocený stisk už zase funguje
        Press(t, WinKeyTap.VkLWin, true);
        Assert.Equal(KeyDecision.SwallowAndToggleStart, Press(t, WinKeyTap.VkLWin, false));
    }

    [Fact]
    public void Auto_repeat_of_win_is_still_a_lone_tap()
    {
        var t = new WinKeyTap();
        for (var i = 0; i < 5; i++) Assert.Equal(KeyDecision.Pass, Press(t, WinKeyTap.VkRWin, true));
        Assert.Equal(KeyDecision.SwallowAndToggleStart, Press(t, WinKeyTap.VkRWin, false));
    }

    [Fact]
    public void Both_win_keys_are_not_a_tap()
    {
        var t = new WinKeyTap();
        Press(t, WinKeyTap.VkLWin, true);
        Press(t, WinKeyTap.VkRWin, true);
        Assert.Equal(KeyDecision.Pass, Press(t, WinKeyTap.VkRWin, false));
        Assert.Equal(KeyDecision.Pass, Press(t, WinKeyTap.VkLWin, false));
    }

    [Fact]
    public void Injected_events_are_ignored_and_dont_break_state()
    {
        var t = new WinKeyTap();
        Press(t, WinKeyTap.VkLWin, true);
        Assert.Equal(KeyDecision.Pass, Press(t, 0xE8, true, injected: true)); // naše maskovací klávesa
        Assert.Equal(KeyDecision.Pass, Press(t, WinKeyTap.VkLWin, false, injected: true)); // naše vložené uvolnění
        Assert.Equal(KeyDecision.SwallowAndToggleStart, Press(t, WinKeyTap.VkLWin, false));
    }

    [Fact]
    public void Disabled_passes_through()
    {
        var t = new WinKeyTap();
        Press(t, WinKeyTap.VkLWin, true, enabled: false);
        Assert.Equal(KeyDecision.Pass, Press(t, WinKeyTap.VkLWin, false, enabled: false));
    }

    [Fact]
    public void Up_without_down_passes()
    {
        var t = new WinKeyTap();
        Assert.Equal(KeyDecision.Pass, Press(t, WinKeyTap.VkLWin, false));
    }

    [Fact]
    public void Mouse_while_win_held_cancels_tap()
    {
        var t = new WinKeyTap();
        Press(t, WinKeyTap.VkLWin, true);
        t.OnOtherInput();
        Assert.Equal(KeyDecision.Pass, Press(t, WinKeyTap.VkLWin, false));
    }
}

public class ThemeCodecTests
{
    [Fact]
    public void Round_trip()
    {
        var style = new ThemeStyle { Name = "Oceán ✓", Accent = "#00B2CA", RadiusCard = 14, GlassBlur = 40, GlassOpacity = 0.2, BarTint = 0.3, DockTint = 0.25 };
        var code = ThemeCodec.Encode(style, 56);
        Assert.StartsWith("BW1.", code);
        Assert.True(ThemeCodec.TryDecode(code, out var d, out var err), err);
        Assert.Equal("Oceán", d!.Style.Name); // ✓ odfiltrováno
        Assert.Equal("#00b2ca", d.Style.Accent);
        Assert.Equal(14, d.Style.RadiusCard);
        Assert.Equal(56, d.DockIconSize);
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("BW1.!!!notbase64")]
    [InlineData("BW1.WzEsMl0")] // [1,2]
    public void Invalid_codes_are_rejected(string code)
    {
        Assert.False(ThemeCodec.TryDecode(code, out _, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void Too_long_is_rejected()
    {
        Assert.False(ThemeCodec.TryDecode("BW1." + new string('A', 2000), out _, out _));
    }

    [Fact]
    public void Malicious_values_are_neutralized()
    {
        // {"a":"red;}body{display:none","r":1e9,"g":-5,"x":"evil","n":"<script>"}
        var json = """{"a":"red;}body{display:none","r":1e9,"g":-5,"x":"evil","n":"<script>alert(1)</script>"}""";
        var code = "BW1." + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Assert.True(ThemeCodec.TryDecode(code, out var d, out _));
        Assert.Equal("#3a6df0", d!.Style.Accent);
        Assert.Equal(40, d.Style.RadiusCard);
        Assert.Equal(0, d.Style.GlassOpacity);
        Assert.DoesNotContain("<", d.Style.Name);
        Assert.Null(d.DockIconSize);
    }

    [Fact]
    public void Normalizer_sanitizes_style_from_file()
    {
        var s = new AppSettings();
        s.Appearance.Style = new ThemeStyle { Accent = "url(javascript:x)", GlassBlur = double.PositiveInfinity };
        SettingsNormalizer.Normalize(s);
        Assert.Equal("#3a6df0", s.Appearance.Style.Accent);
        Assert.Equal(28, s.Appearance.Style.GlassBlur);
    }

    [Fact]
    public void Old_settings_file_without_new_sections_loads()
    {
        using var dir = new TempDir();
        var path = dir.File("settings.json");
        File.WriteAllText(path, """{"version":1,"weather":{"city":"Brno"}}""");
        var s = new SettingsStore(path).Load(out var warning);
        Assert.Null(warning);
        Assert.False(s.Start.ReplaceWinKey); // klávesa Win se sama nepřevezme
        Assert.True(s.Island.Enabled);
        Assert.True(s.Dock.Previews);
        Assert.Equal("Brno", s.Weather.City);
    }
}

public class StartAppsTests
{
    private static readonly Guid ProgramFiles = Guid.Parse("6D809377-6AF0-444B-8957-A3773F02200E");

    private static string? Folder(Guid g) => g == ProgramFiles ? @"C:\Program Files" : null;

    [Fact]
    public void Known_folder_exe_becomes_exe_identity()
    {
        Assert.Equal(@"exe:c:\program files\mozilla firefox\firefox.exe",
            StartApps.ToAppId(@"{6D809377-6AF0-444B-8957-A3773F02200E}\Mozilla Firefox\firefox.exe", Folder));
    }

    [Fact]
    public void Aumid_and_unknown_folder_become_aumid()
    {
        Assert.Equal("aumid:Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", StartApps.ToAppId("Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", Folder));
        Assert.Equal("aumid:Chrome", StartApps.ToAppId("Chrome", Folder));
        Assert.StartsWith("aumid:", StartApps.ToAppId(@"{00000000-0000-0000-0000-000000000001}\x\a.exe", Folder));
    }

    [Fact]
    public void Hides_uninstallers_and_documents()
    {
        Assert.True(StartApps.ShouldHide("Uninstall Foo", "x"));
        Assert.True(StartApps.ShouldHide("Odinstalovat", "x"));
        Assert.True(StartApps.ShouldHide("Web", "https://example.com"));
        Assert.False(StartApps.ShouldHide("Kalkulačka", "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App"));
    }

    [Fact]
    public void Normalize_sorts_czech_and_dedupes()
    {
        var list = StartApps.Normalize([new("a:1", "Žirafa"), new("a:2", "Čáp"), new("A:1", "dup"), new("a:3", "Cesta")]);
        Assert.Equal(["Cesta", "Čáp", "Žirafa"], list.Select(a => a.Name));
    }
}

public class PerformancePolicyTests
{
    [Fact]
    public void Auto_reacts_to_battery_and_fullscreen()
    {
        var s = new AppSettings();
        Assert.False(PerformancePolicy.Evaluate(s, false, false, false).Reduced);
        Assert.True(PerformancePolicy.Evaluate(s, true, false, false).Reduced);
        Assert.True(PerformancePolicy.Evaluate(s, false, true, false).Reduced);
        Assert.True(PerformancePolicy.Evaluate(s, false, false, true).Reduced);
    }

    [Fact]
    public void Manual_reduce_motion_and_modes_win()
    {
        var s = new AppSettings();
        s.Performance.Mode = PerformanceMode.NeverReduced;
        Assert.False(PerformancePolicy.Evaluate(s, true, true, true).Reduced);
        s.Appearance.ReduceMotion = true;
        Assert.True(PerformancePolicy.Evaluate(s, false, false, false).Reduced);
        s.Appearance.ReduceMotion = false;
        s.Performance.Mode = PerformanceMode.AlwaysReduced;
        Assert.True(PerformancePolicy.Evaluate(s, false, false, false).Reduced);
    }

    [Fact]
    public void Battery_option_can_be_disabled()
    {
        var s = new AppSettings();
        s.Performance.ReduceOnBattery = false;
        Assert.False(PerformancePolicy.Evaluate(s, true, false, false).Reduced);
    }
}
