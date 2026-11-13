using System.Windows.Threading;
using BetterWindows.Core;
using BetterWindows.Logic.Lock;
using BetterWindows.Logic.Messaging;
using BetterWindows.Native;
using Windows.Win32;

namespace BetterWindows.Services;

/// <summary>
/// Jazyk klávesnice aktivní aplikace (jako "ENG" v liště Familiar). Rozložení má každé vlákno
/// zvlášť, takže se ptáme okna v popředí; Windows o změně jinak neinformuje, kontrolujeme 2× za sekundu.
/// </summary>
internal sealed class KeyboardLayoutService : IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly uint _ownPid = (uint)Environment.ProcessId;
    private int _lastLang = -1;

    public InputLanguagePayload Current { get; private set; } = new("", "");
    public event Action<InputLanguagePayload>? Changed;

    public KeyboardLayoutService(Dispatcher dispatcher) =>
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Background, (_, _) => Safe.Run("Jazyk klávesnice", Check), dispatcher);

    public void Start()
    {
        Check();
        _timer.Start();
    }

    private unsafe void Check()
    {
        var fg = PInvoke.GetForegroundWindow();
        if (fg.IsNull || WindowInfo.GetProcessId(fg) == _ownPid)
            return; // v našich oknech nech poslední jazyk aplikace
        var thread = PInvoke.GetWindowThreadProcessId(fg, null);
        var hkl = PInvoke.GetKeyboardLayout(thread);
        var lang = (int)((nint)hkl.Value & 0xFFFF);
        if (lang == 0 || lang == _lastLang)
            return;
        _lastLang = lang;
        var (code, name) = KeyboardLanguage.Describe(lang);
        Current = new InputLanguagePayload(code, name);
        Changed?.Invoke(Current);
    }

    public void Dispose() => _timer.Stop();
}
