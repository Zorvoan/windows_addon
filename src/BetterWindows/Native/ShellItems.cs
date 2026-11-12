using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace BetterWindows.Native;

/// <summary>Práce s položkami Shellu (soubory, aplikace v shell:AppsFolder).</summary>
internal static class ShellItems
{
    public static unsafe IShellItem? Create(string parsingName)
    {
        var iid = typeof(IShellItem).GUID;
        fixed (char* name = parsingName)
        {
            var hr = PInvoke.SHCreateItemFromParsingName(new Windows.Win32.Foundation.PCWSTR(name), null, &iid, out var obj);
            return hr.Succeeded ? obj as IShellItem : null;
        }
    }

    public static unsafe string? GetDisplayName(string parsingName)
    {
        var item = Create(parsingName);
        if (item is null)
            return null;
        try
        {
            item.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY, out var name);
            try { return name.ToString(); }
            finally { PInvoke.CoTaskMemFree(name.Value); }
        }
        finally
        {
            Marshal.ReleaseComObject(item);
        }
    }
}
