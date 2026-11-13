using System.IO;
using System.Security.Principal;
using BetterWindows.Core;
using BetterWindows.Logic.Messaging;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Security.Authentication.Identity;

namespace BetterWindows.Services;

/// <summary>Jméno a obrázek účtu Windows (pro horní lištu a zamykací obrazovku).</summary>
internal static class UserService
{
    public static UserStatePayload Load()
    {
        var name = Safe.Run("Uživatel – jméno", DisplayName) ?? Environment.UserName;
        var avatar = Safe.Run("Uživatel – obrázek", Avatar);
        return new UserStatePayload(name, avatar);
    }

    /// <summary>Celé jméno (u účtu Microsoft / domény), jinak přihlašovací jméno.</summary>
    private static string? DisplayName()
    {
        Span<char> buffer = stackalloc char[256];
        uint size = (uint)buffer.Length;
        if (PInvoke.GetUserNameEx(EXTENDED_NAME_FORMAT.NameDisplay, buffer, ref size) && size > 0)
            return new string(buffer[..(int)size]);
        return null;
    }

    /// <summary>
    /// Obrázek účtu: Windows ukládá cesty v HKLM\...\AccountPicture\Users\{SID} (Image192, Image96…).
    /// </summary>
    private static string? Avatar()
    {
        var sid = WindowsIdentity.GetCurrent().User?.Value;
        if (sid is null)
            return null;
        using var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\AccountPicture\Users\{sid}");
        foreach (var value in new[] { "Image192", "Image240", "Image96", "Image448" })
        {
            if (key?.GetValue(value) is string path && File.Exists(path) && new FileInfo(path).Length < 2_000_000)
            {
                var mime = Path.GetExtension(path).ToLowerInvariant() == ".png" ? "image/png" : "image/jpeg";
                return $"data:{mime};base64,{Convert.ToBase64String(File.ReadAllBytes(path))}";
            }
        }
        return null;
    }
}
