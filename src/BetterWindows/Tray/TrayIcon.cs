using BetterWindows.Core;
using BetterWindows.Native;
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
    public event Action<BackdropKind>? BackdropRequested;
    private readonly Dictionary<BackdropKind, Forms.ToolStripMenuItem> _backdropItems = [];
#endif

    public TrayIcon()
    {
        _menu = new Forms.ContextMenuStrip();
        _menu.Items.Add("Nastavení", null, (_, _) => SettingsRequested?.Invoke());
#if DEBUG
        // Testovací přepínač: porovnání způsobů rozmazání horní lišty (krok 1).
        var backdropMenu = new Forms.ToolStripMenuItem("Test: rozmazání lišty");
        foreach (var (kind, text) in new[]
                 {
                     (BackdropKind.SystemAcrylic, "Systémové (Windows 11 Acrylic)"),
                     (BackdropKind.AccentAcrylic, "Vlastní tón (API z Windows 10)"),
                     (BackdropKind.None, "Žádné"),
                 })
        {
            var item = new Forms.ToolStripMenuItem(text, null, (_, _) => BackdropRequested?.Invoke(kind));
            _backdropItems[kind] = item;
            backdropMenu.DropDownItems.Add(item);
        }
        _menu.Items.Add(backdropMenu);
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

    /// <summary>Zaškrtne aktuální způsob rozmazání v testovací nabídce.</summary>
    public void ShowBackdrop(BackdropKind current)
    {
#if DEBUG
        foreach (var (kind, item) in _backdropItems)
            item.Checked = kind == current;
#endif
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
