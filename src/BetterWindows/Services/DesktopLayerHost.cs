using System.Windows.Threading;
using BetterWindows.Core;
using BetterWindows.Logic.Messaging;
using BetterWindows.Logic.Settings;
using BetterWindows.Native;
using BetterWindows.ShellWindows;
using BetterWindows.Logic.Localization;

namespace BetterWindows.Services;

/// <summary>
/// Životní cyklus vrstvy plochy: vytvoření a vložení okna, skrytí ikon (jen když vložení
/// funguje), kontrola každých 10 s, obnova po restartu Průzkumníka, reakce na nastavení.
/// </summary>
internal sealed class DesktopLayerHost : IDisposable
{
    private readonly ShellContext _context;
    private readonly DispatcherTimer _health;
    private readonly Queue<DateTime> _restarts = new();
    private DesktopWindow? _window;
    private bool _closingOnPurpose;
    private bool _iconsHidden;
    private bool _starting;

    public string Mode { get; private set; } = "off";
    public string? Detail { get; private set; }

    /// <summary>Změnil se režim / stav (pro diagnostiku v nastavení).</summary>
    public event Action? StatusChanged;

    public DesktopLayerHost(ShellContext context)
    {
        _context = context;
        _health = new DispatcherTimer(TimeSpan.FromSeconds(10), DispatcherPriority.Background, (_, _) => Safe.Run("Kontrola plochy", CheckHealth), context.Dispatcher);
        _context.Settings.Changed += OnSettingsChanged;
        _context.Hub.On<DesktopTextInputPayload>(MessageTypes.DesktopTextInput, (bridge, p) =>
        {
            if (p is not null && bridge.Page == "desktop")
                Safe.Run("Plocha – psaní do widgetu", () => ShowTextInput(bridge, p));
        });
    }

    private DesktopInputWindow? _input;

    /// <summary>
    /// Psaní do widgetu: přes jeho textové pole ukázat skutečné vstupní okno (vrstva plochy klávesnici
    /// nedostane – fokus má plocha Windows). Výsledek se pošle zpět stránce plochy.
    /// </summary>
    private void ShowTextInput(Bridge.MessageBridge bridge, DesktopTextInputPayload p)
    {
        if (_window is null)
            return;
        _input?.Close();
        var rect = _window.ToScreen(p.Rect);
        var input = new DesktopInputWindow(rect, _window.Monitor.Scale, p.Value, p.Placeholder, p.FontSize, _context.Theme.IsDark);
        input.Finished += (text, submit) =>
        {
            if (_input == input)
                _input = null;
            _context.Hub.Send(bridge, MessageTypes.DesktopTextResult, new DesktopTextResultPayload(p.Key, text, submit));
        };
        _input = input;
        input.Show();
        // Klik šel do našeho okna, takže Windows nám popředí dá; pojistka přes ForegroundHelper.
        ForegroundHelper.BringToForeground(new Windows.Win32.Foundation.HWND(new System.Windows.Interop.WindowInteropHelper(input).Handle));
    }

    public void Start() => Safe.Fire("Vrstva plochy", StartAsync);

    private async Task StartAsync()
    {
        if (_starting || _window is not null)
            return;
        var settings = _context.Settings.Current.Desktop;
        if (!settings.Enabled)
        {
            SetStatus("off", Loc.T("Vypnuto v nastavení"));
            return;
        }

        _starting = true;
        try
        {
            // Automatický režim po zjištění, že vložená plocha nepropouští myš: rovnou „Okno vespod“.
            var mode = settings.Mode == DesktopMode.Auto && _embedBlocksInput ? DesktopMode.Bottom : settings.Mode;
            var snapshot = mode == DesktopMode.Bottom ? null : await DesktopEmbedder.EnsureSplitAsync();
            if (snapshot is not null)
                Log.Info($"Plocha: {snapshot}");

            var window = new DesktopWindow(_context, _context.Monitors.GetPrimary(), mode, snapshot);
            window.SetBackdrop(BackdropMode.None);
            window.Closed += OnWindowClosed;
            _closingOnPurpose = false;
            _window = window;
            window.Show();

            if (settings.Mode == DesktopMode.Embed && window.Embedded?.Success != true)
            {
                Log.Warn("Režim 'jen vložení' selhal – vrstva plochy se nezobrazí.");
                CloseWindow();
                SetStatus("failed", window.ModeDetail);
                return;
            }

            SetStatus(window.ModeName, _embedBlocksInput && window.ModeName == "bottom"
                ? Loc.T("Okno vespod – vložená plocha nepropouštěla kliknutí k widgetům")
                : window.ModeDetail);
            ApplyIcons();
            _health.Start();
            LogHitTestLater(window);
        }
        finally
        {
            _starting = false;
        }
    }

    /// <summary>
    /// Vložená plocha na 25H2 nemusí propouštět kliknutí k našemu oknu (kliknutí převezme plocha Windows).
    /// Zjistíme to přes WindowFromPoint a v automatickém režimu přepneme na „Okno vespod“, kde myš funguje.
    /// Platí do změny režimu v nastavení nebo do restartu aplikace.
    /// </summary>
    private bool _embedBlocksInput;

    /// <summary>Za chvíli (okno je rozložené, ikony skryté) ověřit, kdo na ploše dostává kliknutí.</summary>
    private void LogHitTestLater(DesktopWindow window)
    {
        var timer = new DispatcherTimer(DispatcherPriority.Background, _context.Dispatcher) { Interval = TimeSpan.FromSeconds(2) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (_window != window)
                return;
            Safe.Run("Plocha – diagnostika kliknutí", () =>
            {
                var detail = DesktopEmbedder.HitTest(window.Handle, window.Monitor.Bounds, out var ours);
                Log.Info($"Plocha – kdo dostane klik: {detail}");
                if (!ours && window.Embedded?.Success == true && _context.Settings.Current.Desktop.Mode == DesktopMode.Auto)
                {
                    Log.Warn("Plocha: vložená vrstva nedostává kliknutí – přepínám na „Okno vespod“ (widgety pak půjdou ovládat myší).");
                    _embedBlocksInput = true;
                    Restart("vložená plocha nepropouští kliknutí");
                }
            });
        };
        timer.Start();
    }

    public void Stop()
    {
        _health.Stop();
        CloseWindow();
        ShowIcons();
        SetStatus("off", Loc.T("Zastaveno"));
    }

    public void Restart(string reason)
    {
        Log.Info($"Plocha: restart ({reason})");
        _restarts.Enqueue(DateTime.UtcNow);
        while (_restarts.Count > 0 && DateTime.UtcNow - _restarts.Peek() > TimeSpan.FromMinutes(1))
            _restarts.Dequeue();
        if (_restarts.Count > 3)
        {
            Log.Error("Plocha: příliš mnoho restartů během minuty – vrstvu plochy vypínám do dalšího spuštění.");
            Stop();
            SetStatus("failed", Loc.T("Opakované selhání (víc než 3 restarty za minutu)"));
            return;
        }
        _health.Stop();
        CloseWindow();
        _context.Dispatcher.InvokeAsync(Start, DispatcherPriority.Background);
    }

    /// <summary>Průzkumník byl restartován – plocha je nová, ikony zase vidět.</summary>
    public void OnExplorerRestarted()
    {
        _iconsHidden = false;
        SystemGuard.Record(s => s.DesktopIconsHidden = false);
        // Dáme Průzkumníku chvilku, ať plochu postaví.
        var timer = new DispatcherTimer(DispatcherPriority.Background, _context.Dispatcher) { Interval = TimeSpan.FromSeconds(2) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Restart("restart Průzkumníka");
        };
        timer.Start();
    }

    public void OnMonitorsChanged() => _window?.RefreshMonitor();

    private void CheckHealth()
    {
        if (_window is null || _starting)
            return;
        if (!_window.CheckHealthy())
            Restart("vrstva plochy už není na svém místě");
    }

    private void OnSettingsChanged(AppSettings old, AppSettings now)
    {
        var o = old.Desktop;
        var n = now.Desktop;
        if (o.Enabled != n.Enabled || o.Mode != n.Mode)
        {
            if (o.Mode != n.Mode)
                _embedBlocksInput = false; // uživatel zvolil režim sám -> zkusit znovu
            if (!n.Enabled)
                Stop();
            else
                Restart("změna nastavení");
        }
        else if (o.HideIcons != n.HideIcons)
        {
            ApplyIcons();
        }
    }

    private void ApplyIcons()
    {
        // Ikony skryjeme JEN když je vrstva opravdu vložená do plochy – jinak by uživatel přišel o plochu.
        var want = _context.Settings.Current.Desktop.HideIcons && _window?.Embedded?.Success == true && _window.CheckHealthy();
        if (want && !_iconsHidden)
        {
            SystemGuard.Record(s => s.DesktopIconsHidden = true); // nejdřív pojistka
            if (ExplorerWindows.SetDesktopIconsVisible(false))
                _iconsHidden = true;
            else
                SystemGuard.Record(s => s.DesktopIconsHidden = false);
        }
        else if (!want && _iconsHidden)
        {
            ShowIcons();
        }
        PublishStatus();
    }

    private void ShowIcons()
    {
        if (!_iconsHidden)
            return;
        ExplorerWindows.SetDesktopIconsVisible(true);
        _iconsHidden = false;
        SystemGuard.Record(s => s.DesktopIconsHidden = false);
        PublishStatus();
    }

    private void CloseWindow()
    {
        if (_window is null)
            return;
        _closingOnPurpose = true;
        var w = _window;
        _window = null;
        Safe.Run("Zavření plochy", w.Close);
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (_closingOnPurpose || AppInfo.Exiting || sender != _window)
            return;
        // Okno zaniklo samo – nejspíš spadl / restartoval se Průzkumník a vzal ho s sebou.
        Log.Warn("Vrstva plochy zanikla (pravděpodobně restart Průzkumníka). Čekám na obnovu plochy.");
        _window = null;
        _iconsHidden = false;
        SystemGuard.Record(s => s.DesktopIconsHidden = false);
        SetStatus("failed", Loc.T("Okno zaniklo, čekám na Průzkumníka"));
        // Pozn.: konstruktor DispatcherTimer s obsluhou ji vyžaduje (null = výjimka), proto bez něj + Tick.
        var timer = new DispatcherTimer(DispatcherPriority.Background, _context.Dispatcher) { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (_window is null && !AppInfo.Exiting && !ExplorerWindows.FindProgman().IsNull)
                Restart("okno zaniklo");
        };
        timer.Start();
    }

    private void SetStatus(string mode, string? detail)
    {
        Mode = mode;
        Detail = detail;
        PublishStatus();
        StatusChanged?.Invoke();
    }

    private void PublishStatus() =>
        _context.Hub.Publish(MessageTypes.DesktopStatus, new DesktopStatusPayload(Mode, _iconsHidden, Detail));

    public void Dispose()
    {
        _context.Settings.Changed -= OnSettingsChanged;
        Stop();
    }
}
