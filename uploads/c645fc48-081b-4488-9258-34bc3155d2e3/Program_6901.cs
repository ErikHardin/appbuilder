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
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // One borderless overlay per monitor so it works on multi-screen setups too.
            var overlays = new List<OverlayForm>();
            foreach (var screen in Screen.AllScreens)
            {
                var form = new OverlayForm(screen.Bounds);
                overlays.Add(form);
                form.Show();
            }

            Application.Run();
        }
    }

    public class OverlayForm : Form
    {
        // ---- Customize here ----
        private const string BorderText = "EPIC BCA - PLEASE USE IN CASE OF DOWNTIME";
        private const int Repetitions = 10;
        private const int BorderThickness = 10; // px, thin border
        private static readonly Color BorderPink = Color.FromArgb(255, 236, 0, 140);
        private static readonly Color TextColor = Color.White;
        private const float FontSize = 16f;
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

            int w = ClientSize.Width;
            int h = ClientSize.Height;
            int t = BorderThickness;

            using (var pen = new Pen(BorderPink, t))
            {
                float offset = t / 2f;
                g.DrawRectangle(pen, offset, offset, w - t, h - t);
            }

            DrawPerimeterText(g, w, h, t);
        }

        private void DrawPerimeterText(Graphics g, int w, int h, int t)
        {
            double perimeter = 2 * (w + h);
            double spacing = perimeter / Repetitions;

            using var font = new Font("Segoe UI", FontSize, FontStyle.Bold);
            using var brush = new SolidBrush(TextColor);

            for (int i = 0; i < Repetitions; i++)
            {
                double s = i * spacing;
                var (x, y, angle) = PointOnPerimeter(s, w, h, t);

                var state = g.Save();
                g.TranslateTransform(x, y);
                g.RotateTransform(angle);
                var size = g.MeasureString(BorderText, font);
                g.DrawString(BorderText, font, brush, -size.Width / 2f, -size.Height / 2f);
                g.Restore(state);
            }
        }

        // Walks clockwise around the border centerline starting at top-left.
        // Returns the point and the rotation angle so text stays upright on each edge.
        private (float x, float y, float angle) PointOnPerimeter(double s, int w, int h, int t)
        {
            float half = t / 2f;
            double top = w;
            double right = h;
            double bottom = w;

            if (s < top)
                return ((float)s, half, 0f);
            s -= top;
            if (s < right)
                return (w - half, (float)s, 90f);
            s -= right;
            if (s < bottom)
                return (w - (float)s, h - half, 180f);
            s -= bottom;
            return (half, h - (float)s, 270f);
        }
    }
}
