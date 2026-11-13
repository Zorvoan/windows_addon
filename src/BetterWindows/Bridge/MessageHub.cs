using System.Text.Json;
using System.Windows.Threading;
using BetterWindows.Core;
using BetterWindows.Logic.Messaging;

namespace BetterWindows.Bridge;

/// <summary>
/// Ústředna zpráv mezi službami v C# a všemi stránkami.
///  - Publish: pošle zprávu všem (nebo jedné) stránce. "Sticky" zprávy (stav baterie, počasí...)
///    si pamatuje a pošle je i stránce, která se načte později.
///  - On: obsluha zprávy z JS. Chyba v obsluze se zaloguje, nic dalšího neshodí.
/// Vše běží v UI vlákně (WebView2 to vyžaduje); Publish z jiného vlákna se tam sám přesune.
/// </summary>
internal sealed class MessageHub(Dispatcher dispatcher)
{
    private readonly List<MessageBridge> _bridges = [];
    private readonly Dictionary<string, string> _sticky = [];
    private readonly Dictionary<string, List<Action<MessageBridge, JsonElement>>> _handlers = [];

    public void Register(MessageBridge bridge)
    {
        _bridges.Add(bridge);
        bridge.MessageReceived += Dispatch;
    }

    public void Unregister(MessageBridge bridge)
    {
        bridge.MessageReceived -= Dispatch;
        _bridges.Remove(bridge);
    }

    /// <param name="sticky">Zapamatovat a poslat i později načteným stránkám.</param>
    /// <param name="page">Jen pro tuto stránku (null = všem).</param>
    public void Publish(string type, object payload, bool sticky = true, string? page = null)
    {
        if (!dispatcher.CheckAccess())
        {
            dispatcher.InvokeAsync(() => Publish(type, payload, sticky, page));
            return;
        }

        string json;
        try
        {
            json = Envelope.Serialize(type, payload);
        }
        catch (Exception ex)
        {
            Log.Error($"Serializace zprávy {type} selhala", ex);
            return;
        }

        if (sticky)
            _sticky[type] = json;
        foreach (var bridge in _bridges.ToArray())
        {
            if (bridge.IsReady && (page is null || bridge.Page == page))
                bridge.PostJson(json);
        }
    }

    /// <summary>Jen zapamatovat stav pro později načtené stránky (nic neposílat).</summary>
    public void Remember(string type, object payload)
    {
        if (!dispatcher.CheckAccess())
        {
            dispatcher.InvokeAsync(() => Remember(type, payload));
            return;
        }
        _sticky[type] = Envelope.Serialize(type, payload);
    }

    /// <summary>Pošle zprávu jedné konkrétní stránce (např. odpověď na dotaz).</summary>
    public void Send(MessageBridge bridge, string type, object payload)
    {
        if (bridge.IsReady)
            bridge.PostJson(Envelope.Serialize(type, payload));
    }

    /// <summary>Obsluha zprávy z JS s typovaným obsahem (payload může být null).</summary>
    public void On<T>(string type, Action<MessageBridge, T?> handler) where T : class
    {
        if (!_handlers.TryGetValue(type, out var list))
            _handlers[type] = list = [];
        list.Add((bridge, payload) => handler(bridge, Envelope.Read<T>(payload)));
    }

    private void Dispatch(MessageBridge bridge, string type, JsonElement payload)
    {
        if (type == MessageTypes.UiReady)
        {
            bridge.IsReady = true;
            foreach (var json in _sticky.Values)
                bridge.PostJson(json);
        }

        if (!_handlers.TryGetValue(type, out var list))
        {
            if (type != MessageTypes.UiReady)
                Log.Warn($"[{bridge.Page}] zpráva bez obsluhy: {type}");
            return;
        }

        foreach (var handler in list)
        {
            try
            {
                handler(bridge, payload);
            }
            catch (Exception ex)
            {
                Log.Error($"[{bridge.Page}] obsluha zprávy {type} selhala", ex);
            }
        }
    }
}
