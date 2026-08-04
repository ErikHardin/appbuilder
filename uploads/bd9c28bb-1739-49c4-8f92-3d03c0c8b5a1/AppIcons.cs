using System.Drawing.Drawing2D;

namespace EpicCameraScanner;

// A handle bundling an Icon with the raw HICON it was built from, since Icon.FromHandle doesn't
// take ownership of the handle — the caller has to destroy it explicitly to avoid a GDI leak.
internal sealed class TrayIconHandle : IDisposable
{
    public Icon Icon { get; }
    private readonly IntPtr _hIcon;
    public TrayIconHandle(Icon icon, IntPtr hIcon) { Icon = icon; _hIcon = hIcon; }
    public void Dispose() { Icon.Dispose(); NativeMethods.DestroyIcon(_hIcon); }
}

internal static class AppIcons
{
    // Draws a small barcode glyph at runtime rather than shipping a bundled .ico resource,
    // so the appbuilder pipeline (which only needs .cs/.csproj) doesn't need an extra asset file.
    public static TrayIconHandle CreateBarcodeIcon(int size = 32)
    {
        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.None;
            g.Clear(Color.Transparent);

            using var bg = new SolidBrush(Color.FromArgb(255, 13, 18, 25));
            var bgRect = new Rectangle(0, 0, size, size);
            using (var path = RoundedRect(bgRect, size * 0.22f)) g.FillPath(bg, path);

            using var bar = new SolidBrush(Color.FromArgb(255, 46, 204, 113));
            float margin = size * 0.16f, top = size * 0.24f, bottom = size * 0.76f;
            float[] widths = { 2f, 1f, 3f, 1f, 1f, 2f, 1f, 3f };
            float x = margin, gap = size * 0.045f;
            float scale = size / 32f;
            foreach (var w in widths)
            {
                var bw = w * scale;
                if (x + bw > size - margin) break;
                g.FillRectangle(bar, x, top, bw, bottom - top);
                x += bw + gap;
            }
        }
        var hIcon = bmp.GetHicon();
        return new TrayIconHandle(Icon.FromHandle(hIcon), hIcon);
    }

    private static GraphicsPath RoundedRect(Rectangle r, float radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
