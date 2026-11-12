using BetterWindows.Core;
using Forms = System.Windows.Forms;

namespace BetterWindows.Tray;

/// <summary>
/// Ikona v oznamovací oblasti (vedle hodin) s nabídkou Nastavení / Ukončit.
/// WPF vlastní NotifyIcon nemá, proto půjčujeme ten z WinForms.
/// Běží ve stejném (UI) vlákně jako WPF – zprávy mu doručuje WPF Dispatcher.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ContextMenuStrip _menu;
    private readonly System.Drawing.Icon _icon;

    public event Action? SettingsRequested;
    public event Action? ExitRequested;
#if DEBUG
    public event Action? CrashTestRequested;
#endif

    public TrayIcon()
    {
        _menu = new Forms.ContextMenuStrip();
        _menu.Items.Add("Nastavení", null, (_, _) => SettingsRequested?.Invoke());
#if DEBUG
        _menu.Items.Add("Test: vyvolat pád", null, (_, _) => CrashTestRequested?.Invoke());
#endif
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add("Ukončit", null, (_, _) => ExitRequested?.Invoke());

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

    public void Dispose()
    {
        // Bez Visible = false by ikona "visela" v oznamovací oblasti, dokud na ni nenajedeš myší.
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        _icon.Dispose();
    }
}
