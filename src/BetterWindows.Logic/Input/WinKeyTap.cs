namespace BetterWindows.Logic.Input;

/// <summary>Co udělat s klávesovou událostí v low-level háčku.</summary>
public enum KeyDecision
{
    /// <summary>Nechat projít (normální chování Windows).</summary>
    Pass,
    /// <summary>
    /// Osamocený stisk klávesy Windows: zahodit TOTO uvolnění klávesy a hned poslat vlastní
    /// sekvenci "maskovací klávesa + uvolnění Win" (Windows pak neotevře svůj Start) a otevřít náš Start.
    /// </summary>
    SwallowAndToggleStart,
}

/// <summary>
/// Stavový automat pro klávesu Windows. Pravidla bezpečnosti:
///  - STISK klávesy Win nikdy nezahazujeme (zkratky Win+E, Win+L... fungují beze změny),
///  - zahodit se smí jen UVOLNĚNÍ po osamoceném stisku – volající ho musí VŽDY nahradit vlastním
///    (vloženým) uvolněním, jinak by Win zůstala "držená",
///  - vložené (injected) události ignorujeme – jsou to i naše vlastní,
///  - když je funkce vypnutá (nastavení, Start nepřipravený, hra přes celou obrazovku) -> vše projde.
/// </summary>
public sealed class WinKeyTap
{
    public const int VkLWin = 0x5B;
    public const int VkRWin = 0x5C;

    private readonly HashSet<int> _winDown = [];
    private bool _comboUsed;

    public KeyDecision OnKey(int vk, bool isDown, bool injected, bool enabled)
    {
        if (injected)
            return KeyDecision.Pass;

        var isWin = vk is VkLWin or VkRWin;
        if (isWin && isDown)
        {
            if (_winDown.Count > 0 && !_winDown.Contains(vk))
                _comboUsed = true; // obě klávesy Win najednou = ne osamocený stisk
            if (_winDown.Count == 0)
                _comboUsed = false;
            _winDown.Add(vk); // opakování (auto-repeat) jen znovu přidá stejnou klávesu
            return KeyDecision.Pass;
        }

        if (isWin)
        {
            var wasDown = _winDown.Remove(vk);
            if (!wasDown || _winDown.Count > 0 || _comboUsed)
            {
                if (_winDown.Count == 0) _comboUsed = false;
                return KeyDecision.Pass;
            }
            _comboUsed = false;
            return enabled ? KeyDecision.SwallowAndToggleStart : KeyDecision.Pass;
        }

        // Jiná klávesa při držené Win = zkratka (Win+E apod.).
        if (_winDown.Count > 0 && isDown)
            _comboUsed = true;
        return KeyDecision.Pass;
    }

    /// <summary>Myš / cokoli jiného při držené Win (Win+klik) – už to není osamocený stisk.</summary>
    public void OnOtherInput()
    {
        if (_winDown.Count > 0)
            _comboUsed = true;
    }
}
