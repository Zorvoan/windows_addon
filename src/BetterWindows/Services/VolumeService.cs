using BetterWindows.Core;
using BetterWindows.Logic.Messaging;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace BetterWindows.Services;

/// <summary>
/// Hlasitost výchozího zvukového výstupu (Core Audio přes NAudio): čtení, nastavení a upozornění
/// na změnu (i klávesami hlasitosti) – ostrůvek pak ukáže ukazatel.
/// </summary>
internal sealed class VolumeService : IDisposable
{
    private readonly System.Windows.Threading.Dispatcher _dispatcher;
    private readonly MMDeviceEnumerator? _enumerator;
    private readonly DeviceNotifications _notifications;
    private MMDevice? _device;

    public VolumeStatePayload Current { get; private set; } = new(0, false, false);

    /// <summary>(stav, je to změna právě teď?) – volá se v UI vlákně.</summary>
    public event Action<VolumeStatePayload>? Changed;

    public VolumeService(System.Windows.Threading.Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _notifications = new DeviceNotifications(this);
        _enumerator = Safe.Run("Hlasitost – zařízení", () => new MMDeviceEnumerator());
        if (_enumerator is not null)
            Safe.Run("Hlasitost – upozornění", () => _enumerator.RegisterEndpointNotificationCallback(_notifications));
    }

    public void Start() => Safe.Run("Hlasitost", Bind);

    private void Bind()
    {
        if (_device is not null)
        {
            _device.AudioEndpointVolume.OnVolumeNotification -= OnVolume;
            _device.Dispose();
            _device = null;
        }
        if (_enumerator is null)
            return;
        _device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        _device.AudioEndpointVolume.OnVolumeNotification += OnVolume;
        var v = _device.AudioEndpointVolume;
        Publish(new VolumeStatePayload(Math.Round(v.MasterVolumeLevelScalar, 3), v.Mute, false));
    }

    private void OnVolume(AudioVolumeNotificationData data) =>
        _dispatcher.InvokeAsync(() => Publish(new VolumeStatePayload(Math.Round(data.MasterVolume, 3), data.Muted, true)));

    public void Set(double? level, bool? muted) => Safe.Run("Hlasitost – nastavení", () =>
    {
        var v = _device?.AudioEndpointVolume;
        if (v is null)
            return;
        if (level is { } l && double.IsFinite(l))
            v.MasterVolumeLevelScalar = (float)Math.Clamp(l, 0, 1);
        if (muted is { } m)
            v.Mute = m;
    });

    private void Publish(VolumeStatePayload state)
    {
        Current = state;
        Changed?.Invoke(state);
    }

    private sealed class DeviceNotifications(VolumeService owner) : IMMNotificationClient
    {
        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            if (flow == DataFlow.Render && role == Role.Multimedia)
                owner._dispatcher.InvokeAsync(() => Safe.Run("Hlasitost – nové zařízení", owner.Bind));
        }

        public void OnDeviceStateChanged(string deviceId, DeviceState newState) { }
        public void OnDeviceAdded(string pwstrDeviceId) { }
        public void OnDeviceRemoved(string deviceId) { }
        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }
    }

    public void Dispose()
    {
        if (_device is not null)
        {
            _device.AudioEndpointVolume.OnVolumeNotification -= OnVolume;
            _device.Dispose();
        }
        if (_enumerator is not null)
        {
            Safe.Run("Hlasitost – odhlášení", () => _enumerator.UnregisterEndpointNotificationCallback(_notifications));
            _enumerator.Dispose();
        }
    }
}
