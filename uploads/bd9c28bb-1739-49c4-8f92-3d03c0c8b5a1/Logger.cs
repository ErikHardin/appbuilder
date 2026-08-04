namespace EpicCameraScanner;

internal static class Logger
{
    public static void Write(AppSettings settings, string message)
    {
        if (!settings.EnableLogging) return;
        try
        {
            Directory.CreateDirectory(AppSettings.SettingsDirectory);
            File.AppendAllText(AppSettings.LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch { }
    }
}
