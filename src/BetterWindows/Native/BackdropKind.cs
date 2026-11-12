namespace BetterWindows.Native;

/// <summary>Jak se kreslí rozmazané pozadí horní lišty a docku.</summary>
public enum BackdropKind
{
    /// <summary>Oficiální Windows 11 API (DWMWA_SYSTEMBACKDROP_TYPE = Acrylic). Tón určuje systém.</summary>
    SystemAcrylic,

    /// <summary>Nedokumentované API z Windows 10. Vlastní barva a průhlednost tónu.</summary>
    AccentAcrylic,

    /// <summary>Bez rozmazání (diagnostika: ukáže, co je pod průhlednými pixely).</summary>
    None,
}
