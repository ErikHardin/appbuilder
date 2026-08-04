using ZXing;

namespace EpicCameraScanner;

internal sealed class SettingsForm : Form
{
    // Dark palette matching the rest of ECH Technical Solutions' tooling.
    private static readonly Color BgColor = Color.FromArgb(10, 14, 20);
    private static readonly Color PanelColor = Color.FromArgb(16, 22, 31);
    private static readonly Color SidebarColor = Color.FromArgb(13, 18, 25);
    private static readonly Color BorderColor = Color.FromArgb(30, 39, 51);
    private static readonly Color TextColor = Color.FromArgb(215, 224, 234);
    private static readonly Color DimColor = Color.FromArgb(107, 118, 136);
    private static readonly Color AccentColor = Color.FromArgb(46, 204, 113);
    private static readonly Font UiFont = new("Segoe UI", 9f);
    private static readonly Font MonoFont = new("Consolas", 9f);

    private readonly TextBox _prefix = new();
    private readonly TextBox _suffix = new();
    private readonly ComboBox _sendAfter = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _camera = new() { Minimum = 0, Maximum = 20 };
    private readonly NumericUpDown _timeout = new() { Minimum = 3, Maximum = 120 };
    private readonly HotkeyBox _hotkey = new();
    private readonly CheckBox _sound = new() { Text = "Play success sound" };
    private readonly CheckBox _overlay = new() { Text = "Show green success overlay" };
    private readonly CheckBox _logging = new() { Text = "Enable diagnostic logging" };
    private readonly CheckBox _startup = new() { Text = "Start automatically when I sign in" };
    private readonly CheckBox _autoRotate = new() { Text = "Auto-rotate barcode images" };
    private readonly CheckBox _tryHarder = new() { Text = "Use enhanced decoding" };
    private readonly CheckBox _tryInverted = new() { Text = "Detect light-on-dark barcodes" };
    private readonly CheckBox _compatibilityMode = new() { Text = "Compatibility mode (VMware / Remote Desktop)" };
    private readonly CheckedListBox _formats = new() { CheckOnClick = true, Height = 150 };
    private readonly ToolTip _toolTip = new() { AutoPopDelay = 15000, InitialDelay = 250, ReshowDelay = 100, ShowAlways = true };
    private readonly AppSettings _settings;
    private readonly Panel _content = new() { Dock = DockStyle.Fill };
    private readonly List<(Button Nav, Panel Page)> _sections = new();
    private readonly FlowLayoutPanel _navPanel = new() { Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Padding = new Padding(0, 8, 0, 0) };

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;
        Text = "Epic Camera Scanner Settings";
        ClientSize = new Size(760, 560);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        BackColor = BgColor; ForeColor = TextColor; Font = UiFont;

        _sendAfter.Items.AddRange(Enum.GetNames<SendAfterScan>());
        foreach (var f in AppSettings.DefaultFormats()) _formats.Items.Add(f, settings.EnabledFormats.Contains(f));

        _prefix.Text = settings.Prefix; _suffix.Text = settings.Suffix;
        _sendAfter.SelectedItem = settings.SendAfterScan.ToString();
        _camera.Value = settings.CameraIndex; _timeout.Value = settings.ScanTimeoutSeconds;
        _hotkey.SetHotkey(settings.HotkeyModifiers, settings.HotkeyVirtualKey);
        _sound.Checked = settings.PlaySuccessSound; _overlay.Checked = settings.ShowSuccessOverlay;
        _logging.Checked = settings.EnableLogging; _startup.Checked = settings.StartWithWindows;
        _autoRotate.Checked = settings.AutoRotate; _tryHarder.Checked = settings.TryHarder;
        _tryInverted.Checked = settings.TryInverted; _compatibilityMode.Checked = settings.CompatibilityMode;

        Style(_prefix); Style(_suffix); Style(_sendAfter); Style(_camera); Style(_timeout); Style(_formats);
        _hotkey.Font = MonoFont; _hotkey.BackColor = PanelColor; _hotkey.ForeColor = AccentColor; _hotkey.BorderStyle = BorderStyle.FixedSingle;
        foreach (var cb in new[] { _sound, _overlay, _logging, _startup, _autoRotate, _tryHarder, _tryInverted, _compatibilityMode })
        { cb.ForeColor = TextColor; cb.BackColor = BgColor; cb.FlatStyle = FlatStyle.Flat; }

        var sidebar = new Panel { Dock = DockStyle.Left, Width = 168, BackColor = SidebarColor, Padding = new Padding(0, 16, 0, 0) };
        var title = new Label { Text = "SETTINGS", ForeColor = DimColor, Font = new Font("Segoe UI", 8f, FontStyle.Bold), AutoSize = false, Height = 28, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0), Dock = DockStyle.Top };
        sidebar.Controls.Add(_navPanel);
        sidebar.Controls.Add(title);

        var generalPage = BuildGeneralPage();
        var scanningPage = BuildScanningPage();
        var advancedPage = BuildAdvancedPage();
        AddSection("General", generalPage, isFirst: true);
        AddSection("Scanning", scanningPage);
        AddSection("Advanced", advancedPage);
        ShowSection(0);

        var footer = BuildFooter();

        Controls.Add(_content);
        Controls.Add(footer);
        Controls.Add(sidebar);
    }

    private void AddSection(string name, Panel page, bool isFirst = false)
    {
        var btn = new Button
        {
            Text = name, Width = 168, Height = 38, FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0),
            ForeColor = isFirst ? AccentColor : TextColor, BackColor = isFirst ? PanelColor : SidebarColor, Font = UiFont
        };
        btn.FlatAppearance.BorderSize = 0;
        var index = _sections.Count;
        btn.Click += (_, _) => ShowSection(index);
        _navPanel.Controls.Add(btn);
        page.Visible = false; page.Dock = DockStyle.Fill; page.BackColor = BgColor;
        _content.Controls.Add(page);
        _sections.Add((btn, page));
    }

    private void ShowSection(int index)
    {
        for (var i = 0; i < _sections.Count; i++)
        {
            var (btn, page) = _sections[i];
            page.Visible = i == index;
            btn.BackColor = i == index ? PanelColor : SidebarColor;
            btn.ForeColor = i == index ? AccentColor : TextColor;
        }
    }

    private Panel BuildGeneralPage()
    {
        var table = NewTable();
        AddInfoRow(table, "Hotkey", _hotkey, "Global shortcut that opens the scan window from anywhere. Click the box, then press your desired key combo.");
        AddInfoRow(table, "Prefix", _prefix, "Text automatically typed before the scanned barcode value.");
        AddInfoRow(table, "Suffix", _suffix, "Text automatically typed after the scanned barcode value.");
        AddInfoRow(table, "After scan", _sendAfter, "Key sent once the barcode text has been typed — useful for auto-advancing to the next field.");
        AddFullInfo(table, _startup, "Registers Epic Camera Scanner to launch automatically when you sign in to Windows.");
        var note = new Label { AutoSize = false, Height = 60, Dock = DockStyle.Top, ForeColor = DimColor, Font = new Font(UiFont.FontFamily, 8f), Text = "Settings are stored in %LOCALAPPDATA%\\EpicCameraScanner\\settings.json." };
        AddFull(table, note);
        return Wrap(table);
    }

    private Panel BuildScanningPage()
    {
        var table = NewTable();
        AddInfoRow(table, "Camera index", _camera, "Which webcam to use. 0 is normally the built-in/default camera — try 1 or 2 for another attached camera.");
        AddInfoRow(table, "Timeout (seconds)", _timeout, "How long the scan window stays open waiting for a barcode before giving up.");
        AddFullInfo(table, _autoRotate, "Tries rotated versions of the camera image, useful if barcodes aren't always held upright.");
        AddFullInfo(table, _tryHarder, "Makes the decoder try harder on difficult images. More reliable, slightly slower.");
        AddFullInfo(table, _tryInverted, "Also looks for light-colored barcodes on a dark background.");
        AddInfoRow(table, "Barcode formats", _formats, "Which barcode symbologies to scan for. Unchecking unused formats can slightly speed up decoding.");
        return Wrap(table);
    }

    private Panel BuildAdvancedPage()
    {
        var table = NewTable();
        AddFullInfo(table, _compatibilityMode, "Switch from Unicode text injection to real keystroke scancodes. Turn this on if scanning works in apps like Notepad but not into a VMware, Remote Desktop, or Citrix window — those clients often ignore synthetic Unicode input.");
        AddFullInfo(table, _sound, "Plays a short system sound when a barcode is successfully decoded.");
        AddFullInfo(table, _overlay, "Flashes a green \"SCAN SUCCESSFUL\" banner over the scan window on a successful read.");
        AddFullInfo(table, _logging, "Writes app events to a local log file for troubleshooting. Barcode values themselves are never logged.");
        return Wrap(table);
    }

    private static TableLayoutPanel NewTable()
    {
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(28, 24, 28, 16), ColumnCount = 2, AutoSize = false, BackColor = BgColor };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40)); table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        return table;
    }

    private static Panel Wrap(TableLayoutPanel table) => new() { Controls = { table }, BackColor = BgColor, AutoScroll = false };

    private Panel BuildFooter()
    {
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = PanelColor };
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Padding = new Padding(0, 12, 20, 0), BackColor = PanelColor };
        var save = new Button { Text = "Save", DialogResult = DialogResult.OK, Width = 88, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = AccentColor, ForeColor = Color.Black, Font = new Font(UiFont, FontStyle.Bold) };
        save.FlatAppearance.BorderSize = 0;
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 88, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = PanelColor, ForeColor = TextColor };
        cancel.FlatAppearance.BorderColor = BorderColor;
        buttons.Controls.Add(save); buttons.Controls.Add(cancel);
        footer.Controls.Add(buttons);
        AcceptButton = save; CancelButton = cancel;
        save.Click += (_, _) => SaveSettings();
        return footer;
    }

    private void SaveSettings()
    {
        _settings.Prefix = _prefix.Text; _settings.Suffix = _suffix.Text;
        _settings.SendAfterScan = Enum.Parse<SendAfterScan>(_sendAfter.SelectedItem?.ToString() ?? "None");
        _settings.CameraIndex = (int)_camera.Value; _settings.ScanTimeoutSeconds = (int)_timeout.Value;
        if (_hotkey.VirtualKey != 0) { _settings.HotkeyModifiers = _hotkey.Modifiers; _settings.HotkeyVirtualKey = _hotkey.VirtualKey; }
        _settings.PlaySuccessSound = _sound.Checked; _settings.ShowSuccessOverlay = _overlay.Checked;
        _settings.EnableLogging = _logging.Checked; _settings.StartWithWindows = _startup.Checked;
        _settings.AutoRotate = _autoRotate.Checked; _settings.TryHarder = _tryHarder.Checked; _settings.TryInverted = _tryInverted.Checked;
        _settings.CompatibilityMode = _compatibilityMode.Checked;
        _settings.EnabledFormats = _formats.CheckedItems.Cast<string>().ToList();
        if (_settings.EnabledFormats.Count == 0) _settings.EnabledFormats = AppSettings.DefaultFormats();
        _settings.Save(); StartupManager.SetEnabled(_settings.StartWithWindows);
    }

    private void AddInfoRow(TableLayoutPanel p, string label, Control control, string description)
    {
        var row = p.RowCount++; p.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        p.Controls.Add(BuildLabelWithInfo(label, description), 0, row);
        control.Dock = DockStyle.Fill; control.Margin = new Padding(3, 5, 3, 5); p.Controls.Add(control, 1, row);
    }

    private void AddFullInfo(TableLayoutPanel p, Control control, string description)
    {
        var row = p.RowCount++; p.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var line = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(3, 7, 3, 7), BackColor = BgColor };
        control.Margin = new Padding(0);
        line.Controls.Add(control);
        line.Controls.Add(BuildInfoIcon(description));
        p.Controls.Add(line, 0, row); p.SetColumnSpan(line, 2);
    }

    private void AddFull(TableLayoutPanel p, Control control)
    {
        var row = p.RowCount++; p.RowStyles.Add(new RowStyle(SizeType.AutoSize)); control.Margin = new Padding(3, 7, 3, 7);
        p.Controls.Add(control, 0, row); p.SetColumnSpan(control, 2);
    }

    private Control BuildLabelWithInfo(string text, string description)
    {
        var panel = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(3, 10, 0, 0), BackColor = BgColor };
        panel.Controls.Add(new Label { Text = text, AutoSize = true, ForeColor = TextColor, Margin = new Padding(0, 3, 4, 0) });
        panel.Controls.Add(BuildInfoIcon(description));
        return panel;
    }

    private Label BuildInfoIcon(string description)
    {
        var icon = new Label { Text = "\u24D8", AutoSize = true, ForeColor = DimColor, Font = new Font("Segoe UI", 9f), Cursor = Cursors.Help, Margin = new Padding(2, 2, 0, 0) };
        _toolTip.SetToolTip(icon, description);
        return icon;
    }

    private static void Style(Control c)
    {
        c.BackColor = PanelColor; c.ForeColor = TextColor;
        if (c is TextBox tb) tb.BorderStyle = BorderStyle.FixedSingle;
        if (c is CheckedListBox clb) clb.BorderStyle = BorderStyle.FixedSingle;
    }
}
