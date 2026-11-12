using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BetterWindows.Core;
using BetterWindows.Logic.Apps;
using BetterWindows.Native;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Shell;

namespace BetterWindows.Services;

/// <summary>
/// Ikony aplikací jako PNG (data URL) pro dock.
///  - exe: položka Shellu podle cesty k souboru,
///  - aplikace z balíčku: položka "shell:AppsFolder\AUMID" (Shell sám vezme ikonu z balíčku),
///  - obrázek přes IShellItemImageFactory (velká ikona s průhledností), záloha ExtractAssociatedIcon.
/// Shell COM objekty vyžadují STA vlákno, a extrakce může trvat -> vlastní vlákno s frontou.
/// Cache: v paměti + na disku (%LocalAppData%\BetterWindows\icons).
/// </summary>
internal sealed class IconService : IDisposable
{
    private const int IconSize = 128; // stačí i pro zvětšenou ikonu na 200 % měřítku

    private readonly BlockingCollection<string> _queue = new();
    private readonly ConcurrentDictionary<string, string?> _memory = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _diskDir = Path.Combine(AppInfo.LocalDataDir, "icons");
    private readonly Thread _worker;

    /// <summary>Ikona je hotová (appId, data URL nebo null). Volá se z pracovního vlákna!</summary>
    public event Action<string, string?>? IconReady;

    public IconService()
    {
        Directory.CreateDirectory(_diskDir);
        _worker = new Thread(Work) { IsBackground = true, Name = "IconService" };
        _worker.SetApartmentState(ApartmentState.STA);
        _worker.Start();
    }

    /// <summary>Vrátí ikonu z paměti, nebo null a zařadí ji do fronty (výsledek přijde přes IconReady).</summary>
    public bool TryGet(string appId, out string? dataUrl)
    {
        if (_memory.TryGetValue(appId, out dataUrl))
            return true;
        if (_pending.TryAdd(appId, 0))
            _queue.Add(appId);
        return false;
    }

    private void Work()
    {
        foreach (var appId in _queue.GetConsumingEnumerable())
        {
            string? url = null;
            try
            {
                url = LoadFromDisk(appId) ?? Extract(appId);
            }
            catch (Exception ex)
            {
                Log.Warn($"Ikona {appId}: {ex.Message}");
            }
            _memory[appId] = url;
            _pending.TryRemove(appId, out _);
            try { IconReady?.Invoke(appId, url); }
            catch (Exception ex) { Log.Error("IconReady selhalo", ex); }
        }
    }

    private string DiskPath(string appId)
    {
        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(appId.ToLowerInvariant())))[..16];
        return Path.Combine(_diskDir, $"{hash}-{IconSize}.png");
    }

    private string? LoadFromDisk(string appId)
    {
        var path = DiskPath(appId);
        if (!File.Exists(path) || DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > TimeSpan.FromDays(7))
            return null;
        return "data:image/png;base64," + Convert.ToBase64String(File.ReadAllBytes(path));
    }

    private string? Extract(string appId)
    {
        var (kind, value) = AppId.Parse(appId);
        var parsingName = kind switch
        {
            AppIdKind.Exe => value,
            AppIdKind.Aumid => "shell:AppsFolder\\" + value,
            _ => null,
        };
        if (parsingName is null)
            return null;

        var png = ExtractWithShell(parsingName) ?? (kind == AppIdKind.Exe ? ExtractAssociated(value) : null);
        if (png is null)
            return null;
        Safe.Run("Uložení ikony", () => File.WriteAllBytes(DiskPath(appId), png));
        return "data:image/png;base64," + Convert.ToBase64String(png);
    }

    private static unsafe byte[]? ExtractWithShell(string parsingName)
    {
        var item = ShellItems.Create(parsingName);
        if (item is not IShellItemImageFactory factory)
            return null;
        try
        {
            // ICONONLY = ikona, ne náhled obsahu souboru.
            factory.GetImage(new SIZE(IconSize, IconSize), SIIGBF.SIIGBF_ICONONLY | SIIGBF.SIIGBF_BIGGERSIZEOK, out var bitmapHandle);
            // SafeHandle: bitmapu po použití sám uvolní (DeleteObject).
            using (bitmapHandle)
                return HBitmapToPng(new HBITMAP(bitmapHandle.DangerousGetHandle()));
        }
        catch (COMException)
        {
            return null;
        }
        finally
        {
            Marshal.ReleaseComObject(item);
        }
    }

    /// <summary>
    /// HBITMAP -> PNG s průhledností. (Image.FromHbitmap průhlednost ztrácí, proto čteme pixely
    /// ručně přes GetDIBits jako 32bitové BGRA, řádky odshora dolů.)
    /// </summary>
    private static unsafe byte[]? HBitmapToPng(HBITMAP hbitmap)
    {
        BITMAP info;
        if (PInvoke.GetObject(new HGDIOBJ(hbitmap.Value), sizeof(BITMAP), &info) == 0)
            return null;
        int w = info.bmWidth, h = Math.Abs(info.bmHeight);
        if (w <= 0 || h <= 0)
            return null;

        var bmi = new BITMAPINFO();
        bmi.bmiHeader.biSize = (uint)sizeof(BITMAPINFOHEADER);
        bmi.bmiHeader.biWidth = w;
        bmi.bmiHeader.biHeight = -h; // záporná výška = řádky odshora
        bmi.bmiHeader.biPlanes = 1;
        bmi.bmiHeader.biBitCount = 32;
        bmi.bmiHeader.biCompression = 0; // BI_RGB

        var pixels = new byte[w * h * 4];
        var dc = PInvoke.CreateCompatibleDC(HDC.Null);
        try
        {
            fixed (byte* p = pixels)
            {
                if (PInvoke.GetDIBits(dc, hbitmap, 0, (uint)h, p, &bmi, DIB_USAGE.DIB_RGB_COLORS) == 0)
                    return null;
            }
        }
        finally
        {
            PInvoke.DeleteDC(dc);
        }

        // Některé ikony nemají alfa kanál (vše 0) -> brát jako neprůhledné.
        var hasAlpha = false;
        for (var i = 3; i < pixels.Length; i += 4)
            if (pixels[i] != 0) { hasAlpha = true; break; }
        if (!hasAlpha)
            for (var i = 3; i < pixels.Length; i += 4)
                pixels[i] = 255;

        // Shell vrací "předem vynásobenou" průhlednost (premultiplied) -> Pbgra32.
        var source = BitmapSource.Create(w, h, 96, 96, hasAlpha ? PixelFormats.Pbgra32 : PixelFormats.Bgra32, null, pixels, w * 4);
        source.Freeze();
        return EncodePng(source);
    }

    private static byte[]? ExtractAssociated(string exePath)
    {
        if (!File.Exists(exePath))
            return null;
        using var icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
        if (icon is null)
            return null;
        using var bitmap = icon.ToBitmap();
        using var ms = new MemoryStream();
        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        return ms.ToArray();
    }

    private static byte[] EncodePng(BitmapSource source)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return ms.ToArray();
    }

    public void Dispose()
    {
        _queue.CompleteAdding();
        _worker.Join(TimeSpan.FromSeconds(1));
    }
}
