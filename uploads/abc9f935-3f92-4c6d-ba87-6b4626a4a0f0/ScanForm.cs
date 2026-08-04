using OpenCvSharp;
using OpenCvSharp.Extensions;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace EpicCameraScanner;

internal sealed class ScanForm : Form
{
    private readonly PictureBox _preview = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 46, TextAlign = ContentAlignment.MiddleCenter };
    private readonly Label _countdown = new() { Dock = DockStyle.Top, Height = 28, TextAlign = ContentAlignment.MiddleCenter };
    private readonly Panel _successOverlay = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(185, 30, 150, 70), Visible = false };
    private readonly Label _successLabel = new() { Dock = DockStyle.Fill, Text = "SCAN SUCCESSFUL", ForeColor = Color.White, Font = new Font(SystemFonts.DefaultFont.FontFamily, 22, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
    private readonly System.Windows.Forms.Timer _frameTimer = new() { Interval = 70 };
    private readonly System.Windows.Forms.Timer _timeoutTimer = new() { Interval = 250 };
    private VideoCapture? _camera;
    private BarcodeReader? _reader;
    private bool _processing;
    private DateTime _expiresAt;
    private AppSettings _settings = new();

    public event EventHandler<string>? BarcodeScanned;
    public event EventHandler<string>? ScanFailed;
    public event EventHandler? Cancelled;

    public ScanForm()
    {
        Text = "Epic Camera Scanner"; Width = 760; Height = 560; StartPosition = FormStartPosition.CenterScreen;
        TopMost = true; KeyPreview = true; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        _successOverlay.Controls.Add(_successLabel);
        Controls.Add(_successOverlay); Controls.Add(_preview); Controls.Add(_countdown); Controls.Add(_status);
        _successOverlay.BringToFront();
        _frameTimer.Tick += CaptureFrame; _timeoutTimer.Tick += UpdateTimeout;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) { e.Handled = true; Cancelled?.Invoke(this, EventArgs.Empty); } };
        FormClosing += (_, e) => { e.Cancel = true; Cancelled?.Invoke(this, EventArgs.Empty); };
    }

    public void BeginScan(AppSettings settings)
    {
        _settings = settings; _successOverlay.Visible = false; _status.Text = "Starting camera…"; _countdown.Text = "";
        _reader = new BarcodeReader
        {
            AutoRotate = settings.AutoRotate,
            Options = new DecodingOptions { TryHarder = settings.TryHarder, TryInverted = settings.TryInverted, PossibleFormats = settings.GetBarcodeFormats().ToList() }
        };
        Show(); Activate();
        _camera?.Dispose();
        _camera = new VideoCapture(settings.CameraIndex, VideoCaptureAPIs.DSHOW);
        if (!_camera.IsOpened())
        {
            _status.Text = $"Camera {settings.CameraIndex} could not be opened.";
            ScanFailed?.Invoke(this, "Camera could not be opened. Check the selected camera and Windows camera privacy settings.");
            return;
        }
        _camera.FrameWidth = 1280; _camera.FrameHeight = 720;
        _expiresAt = DateTime.UtcNow.AddSeconds(settings.ScanTimeoutSeconds);
        _status.Text = "Hold barcode inside the camera view — Esc to cancel";
        _frameTimer.Start(); _timeoutTimer.Start();
    }

    public async Task ShowSuccessAsync()
    {
        if (!_settings.ShowSuccessOverlay) return;
        _successOverlay.Visible = true; _successOverlay.BringToFront();
        await Task.Delay(350);
    }

    public void EndScan()
    {
        _frameTimer.Stop(); _timeoutTimer.Stop(); _camera?.Release(); Hide();
        var old = _preview.Image; _preview.Image = null; old?.Dispose();
    }

    private void UpdateTimeout(object? sender, EventArgs e)
    {
        var remaining = Math.Max(0, (int)Math.Ceiling((_expiresAt - DateTime.UtcNow).TotalSeconds));
        _countdown.Text = $"Scan timeout: {remaining}s";
        if (remaining == 0) ScanFailed?.Invoke(this, "No barcode was detected before the scan timed out.");
    }

    private void CaptureFrame(object? sender, EventArgs e)
    {
        if (_processing || _camera is null || !_camera.IsOpened() || _reader is null) return;
        _processing = true;
        try
        {
            using var frame = new Mat();
            if (!_camera.Read(frame) || frame.Empty()) return;
            using var bitmap = BitmapConverter.ToBitmap(frame);
            var result = _reader.Decode(bitmap);
            var previewCopy = new Bitmap(bitmap); var old = _preview.Image; _preview.Image = previewCopy; old?.Dispose();
            if (result is null || string.IsNullOrWhiteSpace(result.Text)) return;
            _frameTimer.Stop(); _timeoutTimer.Stop();
            BarcodeScanned?.Invoke(this, result.Text);
        }
        catch (Exception ex) { _status.Text = $"Scan error: {ex.Message}"; Logger.Write(_settings, $"Scan error: {ex}"); }
        finally { _processing = false; }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _frameTimer.Dispose(); _timeoutTimer.Dispose(); _camera?.Dispose(); _preview.Image?.Dispose(); }
        base.Dispose(disposing);
    }
}
