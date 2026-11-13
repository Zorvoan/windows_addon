using BetterWindows.Core;
using BetterWindows.Logic.Settings;
using Forms = System.Windows.Forms;
using BetterWindows.Logic.Localization;

namespace BetterWindows.Tray;

/// <summary>
/// Ikona v oznamovací oblasti (vedle hodin) s nabídkou.
/// WPF vlastní NotifyIcon nemá, proto půjčujeme ten z WinForms.
/// Běží ve stejném (UI) vlákně jako WPF – zprávy mu doručuje WPF Dispatcher.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ContextMenuStrip _menu;
    private readonly System.Drawing.Icon _icon;
    private readonly Dictionary<BackdropMode, Forms.ToolStripMenuItem> _backdropItems = [];

    public event Action? SettingsRequested;
    public event Action? RestoreSystemRequested;
    public event Action<BackdropMode>? BackdropRequested;
    public event Action? ExitRequested;
#if DEBUG
    public event Action? UiErrorTestRequested;
    public event Action? CrashTestRequested;
#endif

    public TrayIcon()
    {
        _menu = new Forms.ContextMenuStrip();
        _menu.Items.Add(Loc.T("Nastavení"), null, (_, _) => SettingsRequested?.Invoke());

        // Rychlé přepnutí rozmazání lišty a docku (totéž je v Nastavení > Vzhled).
        var backdropMenu = new Forms.ToolStripMenuItem(Loc.T("Rozmazání lišty a docku"));
        foreach (var (mode, text) in new[]
                 {
                     (BackdropMode.SystemAcrylic, Loc.T("Systémové (Windows 11 Acrylic)")),
                     (BackdropMode.AccentAcrylic, Loc.T("Vlastní tón (API z Windows 10)")),
                     (BackdropMode.None, Loc.T("Žádné")),
                 })
        {
            var item = new Forms.ToolStripMenuItem(text, null, (_, _) => BackdropRequested?.Invoke(mode));
            _backdropItems[mode] = item;
            backdropMenu.DropDownItems.Add(item);
        }
        _menu.Items.Add(backdropMenu);

        // Nouzové tlačítko: ukáže ikony plochy a hlavní panel Windows, kdyby cokoli zlobilo.
        _menu.Items.Add(Loc.T("Obnovit ikony plochy a hlavní panel"), null, (_, _) => RestoreSystemRequested?.Invoke());
#if DEBUG
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add("Test: chyba v UI (aplikace běží dál)", null, (_, _) => UiErrorTestRequested?.Invoke());
        _menu.Items.Add("Test: pád procesu (obnoví watchdog)", null, (_, _) => CrashTestRequested?.Invoke());
#endif
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add(Loc.T("Ukončit"), null, (_, _) => ExitRequested?.Invoke());

        _icon = AppIcons.CreateTrayIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _icon,
            Text = AppInfo.DisplayName,
            ContextMenuStrip = _menu,
            Visible = true,
        };
        // Levé tlačítko otevře nastavení, pravé ukáže nabídku (to řeší NotifyIcon sám).
        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left)
                SettingsRequested?.Invoke();
        };
    }

    /// <summary>Zaškrtne aktuální způsob rozmazání.</summary>
    public void ShowBackdrop(BackdropMode current)
    {
        foreach (var (mode, item) in _backdropItems)
            item.Checked = mode == current;
    }

    public void Dispose()
    {
        // Bez Visible = false by ikona "visela" v oznamovací oblasti, dokud na ni nenajedeš myší.
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        _icon.Dispose();
    }
}
