using System.Runtime.InteropServices;
using System.Text.Json;
using BetterWindows.Core;
using BetterWindows.Logic.Messaging;
using Microsoft.Web.WebView2.Core;

namespace BetterWindows.Bridge;

/// <summary>
/// Spojení s jednou stránkou ve WebView2.
///  - C# -> JS: PostWebMessageAsJson (v JS přijde jako event "message" s hotovým objektem),
///  - JS -> C#: window.chrome.webview.postMessage(obj) (v C# událost WebMessageReceived).
/// </summary>
internal sealed class MessageBridge
{
    private readonly CoreWebView2 _core;

    /// <summary>Jméno stránky (topbar, dock, desktop, settings).</summary>
    public string Page { get; }

    /// <summary>Stránka poslala "ui.ready" = má posluchače a může přijímat zprávy.</summary>
    public bool IsReady { get; set; }

    public event Action<MessageBridge, string, JsonElement>? MessageReceived;

    public MessageBridge(string page, CoreWebView2 core)
    {
        Page = page;
        _core = core;
        _core.WebMessageReceived += OnWebMessageReceived;
        // Znovunačtení stránky (Ctrl+R) -> do dalšího "ui.ready" nic neposílat.
        _core.NavigationStarting += (_, _) => IsReady = false;
    }

    public void PostJson(string json)
    {
        try
        {
            _core.PostWebMessageAsJson(json);
        }
        catch (Exception ex) when (ex is InvalidOperationException or COMException or ObjectDisposedException)
        {
            // Okno se právě zavírá – zprávu nemá kdo přijmout.
            IsReady = false;
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        string json;
        try
        {
            json = e.WebMessageAsJson;
        }
        catch (Exception ex)
        {
            Log.Warn($"[{Page}] nečitelná zpráva z JS", ex);
            return;
        }

        if (!Envelope.TryParse(json, out var type, out var payload))
        {
            Log.Warn($"[{Page}] neplatná zpráva z JS: {Truncate(json)}");
            return;
        }
        MessageReceived?.Invoke(this, type, payload);
    }

    private static string Truncate(string s) => s.Length <= 200 ? s : s[..200] + "…";
}
