namespace EpicCameraScanner;

internal sealed class ScannerContext : ApplicationContext
{
    private readonly HotKeyWindow _hotKey;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _startupMenu;
    private ScanForm? _scanForm;
    private IntPtr _returnWindow;
    private AppSettings _settings;
    private bool _scanCompleting;

    public ScannerContext()
    {
        _settings = AppSettings.Load();
        try { StartupManager.SetEnabled(_settings.StartWithWindows); } catch { }
        _hotKey = new HotKeyWindow(); _hotKey.HotKeyPressed += (_, _) => StartScan();
        var menu = new ContextMenuStrip();
        menu.Items.Add("Scan now (Ctrl+Alt+S)", null, (_, _) => StartScan());
        menu.Items.Add("Settings…", null, (_, _) => OpenSettings());
        _startupMenu = new ToolStripMenuItem("Start with Windows") { Checked = _settings.StartWithWindows, CheckOnClick = true };
        _startupMenu.CheckedChanged += (_, _) => ToggleStartup(); menu.Items.Add(_startupMenu);
        menu.Items.Add("Open settings folder", null, (_, _) => OpenSettingsFolder());
        menu.Items.Add(new ToolStripSeparator()); menu.Items.Add("Exit", null, (_, _) => ExitThread());
        _trayIcon = new NotifyIcon { Icon = SystemIcons.Information, Text = "Epic Camera Scanner — Ctrl+Alt+S", Visible = true, ContextMenuStrip = menu };
        _trayIcon.DoubleClick += (_, _) => StartScan();
        _trayIcon.ShowBalloonTip(2500, "Epic Camera Scanner", "Running in the system tray. Press Ctrl+Alt+S to scan.", ToolTipIcon.Info);
        Logger.Write(_settings, "Application started.");
    }

    private void StartScan()
    {
        if (_scanCompleting || _scanForm is { Visible: true }) return;
        _returnWindow = NativeMethods.GetForegroundWindow();
        _scanForm ??= new ScanForm();
        _scanForm.BarcodeScanned -= OnBarcodeScanned; _scanForm.BarcodeScanned += OnBarcodeScanned;
        _scanForm.Cancelled -= OnCancelled; _scanForm.Cancelled += OnCancelled;
        _scanForm.ScanFailed -= OnScanFailed; _scanForm.ScanFailed += OnScanFailed;
        Logger.Write(_settings, $"Scan started using camera index {_settings.CameraIndex}.");
        _scanForm.BeginScan(_settings);
    }

    private async void OnBarcodeScanned(object? sender, string barcode)
    {
        if (_scanCompleting) return; _scanCompleting = true;
        try
        {
            Logger.Write(_settings, $"Barcode decoded. Length={barcode.Length}.");
            if (_settings.PlaySuccessSound) System.Media.SystemSounds.Asterisk.Play();
            if (_scanForm is not null) await _scanForm.ShowSuccessAsync();
            _scanForm?.EndScan();
            if (_returnWindow != IntPtr.Zero) { NativeMethods.SetForegroundWindow(_returnWindow); await Task.Delay(180); }
            NativeMethods.SendScan(_settings.Prefix + barcode + _settings.Suffix, _settings.SendAfterScan);
            Logger.Write(_settings, "Scan output sent successfully.");
        }
        catch (Exception ex)
        {
            Logger.Write(_settings, $"Output error: {ex}");
            MessageBox.Show($"The barcode was read, but keyboard output failed.\n\n{ex.Message}", "Epic Camera Scanner", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { _scanCompleting = false; }
    }

    private void OnScanFailed(object? sender, string message)
    {
        _scanForm?.EndScan(); Logger.Write(_settings, $"Scan failed: {message}");
        _trayIcon.ShowBalloonTip(3500, "Scan not completed", message, ToolTipIcon.Warning);
    }
    private void OnCancelled(object? sender, EventArgs e) { _scanForm?.EndScan(); Logger.Write(_settings, "Scan cancelled."); }

    private void OpenSettings()
    {
        using var form = new SettingsForm(_settings);
        if (form.ShowDialog() == DialogResult.OK) { _settings = AppSettings.Load(); _startupMenu.Checked = _settings.StartWithWindows; _trayIcon.ShowBalloonTip(2000, "Epic Camera Scanner", "Settings saved.", ToolTipIcon.Info); }
    }
    private void ToggleStartup()
    {
        _settings.StartWithWindows = _startupMenu.Checked;
        try { StartupManager.SetEnabled(_settings.StartWithWindows); _settings.Save(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Startup setting failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private static void OpenSettingsFolder()
    {
        Directory.CreateDirectory(AppSettings.SettingsDirectory);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", AppSettings.SettingsDirectory) { UseShellExecute = true });
    }

    protected override void ExitThreadCore()
    {
        Logger.Write(_settings, "Application exiting."); _scanForm?.Dispose(); _hotKey.Dispose(); _trayIcon.Visible = false; _trayIcon.Dispose(); base.ExitThreadCore();
    }
}
