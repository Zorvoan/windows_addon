using System.Globalization;
using System.Text.RegularExpressions;
using BetterWindows.Logic.Localization;
using Xunit;

namespace BetterWindows.Logic.Tests;

public class LocTests
{
    [Theory]
    [InlineData("cs-CZ", true)]
    [InlineData("sk-SK", true)]
    [InlineData("en-US", false)]
    [InlineData("de-DE", false)]
    public void Language_FollowsWindowsDisplayLanguage(string culture, bool czech) =>
        Assert.Equal(czech, Loc.IsCzech(new CultureInfo(culture)));

    [Fact]
    public void Translate_CzechIsKey_EnglishFromDictionary_FallbackIsCzech()
    {
        Assert.Equal("Ukončit", Loc.Translate("Ukončit", czech: true));
        Assert.Equal("Quit", Loc.Translate("Ukončit", czech: false));
        Assert.Equal("Tohle v slovníku není", Loc.Translate("Tohle v slovníku není", czech: false));
    }

    /// <summary>Každé Loc.T("…") ve zdrojácích musí mít anglický překlad a nesmí být interpolované (klíč by se měnil).</summary>
    [Fact]
    public void EveryLocKey_HasEnglishTranslation()
    {
        var src = FindSrc();
        var missing = new List<string>();
        var files = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && Path.GetFileName(f) != "Loc.cs") // dokumentační komentář s ukázkou
            .ToList();
        var count = 0;
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            if (text.Contains("Loc.T($\""))
                missing.Add($"{Path.GetFileName(file)}: interpolovaný klíč Loc.T($\"…\")");
            foreach (Match m in Regex.Matches(text, "Loc\\.T\\(\\s*\"((?:[^\"\\\\]|\\\\.)*)\""))
            {
                count++;
                var key = Regex.Unescape(m.Groups[1].Value);
                if (!Loc.HasTranslation(key))
                    missing.Add($"{Path.GetFileName(file)}: {key}");
            }
        }
        Assert.True(count > 40, $"našel jsem jen {count} volání Loc.T – špatná cesta ke zdrojákům?");
        Assert.True(missing.Count == 0, "Chybí překlad:\n" + string.Join("\n", missing));
    }

    private static string FindSrc()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var src = Path.Combine(dir.FullName, "src");
            if (Directory.Exists(Path.Combine(src, "BetterWindows.Logic")))
                return src;
        }
        throw new DirectoryNotFoundException("Nenašel jsem složku src.");
    }
}
