using System.Text.Json;
using BetterWindows.Logic.Messaging;
using BetterWindows.Logic.Settings;

namespace BetterWindows.Logic.Tests;

/// <summary>
/// Kontrakt C# -> UI: zprávy serializované přesně tak, jak je posílá aplikace.
/// Test je zároveň zapíše do ui/harness/fixtures.json, odkud je bere UI harness
/// (npm run harness) a vykreslí s nimi všechny stránky. Rozdíl ve tvaru se tak projeví.
/// </summary>
public class ContractTests
{
    public static IEnumerable<(string Type, object Payload)> SampleHostMessages()
    {
        var settings = new AppSettings();
        settings.Desktop.ShowTestCard = true;
        yield return (MessageTypes.SettingsState, new SettingsStatePayload(settings));
        yield return (MessageTypes.ForegroundChanged, new ForegroundChangedPayload("exe:c:\\windows\\notepad.exe", "Poznámkový blok", "Bez názvu – Poznámkový blok"));
        yield return (MessageTypes.DockState, new DockStatePayload(
        [
            new DockItem("exe:c:\\windows\\explorer.exe", "Průzkumník souborů", true, true, false, 2),
            new DockItem("aumid:windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel", "Nastavení", true, false, false, 0),
            new DockItem("exe:c:\\windows\\notepad.exe", "Poznámkový blok", false, true, true, 1),
            new DockItem("exe:c:\\program files\\app\\app.exe", "Aplikace s dlouhým názvem", false, true, false, 1),
        ], TrashCount: 3));
        yield return (MessageTypes.IconsResult, new IconsResultPayload(new Dictionary<string, string>
        {
            ["exe:c:\\windows\\notepad.exe"] = TinyPngDataUrl,
        }));
        yield return (MessageTypes.BatteryState, new BatteryStatePayload(true, 76, true, true));
        yield return (MessageTypes.WeatherState, new WeatherStatePayload("ok", "Praha", 4.3, 3, false, 8.1, 1.2, DateTimeOffset.Parse("2026-11-12T22:00:00+01:00"), null));
        yield return (MessageTypes.PrivacyState, new PrivacyStatePayload(true, true, ["Zoom"], ["WindowsCamera"]));
        yield return (MessageTypes.WallpaperState, new WallpaperStatePayload(null, "#2b3a4a"));
        yield return (MessageTypes.DesktopStatus, new DesktopStatusPayload("embed-raised", true, "Progman (24H2+)"));
        yield return (MessageTypes.VisualizerFrame, new VisualizerFramePayload(Enumerable.Range(0, 48).Select(i => (float)Math.Round(0.5 + 0.45 * Math.Sin(i / 4.0), 3)).ToList()));
        yield return (MessageTypes.StoreValue, new StoreValuePayload("reminders", JsonSerializer.SerializeToElement(new[] { new { id = "1", text = "Koupit mléko", done = false } })));
        yield return (MessageTypes.PerformanceState, new PerformanceStatePayload(false, "Plné animace"));
        yield return (MessageTypes.MediaState, new MediaStatePayload(true, "Testovací skladba s dlouhým názvem", "Interpret", "Spotify", TinyPngDataUrl, true, 83, 215, true, true));
        yield return (MessageTypes.VolumeState, new VolumeStatePayload(0.42, false, false));
        yield return (MessageTypes.StartApps, new StartAppsPayload(
        [
            new StartAppItem("aumid:Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", "Kalkulačka"),
            new StartAppItem("exe:c:\\windows\\notepad.exe", "Poznámkový blok"),
            new StartAppItem("exe:c:\\program files\\mozilla firefox\\firefox.exe", "Firefox"),
            new StartAppItem("aumid:windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel", "Nastavení"),
            new StartAppItem("aumid:Microsoft.Paint_8wekyb3d8bbwe!App", "Malování"),
            new StartAppItem("aumid:Microsoft.WindowsTerminal_8wekyb3d8bbwe!App", "Terminál"),
        ]));
        yield return (MessageTypes.StartVisibility, new StartVisibilityPayload(true));
        yield return (MessageTypes.LockVisibility, new LockVisibilityPayload(true, false));
        yield return (MessageTypes.UserState, new UserStatePayload("Jan Novák", null));
        yield return (MessageTypes.InputLanguage, new InputLanguagePayload("CES", "čeština"));
        yield return (MessageTypes.Diagnostics, new DiagnosticsPayload("0.1.0", "10.0.26200", "C:\\logs", "C:\\data", "embed-raised", "Progman (24H2+)"));
    }

    // 1×1 průhledný PNG
    private const string TinyPngDataUrl =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

    [Fact]
    public void Envelope_round_trip_and_casing()
    {
        var json = Envelope.Serialize(MessageTypes.BatteryState, new BatteryStatePayload(true, 50, false, true));
        Assert.Contains("\"type\":\"battery.state\"", json);
        Assert.Contains("\"onAc\":true", json);
        Assert.True(Envelope.TryParse(json, out var type, out var payload));
        Assert.Equal("battery.state", type);
        Assert.Equal(50, Envelope.Read<BatteryStatePayload>(payload)!.Percent);
    }

    [Fact]
    public void Invalid_envelopes_are_rejected()
    {
        Assert.False(Envelope.TryParse("not json", out _, out _));
        Assert.False(Envelope.TryParse("[1,2]", out _, out _));
        Assert.False(Envelope.TryParse("""{"payload":{}}""", out _, out _));
    }

    [Fact]
    public void Ui_messages_deserialize()
    {
        Assert.True(Envelope.TryParse("""{"type":"dock.layout","payload":{"hit":[{"x":1,"y":2,"width":3,"height":4}],"band":null}}""", out _, out var p));
        var layout = Envelope.Read<DockLayoutPayload>(p)!;
        Assert.Equal(3, layout.Hit[0].Width);

        Assert.True(Envelope.TryParse("""{"type":"settings.update","payload":{"settings":{"appearance":{"theme":"dark"}}}}""", out _, out var s));
        Assert.Equal(ThemeMode.Dark, Envelope.Read<SettingsUpdatePayload>(s)!.Settings.Appearance.Theme);
    }

    [Fact]
    public void Writes_fixtures_for_ui_harness()
    {
        var root = FindRepoRoot();
        var messages = SampleHostMessages().Select(m => JsonDocument.Parse(Envelope.Serialize(m.Type, m.Payload)).RootElement).ToList();
        Assert.Equal(messages.Count, messages.Select(m => m.GetProperty("type").GetString()).Distinct().Count());
        if (root is null) return; // mimo repozitář nic nezapisujeme
        var dir = Path.Combine(root, "ui", "harness");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "fixtures.json"), JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string? FindRepoRoot()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "BetterWindows.sln")))
                return d.FullName;
        return null;
    }
}
