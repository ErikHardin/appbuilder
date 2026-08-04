namespace EpicCameraScanner;

internal static class Program
{
    private static Mutex? _singleInstance;

    [STAThread]
    private static void Main()
    {
        _singleInstance = new Mutex(true, "Local\\EpicCameraScanner.SingleInstance", out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show("Epic Camera Scanner is already running in the system tray.", "Epic Camera Scanner", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        try { Application.Run(new ScannerContext()); }
        catch (Exception ex)
        {
            MessageBox.Show($"Epic Camera Scanner could not start.\n\n{ex.Message}", "Epic Camera Scanner", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { _singleInstance.ReleaseMutex(); _singleInstance.Dispose(); }
    }
}
