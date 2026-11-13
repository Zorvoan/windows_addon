using System.Runtime.InteropServices;
using System.Threading;
using BetterWindows.Core;
using BetterWindows.Logic.Input;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BetterWindows.Native;

/// <summary>
/// Klávesa Windows otevírá Start Better Windows (jen když je to zapnuté v nastavení).
///
/// Low-level háček klávesnice (WH_KEYBOARD_LL) běží ve VLASTNÍM vlákně se smyčkou zpráv:
/// Windows háček vyřadí, když neodpoví do ~300 ms, takže nesmí čekat na zaneprázdněné UI.
/// Rozhoduje otestovaný stavový automat WinKeyTap:
///  - stisk Win nikdy nezahodíme (zkratky Win+E, Win+L... fungují),
///  - po OSAMOCENÉM stisku zahodíme jen uvolnění a hned (po návratu z háčku) pošleme vlastní:
///    maskovací klávesu 0xE8 (nepřiřazená) a uvolnění Win. Windows pak vidí "zkratku",
///    takže neotevře svůj Start, a klávesa Win nezůstane "držená".
///  - když vložení selže, zkusíme to ještě jednou a zalogujeme.
/// Háček nevidí klávesy, když je v popředí aplikace spuštěná jako správce (ochrana Windows) –
/// tehdy se otevře normální Start Windows.
/// </summary>
internal sealed class WinKeyHook : IDisposable
{
    private const uint WmReplay = PInvoke.WM_APP + 1;
    private const ushort MaskKey = 0xE8;
    private static readonly nuint OurMarker = 0x42575721; // "BWW!" – poznávací značka našich vložených kláves

    private readonly HOOKPROC _proc; // odkaz držíme, aby ho GC neuklidil (jinak pád při další klávese)
    private readonly WinKeyTap _tap = new();
    private Thread? _thread;
    private uint _threadId;
    private volatile bool _enabled;

    /// <summary>Osamocený stisk Win. Volá se z vlákna háčku – obsluha musí jen předat práci dál!</summary>
    public event Action? Tapped;

    public WinKeyHook() => _proc = Callback;

    /// <summary>Smí háček klávesu převzít? (Start připravený, není hra přes celou obrazovku.)</summary>
    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public void Start()
    {
        if (_thread is not null)
            return;
        using var started = new ManualResetEventSlim();
        _thread = new Thread(() => Run(started)) { IsBackground = true, Name = "WinKeyHook" };
        _thread.Start();
        started.Wait(TimeSpan.FromSeconds(2));
    }

    private void Run(ManualResetEventSlim started)
    {
        _threadId = PInvoke.GetCurrentThreadId();
        // Handle vlastního modulu – NEuvolňovat (GetModuleHandle nezvyšuje počítadlo odkazů).
        var module = PInvoke.GetModuleHandle((string?)null);
        var hook = PInvoke.SetWindowsHookEx(WINDOWS_HOOK_ID.WH_KEYBOARD_LL, _proc, module, 0);
        started.Set();
        if (hook.IsInvalid)
        {
            Log.Error($"Háček klávesnice se nepodařilo nainstalovat (chyba {Marshal.GetLastWin32Error()}).");
            return;
        }
        Log.Info("Háček klávesy Windows nainstalován.");

        try
        {
            while (PInvoke.GetMessage(out var msg, HWND.Null, 0, 0) > 0)
            {
                if (msg.message == WmReplay)
                    Replay((ushort)msg.wParam.Value);
            }
        }
        finally
        {
            hook.Dispose(); // UnhookWindowsHookEx
            Log.Info("Háček klávesy Windows odinstalován.");
        }
    }

    private unsafe LRESULT Callback(int nCode, WPARAM wParam, LPARAM lParam)
    {
        try
        {
            if (nCode >= 0)
            {
                var kb = (KBDLLHOOKSTRUCT*)lParam.Value;
                var down = wParam.Value is PInvoke.WM_KEYDOWN or PInvoke.WM_SYSKEYDOWN;
                var injected = (kb->flags & KBDLLHOOKSTRUCT_FLAGS.LLKHF_INJECTED) != 0;
                if (_tap.OnKey((int)kb->vkCode, down, injected, _enabled) == KeyDecision.SwallowAndToggleStart)
                {
                    // Uvolnění Win zahodíme a náhradu pošleme hned po návratu z háčku (ve smyčce zpráv).
                    PInvoke.PostThreadMessage(_threadId, WmReplay, new WPARAM(kb->vkCode), default);
                    Tapped?.Invoke();
                    return new LRESULT(1);
                }
            }
        }
        catch
        {
            // Háček nesmí nikdy vyhodit výjimku – klávesu pustíme dál.
        }
        return PInvoke.CallNextHookEx(null, nCode, wParam, lParam);
    }

    /// <summary>Maskovací klávesa + uvolnění Win (náhrada za zahozené uvolnění).</summary>
    private static void Replay(ushort winVk)
    {
        Span<INPUT> inputs =
        [
            Key(MaskKey, up: false),
            Key(MaskKey, up: true),
            Key(winVk, up: true),
        ];
        for (var attempt = 0; attempt < 2; attempt++)
        {
            if (PInvoke.SendInput(inputs, Marshal.SizeOf<INPUT>()) == inputs.Length)
                return;
        }
        Log.Error($"Vložení uvolnění klávesy Win selhalo (chyba {Marshal.GetLastWin32Error()}). Kdyby se klávesy chovaly divně, stiskni Win ještě jednou.");
    }

    /// <summary>Ctrl+Esc = otevřít normální Start Windows (záloha, když náš Start nedostane fokus).</summary>
    public static void OpenWindowsStart()
    {
        Span<INPUT> inputs =
        [
            Key(0x11, up: false), // Ctrl
            Key(0x1B, up: false), // Esc
            Key(0x1B, up: true),
            Key(0x11, up: true),
        ];
        PInvoke.SendInput(inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT Key(ushort vk, bool up) => new()
    {
        type = INPUT_TYPE.INPUT_KEYBOARD,
        Anonymous = new INPUT._Anonymous_e__Union
        {
            ki = new KEYBDINPUT
            {
                wVk = (VIRTUAL_KEY)vk,
                dwFlags = up ? KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP : 0,
                dwExtraInfo = OurMarker,
            },
        },
    };

    public void Dispose()
    {
        if (_threadId != 0)
            PInvoke.PostThreadMessage(_threadId, PInvoke.WM_QUIT, default, default);
        _thread?.Join(TimeSpan.FromSeconds(2));
        _thread = null;
        _threadId = 0;
    }
}
