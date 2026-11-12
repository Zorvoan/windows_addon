using System.Windows.Threading;
using BetterWindows.Core;
using BetterWindows.Logic.Settings;
using BetterWindows.Native;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BetterWindows.Services;

/// <summary>
/// Volitelné skrytí hlavního panelu Windows (Nastavení › Hlavní panel, výchozí VYPNUTO).
///
/// Jak:
///  1. do pojistky (SystemGuard) se zapíše původní stav panelu,
///  2. panel se přepne na "automaticky skrývat" – tím Windows uvolní místo dole pro okna,
///  3. okna panelu se skryjí (ShowWindow SW_HIDE). Když je Windows znovu ukáže (klávesa Win,
///     najetí myší k okraji), každých 500 ms je skryjeme znovu.
///
/// POZOR: "automaticky skrývat" si Windows UKLÁDÁ – je to jediná změna, kterou restart počítače
/// sám nevrátí. Vrací ji ukončení aplikace, pojistka při pádu, watchdog i další spuštění.
/// I kdyby vše selhalo, panel s automatickým skrýváním se dá vyvolat najetím myší k dolnímu okraji.
/// </summary>
internal sealed class TaskbarService : IDisposable
{
    private readonly SettingsService _settings;
    private readonly DispatcherTimer _guard;
    private bool _hidden;

    /// <summary>Pracovní plocha se změnila (dock se má posunout).</summary>
    public event Action? WorkAreaChanged;

    public TaskbarService(SettingsService settings, Dispatcher dispatcher)
    {
        _settings = settings;
        _guard = new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Background, (_, _) => Safe.Run("Hlídání hlavního panelu", KeepHidden), dispatcher);
        _guard.Stop();
        _settings.Changed += OnSettingsChanged;
    }

    public void Start() => Apply(_settings.Current.Taskbar.Hide);

    private void OnSettingsChanged(AppSettings old, AppSettings now)
    {
        if (old.Taskbar.Hide != now.Taskbar.Hide)
            Apply(now.Taskbar.Hide);
    }

    private void Apply(bool hide)
    {
        if (hide && !_hidden)
            Hide();
        else if (!hide && _hidden)
            Show();
    }

    private void Hide()
    {
        var taskbars = ExplorerWindows.FindTaskbars();
        if (taskbars.Count == 0)
        {
            Log.Warn("Hlavní panel nenalezen – neskrývám.");
            return;
        }

        var original = AppBarNative.GetTaskbarState();
        // 1. Pojistka PŘED změnou.
        SystemGuard.Record(s =>
        {
            s.TaskbarHidden = true;
            s.TaskbarOriginalState ??= original;
        });
        // 2. Automaticky skrývat -> Windows uvolní pracovní plochu.
        AppBarNative.SetTaskbarState(taskbars[0], original | (int)PInvoke.ABS_AUTOHIDE);
        // 3. Skrýt okna panelu.
        foreach (var t in taskbars)
            PInvoke.ShowWindowAsync(t, SHOW_WINDOW_CMD.SW_HIDE);

        _hidden = true;
        _guard.Start();
        Log.Info($"Hlavní panel skryt (původní stav {original}).");
        WorkAreaChanged?.Invoke();
    }

    private void Show()
    {
        _guard.Stop();
        var original = SystemGuard.Current.TaskbarOriginalState ?? 0;
        var taskbars = ExplorerWindows.FindTaskbars();
        foreach (var t in taskbars)
            PInvoke.ShowWindowAsync(t, SHOW_WINDOW_CMD.SW_SHOWNA);
        if (taskbars.Count > 0)
            AppBarNative.SetTaskbarState(taskbars[0], original);
        SystemGuard.Record(s =>
        {
            s.TaskbarHidden = false;
            s.TaskbarOriginalState = null;
        });
        _hidden = false;
        Log.Info($"Hlavní panel obnoven (stav {original}).");
        WorkAreaChanged?.Invoke();
    }

    private void KeepHidden()
    {
        foreach (var t in ExplorerWindows.FindTaskbars())
        {
            if (PInvoke.IsWindowVisible(t))
                PInvoke.ShowWindowAsync(t, SHOW_WINDOW_CMD.SW_HIDE);
        }
    }

    /// <summary>Po restartu Průzkumníka jsou okna panelu nová -> skrýt znovu.</summary>
    public void OnExplorerRestarted()
    {
        if (!_hidden)
            return;
        _hidden = false; // Hide() proběhne znovu, původní stav zůstává v pojistce
        Hide();
    }

    /// <summary>Volá ShellController.RestoreSystem (nouzové tlačítko).</summary>
    public void ForceShow()
    {
        if (_hidden)
            Show();
    }

    public void Dispose()
    {
        _settings.Changed -= OnSettingsChanged;
        if (_hidden)
            Safe.Run("Obnova hlavního panelu", Show);
    }
}
