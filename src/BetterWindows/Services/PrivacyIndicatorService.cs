using System.Threading;
using BetterWindows.Core;
using BetterWindows.Logic.Messaging;
using BetterWindows.Logic.Privacy;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;

namespace BetterWindows.Services;

/// <summary>
/// Používá teď někdo mikrofon / kameru? Windows to zapisuje do registru:
/// HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\{microphone|webcam}.
/// Aplikace, která zařízení právě používá, má LastUsedTimeStop = 0.
///
/// Hlídání: RegNotifyChangeKeyValue (Windows nám dá vědět o změně v klíči) v samostatném
/// vlákně + pojistka: kontrola každých 5 s, i když žádná změna nepřijde.
/// </summary>
internal sealed class PrivacyIndicatorService : IDisposable
{
    private const string BaseKey = @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore";

    private readonly System.Windows.Threading.Dispatcher _dispatcher;
    private readonly ManualResetEvent _stop = new(false);
    private Thread? _thread;

    public PrivacyStatePayload Current { get; private set; } = new(false, false, [], []);
    public event Action<PrivacyStatePayload>? Changed;

    public PrivacyIndicatorService(System.Windows.Threading.Dispatcher dispatcher) => _dispatcher = dispatcher;

    public void Start()
    {
        _thread = new Thread(Loop) { IsBackground = true, Name = "PrivacyIndicator" };
        _thread.Start();
    }

    private void Loop()
    {
        using var micChanged = new AutoResetEvent(false);
        using var camChanged = new AutoResetEvent(false);
        while (true)
        {
            try
            {
                using var mic = Registry.CurrentUser.OpenSubKey(BaseKey + @"\microphone");
                using var cam = Registry.CurrentUser.OpenSubKey(BaseKey + @"\webcam");
                Arm(mic, micChanged);
                Arm(cam, camChanged);

                Scan(mic, cam);

                // Čekáme na změnu v registru, na ukončení, nebo max. 5 s.
                var index = WaitHandle.WaitAny([_stop, micChanged, camChanged], TimeSpan.FromSeconds(5));
                if (index == 0)
                    return;
                if (index != WaitHandle.WaitTimeout)
                    Thread.Sleep(150); // aplikace zapisuje Start i Stop těsně po sobě
            }
            catch (Exception ex)
            {
                Log.Error("Indikátor mikrofonu/kamery: chyba, zkusím znovu za 10 s", ex);
                if (_stop.WaitOne(TimeSpan.FromSeconds(10)))
                    return;
            }
        }
    }

    /// <summary>Požádá Windows o upozornění (událost) při jakékoli změně v klíči a jeho podklíčích.</summary>
    private static void Arm(RegistryKey? key, AutoResetEvent signal)
    {
        if (key is null)
            return;
        var result = PInvoke.RegNotifyChangeKeyValue(
            new HKEY(key.Handle.DangerousGetHandle()),
            bWatchSubtree: true,
            REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_LAST_SET | REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_NAME,
            new HANDLE(signal.SafeWaitHandle.DangerousGetHandle()),
            fAsynchronous: true);
        if (result != WIN32_ERROR.ERROR_SUCCESS)
            Log.Warn($"RegNotifyChangeKeyValue selhalo: {result}");
    }

    private void Scan(RegistryKey? mic, RegistryKey? cam)
    {
        var micApps = ConsentStore.InUseApps(ReadEntries(mic));
        var camApps = ConsentStore.InUseApps(ReadEntries(cam));
        var state = new PrivacyStatePayload(micApps.Count > 0, camApps.Count > 0, micApps, camApps);
        if (state.MicInUse == Current.MicInUse && state.CamInUse == Current.CamInUse &&
            state.MicApps.SequenceEqual(Current.MicApps) && state.CamApps.SequenceEqual(Current.CamApps))
            return;

        Current = state;
        Log.Info($"Mikrofon: {(state.MicInUse ? string.Join(", ", micApps) : "nepoužíván")}; kamera: {(state.CamInUse ? string.Join(", ", camApps) : "nepoužívána")}");
        _dispatcher.InvokeAsync(() => Changed?.Invoke(state));
    }

    private static IEnumerable<ConsentEntry> ReadEntries(RegistryKey? capability)
    {
        if (capability is null)
            yield break;
        foreach (var name in capability.GetSubKeyNames())
        {
            if (name.Equals("NonPackaged", StringComparison.OrdinalIgnoreCase))
            {
                using var nonPackaged = capability.OpenSubKey(name);
                if (nonPackaged is null)
                    continue;
                foreach (var app in nonPackaged.GetSubKeyNames())
                {
                    using var key = nonPackaged.OpenSubKey(app);
                    if (key is not null)
                        yield return new ConsentEntry(app, true, ReadLong(key, "LastUsedTimeStart"), ReadLong(key, "LastUsedTimeStop"));
                }
                continue;
            }

            using var packaged = capability.OpenSubKey(name);
            if (packaged is not null)
                yield return new ConsentEntry(name, false, ReadLong(packaged, "LastUsedTimeStart"), ReadLong(packaged, "LastUsedTimeStop"));
        }
    }

    private static long? ReadLong(RegistryKey key, string name) => key.GetValue(name) switch
    {
        long l => l,
        int i => i,
        _ => null,
    };

    public void Dispose()
    {
        _stop.Set();
        _thread?.Join(TimeSpan.FromSeconds(2));
        _stop.Dispose();
    }
}
