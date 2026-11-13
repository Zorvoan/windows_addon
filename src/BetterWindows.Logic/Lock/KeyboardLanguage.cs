using System.Globalization;
using BetterWindows.Logic.Localization;

namespace BetterWindows.Logic.Lock;

/// <summary>Krátký kód jazyka klávesnice pro horní lištu (jako "ENG" na screenshotech Familiar).</summary>
public static class KeyboardLanguage
{
    /// <param name="langId">Spodních 16 bitů HKL (LANGID), např. 0x0405 = čeština.</param>
    public static (string Code, string Name) Describe(int langId)
    {
        try
        {
            var culture = CultureInfo.GetCultureInfo(langId & 0xFFFF);
            // ISO 639-2 (ces, eng, deu…) – čitelnější než kódy Windows (CSY, ENU).
            var code = culture.ThreeLetterISOLanguageName.ToUpperInvariant();
            if (string.IsNullOrEmpty(code) || code == "IVL")
                code = culture.TwoLetterISOLanguageName.ToUpperInvariant();
            return (code, culture.NativeName);
        }
        catch (CultureNotFoundException)
        {
            return ($"{langId & 0xFFFF:X4}", Loc.T("Neznámý jazyk"));
        }
    }
}
