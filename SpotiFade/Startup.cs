using System.Windows.Forms;
using Microsoft.Win32;

namespace SpotiFade;

/// <summary>
/// Toggles auto-start at login by writing to the per-user Run registry key
/// (HKCU\Software\Microsoft\Windows\CurrentVersion\Run). No admin required.
/// </summary>
internal static class Startup
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SpotiFade";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        return key?.GetValue(ValueName) is string v && !string.IsNullOrEmpty(v);
    }

    public static void Enable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
        if (key == null) return;
        var path = Application.ExecutablePath;
        // Quote to survive paths containing spaces.
        key.SetValue(ValueName, $"\"{path}\"");
    }

    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
