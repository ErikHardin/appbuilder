namespace EpicCameraScanner;

internal sealed class HotKeyWindow : NativeWindow, IDisposable
{
    private const int HotKeyId = 1001;
    public event EventHandler? HotKeyPressed;

    public HotKeyWindow()
    {
        CreateHandle(new CreateParams());
        if (!NativeMethods.RegisterHotKey(Handle, HotKeyId,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT | NativeMethods.MOD_NOREPEAT,
                NativeMethods.VK_S))
        {
            throw new InvalidOperationException("Could not register Ctrl+Alt+S. Another application may already use it.");
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HotKeyId)
            HotKeyPressed?.Invoke(this, EventArgs.Empty);
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        NativeMethods.UnregisterHotKey(Handle, HotKeyId);
        DestroyHandle();
    }
}
