using System.Windows.Threading;
using BetterWindows.Core;
using BetterWindows.Logic.Messaging;
using BetterWindows.Logic.Settings;
using BetterWindows.Native;
using BetterWindows.ShellWindows;

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
    }

    public void Start() => Safe.Fire("Vrstva plochy", StartAsync);

    private async Task StartAsync()
    {
        if (_starting || _window is not null)
            return;
        var settings = _context.Settings.Current.Desktop;
        if (!settings.Enabled)
        {
            SetStatus("off", "Vypnuto v nastavení");
            return;
        }

        _starting = true;
        try
        {
            var snapshot = settings.Mode == DesktopMode.Bottom ? null : await DesktopEmbedder.EnsureSplitAsync();
            if (snapshot is not null)
                Log.Info($"Plocha: {snapshot}");

            var window = new DesktopWindow(_context, _context.Monitors.GetPrimary(), settings.Mode, snapshot);
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

            SetStatus(window.ModeName, window.ModeDetail);
            ApplyIcons();
            _health.Start();
        }
        finally
        {
            _starting = false;
        }
    }

    public void Stop()
    {
        _health.Stop();
        CloseWindow();
        ShowIcons();
        SetStatus("off", "Zastaveno");
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
            SetStatus("failed", "Opakované selhání (víc než 3 restarty za minutu)");
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
        var timer = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Background, null, _context.Dispatcher);
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
        if (_closingOnPurpose || sender != _window)
            return;
        // Okno zaniklo samo – nejspíš spadl / restartoval se Průzkumník a vzal ho s sebou.
        Log.Warn("Vrstva plochy zanikla (pravděpodobně restart Průzkumníka). Čekám na obnovu plochy.");
        _window = null;
        _iconsHidden = false;
        SystemGuard.Record(s => s.DesktopIconsHidden = false);
        SetStatus("failed", "Okno zaniklo, čekám na Průzkumníka");
        var timer = new DispatcherTimer(TimeSpan.FromSeconds(3), DispatcherPriority.Background, null, _context.Dispatcher);
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (_window is null && !ExplorerWindows.FindProgman().IsNull)
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
