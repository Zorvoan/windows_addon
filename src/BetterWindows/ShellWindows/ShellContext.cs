using System.Windows.Threading;
using BetterWindows.Bridge;
using BetterWindows.Monitors;
using BetterWindows.Services;

namespace BetterWindows.ShellWindows;

/// <summary>Sdílené služby, které okna shellu potřebují (ať konstruktory nemají 6 parametrů).</summary>
internal sealed record ShellContext(
    Dispatcher Dispatcher,
    MessageHub Hub,
    MonitorService Monitors,
    ThemeService Theme,
    SettingsService Settings);
