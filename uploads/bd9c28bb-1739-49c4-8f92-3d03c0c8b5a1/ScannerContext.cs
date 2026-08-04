namespace EpicCameraScanner;

internal sealed class ScannerContext : ApplicationContext
{
    private HotKeyWindow? _hotKey;
    private readonly TrayIconHandle _icon;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _scanMenuItem;
    private readonly ToolStripMenuItem _startupMenu;
    private ScanForm? _scanForm;
    private IntPtr _returnWindow;
    private AppSettings _settings;
    private bool _scanCompleting;

    public ScannerContext()
    {
        _settings = AppSettings.Load();
        try { StartupManager.SetEnabled(_settings.StartWithWindows); } catch { }

        _icon = AppIcons.CreateBarcodeIcon();

        var menu = new ContextMenuStrip();
        _scanMenuItem = new ToolStripMenuItem("Scan now", null, (_, _) => StartScan());
        menu.Items.Add(_scanMenuItem);
        menu.Items.Add("Settings…", null, (_, _) => OpenSettings());
        _startupMenu = new ToolStripMenuItem("Start with Windows") { Checked = _settings.StartWithWindows, CheckOnClick = true };
        _startupMenu.CheckedChanged += (_, _) => ToggleStartup(); menu.Items.Add(_startupMenu);
        menu.Items.Add("Open settings folder", null, (_, _) => OpenSettingsFolder());
        menu.Items.Add(new ToolStripSeparator()); menu.Items.Add("Exit", null, (_, _) => ExitThread());
        _trayIcon = new NotifyIcon { Icon = _icon.Icon, Visible = true, ContextMenuStrip = menu };
        _trayIcon.DoubleClick += (_, _) => StartScan();

        TryRegisterHotkey(_settings.HotkeyModifiers, _settings.HotkeyVirtualKey, showBalloonOnFailure: false);
        RefreshTrayText();
        _trayIcon.ShowBalloonTip(2500, "Epic Camera Scanner", $"Running in the system tray. Press {HotkeyFormat.Describe(_settings.HotkeyModifiers, _settings.HotkeyVirtualKey)} to scan.", ToolTipIcon.Info);
        Logger.Write(_settings, "Application started.");
    }

    // Tries the requested combo; on failure falls back to the original Ctrl+Alt+S default; if even
    // that fails, the app keeps running without a global hotkey (scan is still reachable from the tray).
    private void TryRegisterHotkey(uint modifiers, uint vk, bool showBalloonOnFailure)
    {
        try
        {
            if (_hotKey is null) { _hotKey = new HotKeyWindow(modifiers, vk); _hotKey.HotKeyPressed += (_, _) => StartScan(); }
            else _hotKey.Rebind(modifiers, vk);
            _settings.HotkeyModifiers = modifiers; _settings.HotkeyVirtualKey = vk;
            return;
        }
        catch (Exception ex)
        {
            Logger.Write(_settings, $"Hotkey registration failed for {HotkeyFormat.Describe(modifiers, vk)}: {ex.Message}");
        }

        var isDefault = modifiers == (NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT) && vk == NativeMethods.VK_S;
        if (!isDefault)
        {
            try
            {
                if (_hotKey is null) { _hotKey = new HotKeyWindow(NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT, NativeMethods.VK_S); _hotKey.HotKeyPressed += (_, _) => StartScan(); }
                else _hotKey.Rebind(NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT, NativeMethods.VK_S);
                _settings.HotkeyModifiers = NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT; _settings.HotkeyVirtualKey = NativeMethods.VK_S;
                if (showBalloonOnFailure) _trayIcon.ShowBalloonTip(3500, "Hotkey unavailable", $"Couldn't register that combo — reverted to {HotkeyFormat.Describe(_settings.HotkeyModifiers, _settings.HotkeyVirtualKey)}.", ToolTipIcon.Warning);
                return;
            }
            catch { }
        }

        if (showBalloonOnFailure) _trayIcon.ShowBalloonTip(3500, "Hotkey unavailable", "No global hotkey is active. Use the tray icon menu or double-click to scan.", ToolTipIcon.Warning);
    }

    private void RefreshTrayText()
    {
        var combo = HotkeyFormat.Describe(_settings.HotkeyModifiers, _settings.HotkeyVirtualKey);
        _scanMenuItem.Text = $"Scan now ({combo})";
        _trayIcon.Text = Truncate($"Epic Camera Scanner — {combo}", 63);
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    private void StartScan()
    {
        if (_scanCompleting || _scanForm is { Visible: true }) return;
        _returnWindow = NativeMethods.GetForegroundWindow();
        if (_scanForm is null) { _scanForm = new ScanForm(); _scanForm.Icon = _icon.Icon; }
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
            NativeMethods.SendScan(_settings.Prefix + barcode + _settings.Suffix, _settings.SendAfterScan, _settings.CompatibilityMode);
            Logger.Write(_settings, "Scan output sent successfully.");
        }
        catch (Exception ex)
        {
            Logger.Write(_settings, $"Output error: {ex}");
            MessageBox.Show($"The barcode was read, but keyboard output failed.\n\n{ex.Message}\n\nIf this happened while targeting a VMware/Remote Desktop window, try enabling \"Compatibility mode\" in Settings → Advanced.", "Epic Camera Scanner", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        if (form.ShowDialog() != DialogResult.OK) return;
        var previousMods = _settings.HotkeyModifiers; var previousVk = _settings.HotkeyVirtualKey;
        _settings = AppSettings.Load();
        _startupMenu.Checked = _settings.StartWithWindows;
        if (_settings.HotkeyModifiers != previousMods || _settings.HotkeyVirtualKey != previousVk)
            TryRegisterHotkey(_settings.HotkeyModifiers, _settings.HotkeyVirtualKey, showBalloonOnFailure: true);
        RefreshTrayText();
        _trayIcon.ShowBalloonTip(2000, "Epic Camera Scanner", "Settings saved.", ToolTipIcon.Info);
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
        Logger.Write(_settings, "Application exiting.");
        _scanForm?.Dispose(); _hotKey?.Dispose(); _trayIcon.Visible = false; _trayIcon.Dispose(); _icon.Dispose();
        base.ExitThreadCore();
    }
}
