using ZXing;

namespace EpicCameraScanner;

internal sealed class SettingsForm : Form
{
    private readonly TextBox _prefix = new();
    private readonly TextBox _suffix = new();
    private readonly ComboBox _sendAfter = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _camera = new() { Minimum = 0, Maximum = 20 };
    private readonly NumericUpDown _timeout = new() { Minimum = 3, Maximum = 120 };
    private readonly CheckBox _sound = new() { Text = "Play success sound" };
    private readonly CheckBox _overlay = new() { Text = "Show green success overlay" };
    private readonly CheckBox _logging = new() { Text = "Enable diagnostic logging (never logs barcode values)" };
    private readonly CheckBox _startup = new() { Text = "Start automatically when I sign in" };
    private readonly CheckBox _autoRotate = new() { Text = "Auto-rotate barcode images" };
    private readonly CheckBox _tryHarder = new() { Text = "Use enhanced decoding" };
    private readonly CheckBox _tryInverted = new() { Text = "Detect light-on-dark barcodes" };
    private readonly CheckedListBox _formats = new() { CheckOnClick = true, Height = 150 };
    private readonly AppSettings _settings;

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;
        Text = "Epic Camera Scanner Settings";
        Width = 540; Height = 680; StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;

        _sendAfter.Items.AddRange(Enum.GetNames<SendAfterScan>());
        foreach (var f in AppSettings.DefaultFormats()) _formats.Items.Add(f, settings.EnabledFormats.Contains(f));

        _prefix.Text = settings.Prefix; _suffix.Text = settings.Suffix;
        _sendAfter.SelectedItem = settings.SendAfterScan.ToString();
        _camera.Value = settings.CameraIndex; _timeout.Value = settings.ScanTimeoutSeconds;
        _sound.Checked = settings.PlaySuccessSound; _overlay.Checked = settings.ShowSuccessOverlay;
        _logging.Checked = settings.EnableLogging; _startup.Checked = settings.StartWithWindows;
        _autoRotate.Checked = settings.AutoRotate; _tryHarder.Checked = settings.TryHarder;
        _tryInverted.Checked = settings.TryInverted;

        var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 2, AutoScroll = true };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42)); table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        AddRow(table, "Hotkey", new Label { Text = "Ctrl+Alt+S (fixed for POC)", AutoSize = true });
        AddRow(table, "Prefix", _prefix); AddRow(table, "Suffix", _suffix); AddRow(table, "After scan", _sendAfter);
        AddRow(table, "Camera index", _camera); AddRow(table, "Timeout (seconds)", _timeout);
        AddFull(table, _startup); AddFull(table, _sound); AddFull(table, _overlay); AddFull(table, _logging);
        AddFull(table, _autoRotate); AddFull(table, _tryHarder); AddFull(table, _tryInverted);
        AddRow(table, "Barcode formats", _formats);

        var note = new Label { AutoSize = true, MaximumSize = new Size(480, 0), Text = "Camera index 0 is normally the built-in/default camera. Try 1 or 2 for another attached camera. Settings are stored in %LOCALAPPDATA%\\EpicCameraScanner\\settings.json." };
        AddFull(table, note);
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true };
        var save = new Button { Text = "Save", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        buttons.Controls.Add(save); buttons.Controls.Add(cancel); AddFull(table, buttons);
        Controls.Add(table); AcceptButton = save; CancelButton = cancel;
        save.Click += (_, _) => SaveSettings();
    }

    private void SaveSettings()
    {
        _settings.Prefix = _prefix.Text; _settings.Suffix = _suffix.Text;
        _settings.SendAfterScan = Enum.Parse<SendAfterScan>(_sendAfter.SelectedItem?.ToString() ?? "None");
        _settings.CameraIndex = (int)_camera.Value; _settings.ScanTimeoutSeconds = (int)_timeout.Value;
        _settings.PlaySuccessSound = _sound.Checked; _settings.ShowSuccessOverlay = _overlay.Checked;
        _settings.EnableLogging = _logging.Checked; _settings.StartWithWindows = _startup.Checked;
        _settings.AutoRotate = _autoRotate.Checked; _settings.TryHarder = _tryHarder.Checked; _settings.TryInverted = _tryInverted.Checked;
        _settings.EnabledFormats = _formats.CheckedItems.Cast<string>().ToList();
        if (_settings.EnabledFormats.Count == 0) _settings.EnabledFormats = AppSettings.DefaultFormats();
        _settings.Save(); StartupManager.SetEnabled(_settings.StartWithWindows);
    }

    private static void AddRow(TableLayoutPanel p, string label, Control control)
    {
        var row = p.RowCount++; p.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        p.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 8, 8) }, 0, row);
        control.Dock = DockStyle.Fill; control.Margin = new Padding(3, 5, 3, 5); p.Controls.Add(control, 1, row);
    }
    private static void AddFull(TableLayoutPanel p, Control control)
    {
        var row = p.RowCount++; p.RowStyles.Add(new RowStyle(SizeType.AutoSize)); control.Margin = new Padding(3, 7, 3, 7);
        p.Controls.Add(control, 0, row); p.SetColumnSpan(control, 2);
    }
}
