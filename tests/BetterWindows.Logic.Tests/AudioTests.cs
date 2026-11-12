using BetterWindows.Logic.Audio;

namespace BetterWindows.Logic.Tests;

public class AudioTests
{
    [Fact]
    public void Fft_of_impulse_is_flat()
    {
        var re = new float[8]; var im = new float[8];
        re[0] = 1;
        Fft.Transform(re, im);
        Assert.All(re, v => Assert.Equal(1f, v, 4));
        Assert.All(im, v => Assert.Equal(0f, v, 4));
    }

    [Fact]
    public void Sine_1khz_peaks_in_matching_band()
    {
        const int rate = 48000;
        var analyzer = new SpectrumAnalyzer(rate, fftSize: 2048, bandCount: 48);
        var samples = new float[4096];
        for (var i = 0; i < samples.Length; i++)
            samples[i] = 0.5f * MathF.Sin(2 * MathF.PI * 1000 * i / rate);
        analyzer.AddSamples(samples);

        float[] bands = [];
        for (var i = 0; i < 10; i++) bands = analyzer.ComputeBands(); // vyhlazení se ustálí

        var peak = Array.IndexOf(bands, bands.Max());
        // 1 kHz na log ose 40 Hz–16 kHz: log(1000/40)/log(16000/40) * 48 ≈ 25.8
        Assert.InRange(peak, 24, 27);
        Assert.True(bands[peak] > 0.8f);
        Assert.True(bands[2] < 0.3f);
    }

    [Fact]
    public void Silence_is_detected()
    {
        var analyzer = new SpectrumAnalyzer(44100);
        analyzer.AddSamples(new float[4096]);
        var bands = analyzer.ComputeBands();
        Assert.All(bands, b => Assert.Equal(0f, b));
        Assert.Equal(1, analyzer.SilentFrames);
    }
}
