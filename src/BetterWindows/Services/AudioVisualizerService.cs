using System.Runtime.InteropServices;
using BetterWindows.Core;
using BetterWindows.Logic.Audio;
using BetterWindows.Logic.Settings;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;

namespace BetterWindows.Services;

/// <summary>
/// Vizualizér: zachytává zvuk, který jde do REPRODUKTORŮ (WASAPI loopback), ne mikrofon.
/// Každých ~33 ms spočítá pásma (FFT v BetterWindows.Logic) a pošle je stránce plochy.
/// Při tichu posílá jen jeden nulový snímek a pak nic (šetří CPU). Při hře přes celou
/// obrazovku se pozastaví. Změna výchozího zvukového zařízení = restart zachytávání.
/// </summary>
internal sealed class AudioVisualizerService : IDisposable
{
    private readonly SettingsService _settings;
    private readonly object _gate = new();
    private readonly MMDeviceEnumerator? _enumerator;
    private readonly DeviceNotifications _notifications;
    private WasapiLoopbackCapture? _capture;
    private SpectrumAnalyzer? _analyzer;
    private Timer? _timer;
    private DateTime _lastData = DateTime.MinValue;
    private bool _sentSilence;
    private bool _paused;
    private int _channels;
    private int _sampleRate;
    private bool _isFloat;
    private int _bits;

    /// <summary>Nový snímek pásem (0..1). Volá se z vlákna časovače!</summary>
    public event Action<float[]>? Frame;

    public AudioVisualizerService(SettingsService settings)
    {
        _settings = settings;
        _notifications = new DeviceNotifications(this);
        // Když zvuková služba Windows neběží, vizualizér prostě nebude – zbytek aplikace ano.
        _enumerator = Safe.Run("Zvuková zařízení", () => new MMDeviceEnumerator());
        if (_enumerator is not null)
            Safe.Run("Zvuková zařízení – upozornění", () => _enumerator.RegisterEndpointNotificationCallback(_notifications));
        _settings.Changed += OnSettingsChanged;
    }

    /// <summary>Zachytávat, když zvuk potřebuje vizualizér na ploše nebo proužky v ostrůvku.</summary>
    private bool ShouldRun => !_paused &&
        ((_settings.Current.Desktop.Enabled && _settings.Current.Desktop.Visualizer.Enabled) || _settings.Current.Island.Enabled);

    public void Start()
    {
        if (ShouldRun)
            StartCapture();
    }

    /// <summary>Pozastavit (hra přes celou obrazovku / úspora výkonu).</summary>
    public void SetPaused(bool paused)
    {
        _paused = paused;
        if (paused) StopCapture(); else Start();
    }

    private void StartCapture()
    {
        lock (_gate)
        {
            if (_capture is not null)
                return;
            try
            {
                _capture = new WasapiLoopbackCapture();
                var format = _capture.WaveFormat;
                _channels = Math.Max(1, format.Channels);
                _sampleRate = format.SampleRate;
                _isFloat = format.Encoding == WaveFormatEncoding.IeeeFloat ||
                           (format is WaveFormatExtensible ext && ext.SubFormat == NAudio.Dmo.AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT);
                _bits = format.BitsPerSample;
                _analyzer = new SpectrumAnalyzer(_sampleRate);
                _capture.DataAvailable += OnData;
                _capture.RecordingStopped += OnStopped;
                _capture.StartRecording();
                _timer = new Timer(_ => Safe.Run("Vizualizér – snímek", Tick), null, 0, 33);
                Log.Info($"Vizualizér: zachytávám {format.SampleRate} Hz, {format.Channels} kanály, {format.BitsPerSample} bit, {(_isFloat ? "float" : "PCM")}");
            }
            catch (Exception ex) when (ex is COMException or InvalidOperationException or ArgumentException)
            {
                Log.Warn($"Vizualizér: zachytávání nejde spustit ({ex.Message}). Zkusím znovu za 10 s.");
                // Nahrávání se nerozběhlo, takže Dispose nebude na nic čekat.
                DisposeOutsideLock(DetachCapture());
                _timer = new Timer(_ => { StopTimer(); if (ShouldRun) StartCapture(); }, null, 10000, Timeout.Infinite);
            }
        }
    }

    private void StopCapture()
    {
        WasapiLoopbackCapture? old;
        lock (_gate)
        {
            StopTimer();
            old = DetachCapture();
        }
        DisposeOutsideLock(old);
        Frame?.Invoke(new float[48]); // zhasnout
    }

    private void StopTimer()
    {
        _timer?.Dispose();
        _timer = null;
    }

    /// <summary>Odpojí zachytávání (volat pod zámkem). Uvolnit se musí MIMO zámek – viz DisposeOutsideLock.</summary>
    private WasapiLoopbackCapture? DetachCapture()
    {
        var capture = _capture;
        if (capture is null)
            return null;
        capture.DataAvailable -= OnData;
        capture.RecordingStopped -= OnStopped;
        _capture = null;
        _analyzer = null;
        return capture;
    }

    /// <summary>
    /// Dispose čeká na zachytávací vlákno. To může zrovna čekat na náš zámek (OnData),
    /// proto se uvolňuje až po jeho opuštění – jinak by se obě vlákna navzájem zablokovala.
    /// </summary>
    private static void DisposeOutsideLock(WasapiLoopbackCapture? capture)
    {
        if (capture is null)
            return;
        Safe.Run("Vizualizér – stop", capture.StopRecording);
        Safe.Run("Vizualizér – uvolnění", capture.Dispose);
    }

    private void OnData(object? sender, WaveInEventArgs e)
    {
        var analyzer = _analyzer;
        if (analyzer is null || e.BytesRecorded == 0 || _bits < 16)
            return;

        // Převod na mono float -1..1 (průměr kanálů).
        int bytesPerSample = _bits / 8;
        int frames = e.BytesRecorded / (bytesPerSample * _channels);
        var mono = new float[frames];
        var span = e.Buffer.AsSpan(0, frames * bytesPerSample * _channels);
        for (var f = 0; f < frames; f++)
        {
            float sum = 0;
            for (var c = 0; c < _channels; c++)
            {
                var offset = (f * _channels + c) * bytesPerSample;
                sum += _isFloat && _bits == 32
                    ? BitConverter.ToSingle(span.Slice(offset, 4))
                    : _bits == 16 ? BitConverter.ToInt16(span.Slice(offset, 2)) / 32768f
                    : _bits == 24 ? ((span[offset + 2] << 24 | span[offset + 1] << 16 | span[offset] << 8) >> 8) / 8388608f
                    : _bits == 32 ? BitConverter.ToInt32(span.Slice(offset, 4)) / 2147483648f
                    : 0;
            }
            mono[f] = sum / _channels;
        }
        lock (_gate)
        {
            if (_analyzer != analyzer)
                return; // mezitím restart
            analyzer.AddSamples(mono);
            _lastData = DateTime.UtcNow;
        }
    }

    private void Tick()
    {
        float[] bands;
        lock (_gate)
        {
            var analyzer = _analyzer;
            if (analyzer is null)
                return;
            // Když nic nehraje, WASAPI neposílá data -> dosypat ticho, ať pásma klesnou.
            if (DateTime.UtcNow - _lastData > TimeSpan.FromMilliseconds(60))
                analyzer.AddSamples(new float[_sampleRate / 30]);
            bands = analyzer.ComputeBands(_settings.Current.Desktop.Visualizer.Sensitivity);
            if (analyzer.SilentFrames > 0)
            {
                if (_sentSilence)
                    return;
                _sentSilence = true;
            }
            else
            {
                _sentSilence = false;
            }
        }
        Frame?.Invoke(bands);
    }

    private void OnStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is null)
            return;
        Log.Warn($"Vizualizér: zachytávání skončilo chybou ({e.Exception.Message}), restart za 2 s.");
        Task.Delay(2000).ContinueWith(_ => Restart());
    }

    private void Restart()
    {
        WasapiLoopbackCapture? old;
        lock (_gate)
        {
            StopTimer();
            old = DetachCapture();
        }
        DisposeOutsideLock(old);
        if (ShouldRun)
            StartCapture();
    }

    private void OnSettingsChanged(AppSettings old, AppSettings now)
    {
        var shouldRun = ShouldRun;
        var wasRunning = !_paused && ((old.Desktop.Enabled && old.Desktop.Visualizer.Enabled) || old.Island.Enabled);
        if (shouldRun && !wasRunning) StartCapture();
        else if (!shouldRun && wasRunning) StopCapture();
    }

    /// <summary>Windows hlásí změnu výchozího zvukového výstupu (např. připojená sluchátka).</summary>
    private sealed class DeviceNotifications(AudioVisualizerService owner) : IMMNotificationClient
    {
        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            if (flow == DataFlow.Render && role == Role.Multimedia)
            {
                Log.Info("Vizualizér: změna výchozího zvukového výstupu.");
                Task.Delay(500).ContinueWith(_ => owner.Restart());
            }
        }

        public void OnDeviceStateChanged(string deviceId, DeviceState newState) { }
        public void OnDeviceAdded(string pwstrDeviceId) { }
        public void OnDeviceRemoved(string deviceId) { }
        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }
    }

    public void Dispose()
    {
        _settings.Changed -= OnSettingsChanged;
        if (_enumerator is not null)
            Safe.Run("Zvuková zařízení – odhlášení", () => _enumerator.UnregisterEndpointNotificationCallback(_notifications));
        WasapiLoopbackCapture? old;
        lock (_gate)
        {
            StopTimer();
            old = DetachCapture();
        }
        DisposeOutsideLock(old);
        _enumerator?.Dispose();
    }
}
