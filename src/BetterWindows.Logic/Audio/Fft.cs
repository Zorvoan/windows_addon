namespace BetterWindows.Logic.Audio;

/// <summary>Rychlá Fourierova transformace (radix-2, na místě). Délka musí být mocnina dvou.</summary>
public static class Fft
{
    public static void Transform(Span<float> re, Span<float> im)
    {
        var n = re.Length;
        if (n != im.Length || n == 0 || (n & (n - 1)) != 0)
            throw new ArgumentException("Délka musí být mocnina dvou a re/im stejně dlouhé.");

        // Přeházení prvků podle obráceného pořadí bitů.
        for (int i = 1, j = 0; i < n; i++)
        {
            var bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1)
                j ^= bit;
            j ^= bit;
            if (i < j)
            {
                (re[i], re[j]) = (re[j], re[i]);
                (im[i], im[j]) = (im[j], im[i]);
            }
        }

        // "Motýlky" – skládání menších transformací do větších.
        for (var len = 2; len <= n; len <<= 1)
        {
            var angle = -2 * Math.PI / len;
            var wRe = (float)Math.Cos(angle);
            var wIm = (float)Math.Sin(angle);
            for (var start = 0; start < n; start += len)
            {
                float curRe = 1, curIm = 0;
                for (var k = 0; k < len / 2; k++)
                {
                    var a = start + k;
                    var b = a + len / 2;
                    var tRe = re[b] * curRe - im[b] * curIm;
                    var tIm = re[b] * curIm + im[b] * curRe;
                    re[b] = re[a] - tRe;
                    im[b] = im[a] - tIm;
                    re[a] += tRe;
                    im[a] += tIm;
                    var nextRe = curRe * wRe - curIm * wIm;
                    curIm = curRe * wIm + curIm * wRe;
                    curRe = nextRe;
                }
            }
        }
    }
}
