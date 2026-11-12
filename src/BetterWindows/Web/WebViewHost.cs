using System.IO;
using BetterWindows.Core;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace BetterWindows.Web;

/// <summary>
/// Společné nastavení všech WebView2 v aplikaci.
///
/// Rozhraní se nenačítá z localhost serveru: složka "ui" vedle exe se namapuje
/// na virtuální doménu https://app.betterwindows/ (SetVirtualHostNameToFolderMapping).
/// Pro stránku to vypadá jako běžný HTTPS web, ale nic neposlouchá na žádném portu.
/// </summary>
public static class WebViewHost
{
    public const string HostName = "app.betterwindows";
    private static readonly string Origin = $"https://{HostName}/";

    private static Task<CoreWebView2Environment>? _environment;

    /// <summary>
    /// Jedno sdílené prostředí = jeden prohlížečový proces pro všechna okna (šetří paměť).
    /// Data (cache, localStorage) jdou do %LocalAppData%, ne vedle exe, kam často nejde zapisovat.
    /// </summary>
    public static Task<CoreWebView2Environment> GetEnvironmentAsync() =>
        _environment ??= CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: Path.Combine(AppInfo.LocalDataDir, "WebView2"));

    /// <summary>Připraví WebView2 a načte stránku ui/&lt;page&gt;.html.</summary>
    /// <exception cref="WebView2RuntimeNotFoundException">Na počítači chybí WebView2 Runtime.</exception>
    public static async Task InitializeAsync(WebView2 view, string page)
    {
        if (!File.Exists(Path.Combine(AppInfo.UiDir, $"{page}.html")))
            throw new FileNotFoundException(
                $"Chybí sestavené rozhraní: {Path.Combine(AppInfo.UiDir, page)}.html. Sestav projekt znovu (dotnet build).");

        var environment = await GetEnvironmentAsync();
        await view.EnsureCoreWebView2Async(environment);
        var core = view.CoreWebView2;

        core.SetVirtualHostNameToFolderMapping(HostName, AppInfo.UiDir, CoreWebView2HostResourceAccessKind.Deny);

        var settings = core.Settings;
        settings.IsStatusBarEnabled = false;
        settings.IsZoomControlEnabled = false;
        settings.IsPinchZoomEnabled = false;
        settings.IsGeneralAutofillEnabled = false;
        settings.IsPasswordAutosaveEnabled = false;
#if DEBUG
        // Při vývoji: F12 / pravé tlačítko -> Prozkoumat, Ctrl+R = znovu načíst.
        settings.AreDevToolsEnabled = true;
        settings.AreDefaultContextMenusEnabled = true;
        settings.AreBrowserAcceleratorKeysEnabled = true;
#else
        settings.AreDevToolsEnabled = false;
        settings.AreDefaultContextMenusEnabled = false;
        settings.AreBrowserAcceleratorKeysEnabled = false;
#endif

        // Bezpečnost: stránka smí zůstat jen na naší virtuální doméně.
        core.NavigationStarting += (_, e) =>
        {
            if (!e.Uri.StartsWith(Origin, StringComparison.OrdinalIgnoreCase))
            {
                Log.Warn($"Zablokována navigace mimo aplikaci: {e.Uri}");
                e.Cancel = true;
            }
        };
        core.NewWindowRequested += (_, e) =>
        {
            Log.Warn($"Zablokováno otevření nového okna: {e.Uri}");
            e.Handled = true;
        };
        core.ProcessFailed += (_, e) =>
            Log.Error($"Proces WebView2 selhal ({page}): {e.ProcessFailedKind}, důvod {e.Reason}, kód {e.ExitCode}");

        core.Navigate($"{Origin}{page}.html");
        Log.Info($"WebView2 '{page}' načteno (runtime {environment.BrowserVersionString}).");
    }
}
