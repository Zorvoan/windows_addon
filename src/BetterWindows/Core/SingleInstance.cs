using System.Threading;

namespace BetterWindows.Core;

/// <summary>
/// Zajistí, že běží jen jedna instance aplikace.
///
/// Jak to funguje:
///  - Pojmenovaný Mutex ("zámek" viditelný napříč procesy). Kdo ho vytvoří první, je hlavní instance.
///  - Pojmenovaný EventWaitHandle ("zvonek"). Druhá instance na něj zazvoní a skončí,
///    hlavní instance zvonění uslyší a zareaguje (otevře nastavení).
/// Prefix "Local\" = platí jen pro aktuální přihlášenou relaci Windows.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private const string MutexName = $@"Local\{AppInfo.Id}.SingleInstance";
    private const string EventName = $@"Local\{AppInfo.Id}.Activate";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activateEvent;
    private readonly RegisteredWaitHandle _waitRegistration;

    /// <summary>Někdo spustil aplikaci podruhé. Volá se z vlákna thread poolu, ne z UI vlákna!</summary>
    public event Action? SecondInstanceStarted;

    private SingleInstance(Mutex mutex, EventWaitHandle activateEvent)
    {
        _mutex = mutex;
        _activateEvent = activateEvent;
        // Čekání na "zvonek" bez blokování vlastního vlákna (thread pool to hlídá za nás).
        _waitRegistration = ThreadPool.RegisterWaitForSingleObject(
            _activateEvent, (_, _) => SecondInstanceStarted?.Invoke(), null, Timeout.Infinite, executeOnlyOnce: false);
    }

    /// <summary>
    /// Vrátí objekt, pokud jsme první instance. Jinak upozorní běžící instanci a vrátí null.
    /// </summary>
    public static SingleInstance? TryAcquire()
    {
        var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        var activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);

        if (createdNew)
            return new SingleInstance(mutex, activateEvent);

        activateEvent.Set();
        activateEvent.Dispose();
        mutex.Dispose();
        return null;
    }

    public void Dispose()
    {
        _waitRegistration.Unregister(null);
        _activateEvent.Dispose();
        try { _mutex.ReleaseMutex(); } catch (ApplicationException) { /* už uvolněn */ }
        _mutex.Dispose();
    }
}
