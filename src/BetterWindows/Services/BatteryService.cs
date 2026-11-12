using System.Windows.Threading;
using BetterWindows.Core;
using BetterWindows.Logic.Messaging;
using Microsoft.Win32;
using Windows.Win32;

namespace BetterWindows.Services;

/// <summary>Stav baterie přes GetSystemPowerStatus. Obnova při změně napájení a každou minutu.</summary>
internal sealed class BatteryService : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _timer;

    public BatteryStatePayload Current { get; private set; } = new(false, null, false, true);
    /// <summary>Zapnutá "Úspora energie" Windows.</summary>
    public bool BatterySaver { get; private set; }
    public event Action<BatteryStatePayload>? Changed;

    public BatteryService(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _timer = new DispatcherTimer(TimeSpan.FromSeconds(60), DispatcherPriority.Background, (_, _) => Refresh(), dispatcher);
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    public void Start()
    {
        Refresh();
        _timer.Start();
    }

    private void Refresh() => Safe.Run("Baterie", () =>
    {
        if (!PInvoke.GetSystemPowerStatus(out var s))
            return;
        // BatteryFlag: 128 = počítač nemá baterii, 255 = neznámé; 8 = nabíjí se.
        var present = s.BatteryFlag != 128 && s.BatteryFlag != 255;
        var state = new BatteryStatePayload(
            Present: present,
            Percent: s.BatteryLifePercent == 255 ? null : s.BatteryLifePercent,
            Charging: present && (s.BatteryFlag & 8) != 0,
            OnAc: s.ACLineStatus == 1);
        var saver = s.SystemStatusFlag == 1;
        if (state == Current && saver == BatterySaver)
            return;
        Current = state;
        BatterySaver = saver;
        Changed?.Invoke(state);
    });

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e) => _dispatcher.InvokeAsync(Refresh);

    public void Dispose()
    {
        _timer.Stop();
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
    }
}
