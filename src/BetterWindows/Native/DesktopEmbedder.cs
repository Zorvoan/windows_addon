using System.Runtime.InteropServices;
using BetterWindows.Core;
using BetterWindows.Monitors;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BetterWindows.Native;

/// <summary>Rozložení oken plochy Windows.</summary>
internal enum DesktopLayout
{
    /// <summary>Windows 11 24H2+: SHELLDLL_DefView i WorkerW jsou potomky Progman.</summary>
    Raised,
    /// <summary>Do 23H2: SHELLDLL_DefView v samostatném WorkerW, tapeta v dalším WorkerW za ním.</summary>
    Classic,
    /// <summary>Plocha ještě není "rozdělená" (WorkerW pro tapetu neexistuje).</summary>
    Unsplit,
    NoProgman,
}

internal sealed record DesktopSnapshot(HWND Progman, HWND DefView, HWND WorkerW, DesktopLayout Layout, bool RaisedStyle)
{
    public override string ToString() =>
        $"{Layout} (Progman 0x{(nint)Progman:X}, DefView 0x{(nint)DefView:X}, WorkerW 0x{(nint)WorkerW:X}, NOREDIRECTIONBITMAP={RaisedStyle})";
}

internal sealed record EmbedResult(bool Success, string Mode, HWND Parent, HWND InsertAfter, string Detail);

/// <summary>
/// Vložení našeho okna do plochy Windows – POD ikony, NAD tapetu Windows, pod všechny aplikace.
///
/// Postup (stejný princip jako Lively Wallpaper a další tapetové aplikace):
///  1. Progman pošleme nedokumentovanou zprávu 0x052C (wParam 0xD, lParam 1). Průzkumník pak
///     vytvoří okno WorkerW, do kterého kreslí tapetu ("rozdělí" plochu).
///  2. Najdeme to WorkerW (podle rozložení – viz DesktopLayout).
///  3. Naše okno uděláme podoknem (WS_CHILD) toho WorkerW a roztáhneme přes monitor.
///     Na 24H2+ musí mít okno navíc WS_EX_LAYERED (s plnou neprůhledností), jinak ho
///     nový způsob skládání plochy vůbec nevykreslí.
///
/// Nic z toho není oficiálně dokumentované API. Proto výsledek ověřujeme a máme zálohu (HWND_BOTTOM).
/// </summary>
internal static class DesktopEmbedder
{
    private const uint WmSpawnWorkerW = 0x052C;

    public static DesktopSnapshot Snapshot()
    {
        var progman = ExplorerWindows.FindProgman();
        if (progman.IsNull)
            return new DesktopSnapshot(HWND.Null, HWND.Null, HWND.Null, DesktopLayout.NoProgman, false);

        var raisedStyle = (WindowStyles.GetExStyle(progman) & WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP) != 0;
        var defInProgman = PInvoke.FindWindowEx(progman, HWND.Null, "SHELLDLL_DefView", null);
        var workerInProgman = LargestChild(progman, "WorkerW");

        if (raisedStyle || (!defInProgman.IsNull && !workerInProgman.IsNull))
            return new DesktopSnapshot(progman, defInProgman, workerInProgman, DesktopLayout.Raised, raisedStyle);

        // Klasicky: top-level okno, které obsahuje SHELLDLL_DefView, a hned za ním WorkerW s tapetou.
        HWND classicDefView = HWND.Null, classicWorker = HWND.Null;
        PInvoke.EnumWindows((top, _) =>
        {
            var defView = PInvoke.FindWindowEx(top, HWND.Null, "SHELLDLL_DefView", null);
            if (defView.IsNull)
                return true;
            classicDefView = defView;
            classicWorker = PInvoke.FindWindowEx(HWND.Null, top, "WorkerW", null);
            return false;
        }, 0);

        if (!classicWorker.IsNull)
            return new DesktopSnapshot(progman, classicDefView, classicWorker, DesktopLayout.Classic, false);

        return new DesktopSnapshot(progman, defInProgman.IsNull ? classicDefView : defInProgman, HWND.Null, DesktopLayout.Unsplit, false);
    }

    /// <summary>Zajistí, že existuje WorkerW pro tapetu (pošle 0x052C a počká, až ho Průzkumník vytvoří).</summary>
    public static async Task<DesktopSnapshot> EnsureSplitAsync()
    {
        var snap = Snapshot();
        if (IsReady(snap) || snap.Layout == DesktopLayout.NoProgman)
            return snap;

        Log.Info($"Plocha: rozdělení přes 0x052C (stav před: {snap})");
        foreach (var (wParam, lParam, attempts) in new[] { ((nuint)0xD, (nint)1, 20), ((nuint)0, (nint)0, 10) })
        {
            SendSpawn(snap.Progman, wParam, lParam);
            for (var i = 0; i < attempts; i++)
            {
                await Task.Delay(100);
                snap = Snapshot();
                if (IsReady(snap))
                    return snap;
            }
        }
        Log.Warn(snap.Layout == DesktopLayout.Raised
            ? $"Plocha: WorkerW se neobjevil, použije se záloha pod SHELLDLL_DefView ({snap})"
            : $"Plocha: rozdělení se nepovedlo ({snap})");
        return snap;
    }

    /// <summary>
    /// Připraveno = existuje WorkerW pro tapetu. (Na 24H2+ existuje SHELLDLL_DefView vždy, proto ho
    /// nestačí; když se WorkerW ani po 0x052C neobjeví, Embed použije zálohu pod SHELLDLL_DefView.)
    /// </summary>
    private static bool IsReady(DesktopSnapshot s) =>
        s.Layout == DesktopLayout.Classic || (s.Layout == DesktopLayout.Raised && !s.WorkerW.IsNull);

    private static unsafe void SendSpawn(HWND progman, nuint wParam, nint lParam)
    {
        nuint result;
        // SendMessageTimeout: kdyby Průzkumník neodpovídal, do 1 s to vzdáme (neblokujeme UI).
        PInvoke.SendMessageTimeout(progman, WmSpawnWorkerW, new WPARAM(wParam), new LPARAM(lParam),
            SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_NORMAL, 1000, &result);
    }

    /// <summary>Vloží okno do plochy. Při neúspěchu vrátí styly do původního stavu.</summary>
    public static EmbedResult Embed(HWND window, DesktopSnapshot snap, PixelRect monitor)
    {
        if (snap.Layout is DesktopLayout.NoProgman or DesktopLayout.Unsplit)
            return new EmbedResult(false, "failed", HWND.Null, HWND.Null, $"Nevhodné rozložení plochy: {snap}");

        var originalStyle = WindowStyles.GetStyle(window);
        var originalEx = WindowStyles.GetExStyle(window);

        HWND parent, insertAfter;
        string mode, detail;
        var layered = snap.Layout == DesktopLayout.Raised;
        if (snap.Layout == DesktopLayout.Raised && !snap.WorkerW.IsNull)
        {
            parent = snap.WorkerW;
            insertAfter = HWND.Null; // HWND_TOP (= 0): navrch mezi sourozenci
            mode = "embed-raised";
            detail = "24H2+: podokno WorkerW uvnitř Progman (WS_EX_LAYERED)";
        }
        else if (snap.Layout == DesktopLayout.Raised)
        {
            parent = snap.Progman;
            insertAfter = snap.DefView; // hned pod vrstvu s ikonami
            mode = "embed-raised";
            detail = "24H2+: podokno Progman pod SHELLDLL_DefView (WS_EX_LAYERED)";
        }
        else
        {
            parent = snap.WorkerW;
            insertAfter = HWND.Null; // HWND_TOP (= 0): navrch mezi sourozenci
            mode = "embed-classic";
            detail = "klasicky: podokno WorkerW za ikonami";
        }

        // Z vyskakovacího okna uděláme podokno.
        var style = (originalStyle & ~(WINDOW_STYLE.WS_POPUP | WINDOW_STYLE.WS_CAPTION | WINDOW_STYLE.WS_SYSMENU | WINDOW_STYLE.WS_THICKFRAME))
                    | WINDOW_STYLE.WS_CHILD;
        var ex = (originalEx | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW) & ~WINDOW_EX_STYLE.WS_EX_NOACTIVATE;
        if (layered)
            ex |= WINDOW_EX_STYLE.WS_EX_LAYERED;
        WindowStyles.SetStyle(window, style);
        WindowStyles.SetExStyle(window, ex);
        if (layered)
            PInvoke.SetLayeredWindowAttributes(window, new COLORREF(0), 255, LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_ALPHA);
        PInvoke.SetWindowPos(window, HWND.Null, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER |
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED);

        var previousParent = PInvoke.SetParent(window, parent);
        var error = Marshal.GetLastWin32Error();
        if (PInvoke.GetParent(window) != parent)
        {
            // Vrátit vše zpět, ať záloha (HWND_BOTTOM) pracuje s normálním oknem.
            PInvoke.SetParent(window, HWND.Null);
            WindowStyles.SetStyle(window, originalStyle);
            WindowStyles.SetExStyle(window, originalEx);
            return new EmbedResult(false, "failed", HWND.Null, HWND.Null, $"SetParent selhalo (chyba {error}, předchozí rodič 0x{(nint)previousParent:X}); {snap}");
        }

        PlaceInParent(window, parent, insertAfter, monitor);
        if (parent == snap.Progman && !snap.WorkerW.IsNull)
        {
            // Systémové WorkerW s tapetou musí být pod námi.
            PInvoke.SetWindowPos(snap.WorkerW, HWND.HWND_BOTTOM, 0, 0, 0, 0,
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE |
                SET_WINDOW_POS_FLAGS.SWP_ASYNCWINDOWPOS);
        }
        return new EmbedResult(true, mode, parent, insertAfter, detail);
    }

    /// <summary>Roztáhne vložené okno přes monitor. Souřadnice podokna jsou relativní k rodiči.</summary>
    public static void PlaceInParent(HWND window, HWND parent, HWND insertAfter, PixelRect monitor)
    {
        PInvoke.GetWindowRect(parent, out var parentRect);
        var flags = SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW;
        PInvoke.SetWindowPos(window, insertAfter, monitor.Left - parentRect.left, monitor.Top - parentRect.top,
            monitor.Width, monitor.Height, flags);
    }

    /// <summary>Je okno pořád vložené a viditelné?</summary>
    public static bool Verify(HWND window, EmbedResult result) =>
        result.Success && PInvoke.IsWindow(result.Parent) && PInvoke.GetParent(window) == result.Parent && PInvoke.IsWindowVisible(window);

    private static HWND LargestChild(HWND parent, string className)
    {
        HWND best = HWND.Null, after = HWND.Null;
        long bestArea = 0;
        while (true)
        {
            after = PInvoke.FindWindowEx(parent, after, className, null);
            if (after.IsNull)
                break;
            PInvoke.GetWindowRect(after, out var r);
            long area = (long)(r.right - r.left) * (r.bottom - r.top);
            if (area > bestArea)
            {
                bestArea = area;
                best = after;
            }
        }
        return best;
    }

    /// <summary>
    /// Diagnostika vstupu: které okno dostane klik na daném místě vrstvy plochy (WindowFromPoint),
    /// a jestli je rodič vrstvy povolený. Když to není naše okno (nebo WebView2 uvnitř), klik se
    /// k widgetům nedostane – to je potřeba vědět, než se zvolí jiné řešení.
    /// </summary>
    public static string DescribeHitTest(HWND ours, PixelRect monitor) => HitTest(ours, monitor, out _);

    /// <summary>Jako DescribeHitTest; <paramref name="widgetsOurs"/> = klik do oblasti widgetů dojde k nám.</summary>
    public static string HitTest(HWND ours, PixelRect monitor, out bool widgetsOurs)
    {
        widgetsOurs = false;
        var parts = new List<string>();
        var parent = PInvoke.GetParent(ours);
        if (!parent.IsNull)
        {
            var ex = WindowStyles.GetExStyle(parent);
            parts.Add($"rodič {ExplorerWindows.GetClassName(parent)} povolený={(bool)PInvoke.IsWindowEnabled(parent)}, " +
                      $"WS_EX_TRANSPARENT={(ex & WINDOW_EX_STYLE.WS_EX_TRANSPARENT) != 0}, WS_EX_LAYERED={(ex & WINDOW_EX_STYLE.WS_EX_LAYERED) != 0}");
        }
        foreach (var (name, x, y) in new[] { ("widgety", monitor.Left + 150, monitor.Top + 150), ("střed", monitor.Left + monitor.Width / 2, monitor.Top + monitor.Height / 2) })
        {
            var hit = PInvoke.WindowFromPoint(new System.Drawing.Point(x, y));
            var mine = hit == ours || PInvoke.IsChild(ours, hit);
            if (name == "widgety")
                widgetsOurs = mine;
            parts.Add($"{name} [{x},{y}] -> {ExplorerWindows.GetClassName(hit)} 0x{(nint)hit:X}{(mine ? " (naše)" : " (CIZÍ – klik k widgetům nedojde)")}");
        }
        return string.Join("; ", parts);
    }
}
