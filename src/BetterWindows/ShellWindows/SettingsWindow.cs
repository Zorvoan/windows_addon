using System.Windows;
using System.Windows.Controls;
using BetterWindows.Core;
using BetterWindows.Web;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace BetterWindows.ShellWindows;

/// <summary>
/// Okno nastavení – běžné okno s rámečkem, obsah je stránka ui/settings.html.
/// (Oproti liště a docku se ukazuje v Alt+Tab a na hlavním panelu, je to "normální" aplikace.)
/// </summary>
public sealed class SettingsWindow : Window
{
    private readonly WebView2 _web = new();

    public SettingsWindow()
    {
        Title = $"{AppInfo.DisplayName} – Nastavení";
        Icon = AppIcons.WindowIcon;
        Width = 760;
        Height = 560;
        MinWidth = 480;
        MinHeight = 360;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Content = _web;

        Loaded += OnLoaded;
        Closed += (_, _) => _web.Dispose();
    }

    // "async void" je v pořádku jen u obsluhy událostí; chyby proto chytáme uvnitř.
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await WebViewHost.InitializeAsync(_web, "settings");
        }
        catch (WebView2RuntimeNotFoundException ex)
        {
            Log.Error("Chybí WebView2 Runtime", ex);
            ShowError("Na počítači chybí Microsoft Edge WebView2 Runtime.\nStáhni ho z https://developer.microsoft.com/microsoft-edge/webview2/");
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
