using System.Windows;
using System.Windows.Controls;
using BetterWindows.Bridge;
using BetterWindows.Core;
using BetterWindows.Web;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using BetterWindows.Logic.Localization;

namespace BetterWindows.ShellWindows;

/// <summary>
/// Okno nastavení – běžné okno s rámečkem, obsah je stránka ui/settings.html.
/// (Oproti liště a docku se ukazuje v Alt+Tab a na hlavním panelu, je to "normální" aplikace.)
/// </summary>
internal sealed class SettingsWindow : Window
{
    private readonly WebView2 _web = new();
    private readonly MessageHub _hub;
    private MessageBridge? _bridge;

    public SettingsWindow(MessageHub hub)
    {
        _hub = hub;
        Title = Loc.T("Better Windows – Nastavení");
        Icon = AppIcons.WindowIcon;
        Width = 1040;
        Height = 760;
        MinWidth = 560;
        MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Content = _web;

        Loaded += OnLoaded;
        Closed += (_, _) =>
        {
            if (_bridge is not null)
                _hub.Unregister(_bridge);
            _web.Dispose();
        };
    }

    // "async void" je v pořádku jen u obsluhy událostí; chyby proto chytáme uvnitř.
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var core = await WebViewHost.InitializeAsync(_web, "settings");
            _bridge = new MessageBridge("settings", core);
            _hub.Register(_bridge);
        }
        catch (WebView2RuntimeNotFoundException ex)
        {
            Log.Error("Chybí WebView2 Runtime", ex);
            ShowError(Loc.T("Na počítači chybí Microsoft Edge WebView2 Runtime.\nStáhni ho z {url}", ("url", "https://developer.microsoft.com/microsoft-edge/webview2/")));
        }
        catch (Exception ex)
        {
            Log.Error("Načtení nastavení selhalo", ex);
            ShowError(ex.Message);
        }
    }

    private void ShowError(string message) =>
        Content = new TextBlock
        {
            Text = message,
            Margin = new Thickness(24),
            TextWrapping = TextWrapping.Wrap,
        };
}
