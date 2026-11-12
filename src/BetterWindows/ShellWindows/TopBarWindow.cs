using BetterWindows.Monitors;

namespace BetterWindows.ShellWindows;

/// <summary>Horní lišta přes celou šířku monitoru. Obsah: ui/topbar.html.</summary>
internal sealed class TopBarWindow : ShellWindowBase
{
    /// <summary>Výška lišty ve WPF jednotkách (při měřítku 100 % = pixely).</summary>
    public const double HeightDip = 36;

    public TopBarWindow(MonitorService monitors, MonitorInfo monitor, bool dark)
        : base("topbar", monitors, monitor, dark)
    {
    }

    protected override PixelRect ComputeBounds(MonitorInfo m) =>
        new(m.Bounds.Left, m.Bounds.Top, m.Bounds.Right, m.Bounds.Top + m.ToPixels(HeightDip));
}
