namespace BetterWindows.Logic.Audio;

/// <summary>
/// Ze zvukových vzorků (mono, float -1..1) spočítá hodnoty pásem pro vizualizér:
/// okno Hann -> FFT -> amplitudy -> logaritmicky rozložená pásma -> decibely -> 0..1 -> vyhlazení.
/// </summary>
public sealed class SpectrumAnalyzer
{
    private const float MinDb = -72f;
    private const float MaxDb = -12f;

    private readonly int _fftSize;
    private readonly float[] _ring;
    private int _ringPos;
    private readonly float[] _window;
    private readonly float[] _re;
    private readonly float[] _im;
    private readonly (int From, int To)[] _bandBins;
    private readonly float[] _smoothed;

    public int BandCount { get; }

    /// <summary>Kolik po sobě jdoucích výpočtů bylo ticho (pro zastavení odesílání).</summary>
    public int SilentFrames { get; private set; }

    public SpectrumAnalyzer(int sampleRate, int fftSize = 2048, int bandCount = 48, double minHz = 40, double maxHz = 16000)
    {
        if ((fftSize & (fftSize - 1)) != 0) throw new ArgumentException("fftSize musí být mocnina dvou");
        _fftSize = fftSize;
        BandCount = bandCount;
        _ring = new float[fftSize];
        _re = new float[fftSize];
        _im = new float[fftSize];
        _smoothed = new float[bandCount];
        _window = new float[fftSize];
        for (var i = 0; i < fftSize; i++)
            _window[i] = (float)(0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (fftSize - 1)));

        // Hranice pásem rovnoměrně na logaritmické ose (tak slyší ucho).
        maxHz = Math.Min(maxHz, sampleRate / 2.0);
        var binHz = (double)sampleRate / fftSize;
        _bandBins = new (int, int)[bandCount];
        for (var b = 0; b < bandCount; b++)
        {
            var lo = minHz * Math.Pow(maxHz / minHz, (double)b / bandCount);
            var hi = minHz * Math.Pow(maxHz / minHz, (double)(b + 1) / bandCount);
            var from = Math.Max(1, (int)Math.Floor(lo / binHz));
            var to = Math.Max(from, (int)Math.Ceiling(hi / binHz) - 1);
            _bandBins[b] = (from, Math.Min(to, fftSize / 2 - 1));
        }
    }

    public void AddSamples(ReadOnlySpan<float> mono)
    {
        foreach (var s in mono)
        {
            _ring[_ringPos] = s;
            _ringPos = (_ringPos + 1) % _fftSize;
        }
    }

    /// <summary>Spočítá aktuální pásma (0..1). Volat cca 30× za sekundu.</summary>
    public float[] ComputeBands(double sensitivity = 1.0)
    {
        for (var i = 0; i < _fftSize; i++)
        {
            _re[i] = _ring[(_ringPos + i) % _fftSize] * _window[i];
            _im[i] = 0;
        }
        Fft.Transform(_re, _im);

        var result = new float[BandCount];
        var anySound = false;
        var norm = 2f / (_fftSize * 0.5f); // amplituda sinusovky 1.0 -> ~1.0 (0 dB), 0.5 = korekce okna Hann
        for (var b = 0; b < BandCount; b++)
        {
            var (from, to) = _bandBins[b];
            var peak = 0f;
            for (var k = from; k <= to; k++)
            {
                var mag = MathF.Sqrt(_re[k] * _re[k] + _im[k] * _im[k]) * norm;
                if (mag > peak) peak = mag;
            }
            var db = 20f * MathF.Log10(peak * (float)sensitivity + 1e-9f);
            var value = Math.Clamp((db - MinDb) / (MaxDb - MinDb), 0f, 1f);

            // Rychlý náběh, pomalejší doznívání = klidnější animace.
            var prev = _smoothed[b];
            var k2 = value > prev ? 0.6f : 0.18f;
            var smoothed = prev + (value - prev) * k2;
            _smoothed[b] = smoothed;
            result[b] = MathF.Round(smoothed, 3);
            if (smoothed > 0.02f) anySound = true;
        }

        SilentFrames = anySound ? 0 : SilentFrames + 1;
        return result;
    }
}
