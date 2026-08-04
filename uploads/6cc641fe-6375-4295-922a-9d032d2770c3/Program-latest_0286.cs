using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EpicBcaOverlay
{
    internal static class Program
    {
        // How far in from each edge to shrink the desktop's usable area, so icons
        // and the taskbar move clear of the border. The border itself still draws
        // flush at the true screen edge.
        private const int IconClearance = 60;

        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

        private const uint SPI_GETWORKAREA = 0x0030;
        private const uint SPI_SETWORKAREA = 0x002F;
        private const uint SPIF_SENDCHANGE = 0x02;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        private static RECT _originalWorkArea;
        private static bool _workAreaShrunk;
        private static NotifyIcon? _trayIcon;

        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ShrinkWorkArea();

            // Restoring only runs on a graceful exit (e.g. the tray icon below).
            // A Task Manager "End Task" skips these, in which case Windows resets
            // the work area back to normal automatically at the next logoff/logon.
            Application.ApplicationExit += (s, e) => RestoreWorkArea();
            AppDomain.CurrentDomain.ProcessExit += (s, e) => RestoreWorkArea();

            SetupTrayIcon();

            var overlays = new List<OverlayForm>();
            foreach (var screen in Screen.AllScreens)
            {
                var form = new OverlayForm(screen.Bounds);
                overlays.Add(form);
                form.Show();
            }

            Application.Run();
        }

        private static void ShrinkWorkArea()
        {
            RECT rect = default;
            if (!SystemParametersInfo(SPI_GETWORKAREA, 0, ref rect, 0)) return;
            _originalWorkArea = rect;

            var shrunk = new RECT
            {
                Left = rect.Left + IconClearance,
                Top = rect.Top + IconClearance,
                Right = rect.Right - IconClearance,
                Bottom = rect.Bottom - IconClearance,
            };

            if (SystemParametersInfo(SPI_SETWORKAREA, 0, ref shrunk, SPIF_SENDCHANGE))
                _workAreaShrunk = true;
        }

        private static void RestoreWorkArea()
        {
            if (!_workAreaShrunk) return;
            SystemParametersInfo(SPI_SETWORKAREA, 0, ref _originalWorkArea, SPIF_SENDCHANGE);
            _workAreaShrunk = false;
        }

        private static void SetupTrayIcon()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Exit Epic BCA Overlay", null, (s, e) => Application.Exit());

            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Text = "Epic BCA Overlay",
                Visible = true,
                ContextMenuStrip = menu,
            };
        }
    }

    public class OverlayForm : Form
    {
        // ---- Customize here ----
        private const string BorderText = "EPIC BCA - PLEASE USE IN CASE OF DOWNTIME";
        private const int BorderThickness = 10; // px, thin border
        private static readonly Color BorderPink = Color.FromArgb(255, 236, 0, 140);
        private static readonly Color TextColor = Color.White;
        private const float FontSize = 8f;
        // -------------------------

        // A color extremely unlikely to appear elsewhere on screen; used as the
        // transparency key so everything except the border/text is click-through.
        private static readonly Color TransparencyKeyColor = Color.FromArgb(255, 1, 2, 3);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_TOOLWINDOW = 0x80; // hides from Alt-Tab / taskbar

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;

        private readonly System.Windows.Forms.Timer _topmostTimer;
        private readonly Rectangle _borderRect;

        public OverlayForm(Rectangle bounds)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = TransparencyKeyColor;
            TransparencyKey = TransparencyKeyColor;
            DoubleBuffered = true;

            // Drawn flush at the true screen edge. Icons/taskbar are kept clear of
            // it by shrinking the OS-level work area in Program.Main instead.
            _borderRect = new Rectangle(0, 0, bounds.Width, bounds.Height);

            // Fullscreen apps (Epic, Citrix, browsers) can steal the topmost slot.
            // Re-assert every few seconds so the border stays visible.
            _topmostTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            _topmostTimer.Tick += (s, e) => ReassertTopMost();
            _topmostTimer.Start();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        private void ReassertTopMost()
        {
            if (IsHandleCreated)
                SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ReassertTopMost();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int t = BorderThickness;
            var rect = _borderRect;

            using (var pen = new Pen(BorderPink, t))
            {
                float offset = t / 2f;
                g.DrawRectangle(pen, rect.X + offset, rect.Y + offset, rect.Width - t, rect.Height - t);
            }

            DrawPerimeterText(g, rect, t);
        }

        private void DrawPerimeterText(Graphics g, Rectangle rect, int t)
        {
            using var font = new Font("Segoe UI", FontSize, FontStyle.Bold);
            using var brush = new SolidBrush(TextColor);
            float half = t / 2f;

            // One instance centered on each side. Top and bottom are both upright
            // (0 degrees) rather than the bottom being flipped to follow the path.
            var positions = new (float x, float y, float angle)[]
            {
                (rect.X + rect.Width / 2f, rect.Y + half, 0f),                         // top
                (rect.X + rect.Width - half, rect.Y + rect.Height / 2f, 90f),           // right
                (rect.X + rect.Width / 2f, rect.Y + rect.Height - half, 0f),            // bottom
                (rect.X + half, rect.Y + rect.Height / 2f, 270f),                       // left
            };

            foreach (var (x, y, angle) in positions)
            {
                var state = g.Save();
                g.TranslateTransform(x, y);
                g.RotateTransform(angle);
                var size = g.MeasureString(BorderText, font);
                g.DrawString(BorderText, font, brush, -size.Width / 2f, -size.Height / 2f);
                g.Restore(state);
            }
        }
    }
}
