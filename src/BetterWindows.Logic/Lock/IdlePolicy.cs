namespace BetterWindows.Logic.Lock;

/// <summary>Má se po nečinnosti ukázat zamykací obrazovka?</summary>
public static class IdlePolicy
{
    /// <param name="idle">Jak dlouho uživatel nesáhl na myš/klávesnici.</param>
    /// <param name="mediaPlaying">Hraje hudba/video (přes ovládání médií Windows).</param>
    /// <param name="fullscreen">Aplikace přes celou obrazovku (hra, video).</param>
    /// <param name="displayRequired">Nějaká aplikace žádá, ať obrazovka nezhasne (přehrávač, prezentace).</param>
    public static bool ShouldShow(int idleMinutes, TimeSpan idle, bool alreadyOpen, bool mediaPlaying, bool fullscreen, bool displayRequired) =>
        idleMinutes > 0 && !alreadyOpen && !mediaPlaying && !fullscreen && !displayRequired &&
        idle >= TimeSpan.FromMinutes(idleMinutes);
}
