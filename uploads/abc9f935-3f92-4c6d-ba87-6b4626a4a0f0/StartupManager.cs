using Microsoft.Win32;

namespace EpicCameraScanner;

internal static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "EpicCameraScanner";

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
        if (enabled)
            key.SetValue(ValueName, $"\"{Application.ExecutablePath}\"");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
