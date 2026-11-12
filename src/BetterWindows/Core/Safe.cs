namespace BetterWindows.Core;

/// <summary>
/// "Pojistka funkce": chyba v jedné části (počasí, dock, vizualizér...) se zaloguje
/// a ta část se vypne, ale zbytek shellu běží dál.
/// </summary>
public static class Safe
{
    /// <summary>Spustí akci; při výjimce zaloguje a vrátí false.</summary>
    public static bool Run(string feature, Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[{feature}] selhalo", ex);
            return false;
        }
    }

    public static T? Run<T>(string feature, Func<T> func, T? fallback = default)
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            Log.Error($"[{feature}] selhalo", ex);
            return fallback;
        }
    }

    /// <summary>Pro "fire and forget" asynchronní úlohy: chyba se zaloguje, nezmizí potichu.</summary>
    public static async void Fire(string feature, Func<Task> work)
    {
        try
        {
            await work();
        }
        catch (Exception ex)
        {
            Log.Error($"[{feature}] selhalo", ex);
        }
    }
}
