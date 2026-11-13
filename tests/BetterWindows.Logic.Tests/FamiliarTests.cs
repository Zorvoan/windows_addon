using BetterWindows.Logic.Lock;
using BetterWindows.Logic.Settings;

namespace BetterWindows.Logic.Tests;

public class FamiliarTests
{
    [Fact]
    public void Idle_lock_only_when_enabled_and_nothing_playing()
    {
        var ten = TimeSpan.FromMinutes(10);
        Assert.False(IdlePolicy.ShouldShow(0, ten, false, false, false, false)); // vypnuto
        Assert.True(IdlePolicy.ShouldShow(5, ten, false, false, false, false));
        Assert.False(IdlePolicy.ShouldShow(5, TimeSpan.FromMinutes(4), false, false, false, false));
        Assert.False(IdlePolicy.ShouldShow(5, ten, true, false, false, false));
        Assert.False(IdlePolicy.ShouldShow(5, ten, false, true, false, false)); // hraje hudba
        Assert.False(IdlePolicy.ShouldShow(5, ten, false, false, true, false)); // hra
        Assert.False(IdlePolicy.ShouldShow(5, ten, false, false, false, true)); // video drží obrazovku
    }

    [Fact]
    public void Keyboard_language_codes()
    {
        Assert.Equal("CES", KeyboardLanguage.Describe(0x0405).Code);
        Assert.Equal("ENG", KeyboardLanguage.Describe(0x0409).Code);
        Assert.Equal("ENG", KeyboardLanguage.Describe(0x0809).Code);
    }

    [Fact]
    public void Lock_defaults_and_clamping()
    {
        var s = new AppSettings();
        Assert.True(s.Lock.RequireWindowsLock);
        Assert.Equal(0, s.Lock.IdleMinutes); // po nečinnosti se sama neukáže
        s.Lock.Dim = 5;
        s.Lock.IdleMinutes = -3;
        SettingsNormalizer.Normalize(s);
        Assert.Equal(0.7, s.Lock.Dim);
        Assert.Equal(0, s.Lock.IdleMinutes);
    }
}
