namespace EpicCameraScanner;

internal sealed class HotKeyWindow : NativeWindow, IDisposable
{
    private const int HotKeyId = 1001;
    private bool _registered;
    public event EventHandler? HotKeyPressed;

    public HotKeyWindow(uint modifiers, uint vk)
    {
        CreateHandle(new CreateParams());
        Register(modifiers, vk);
    }

    // Swaps to a new combo at runtime (called after the user changes the hotkey in Settings).
    // Throws if the new combo can't be registered; caller is responsible for deciding what to fall back to.
    public void Rebind(uint modifiers, uint vk)
    {
        if (_registered) { NativeMethods.UnregisterHotKey(Handle, HotKeyId); _registered = false; }
        Register(modifiers, vk);
    }

    private void Register(uint modifiers, uint vk)
    {
        if (!NativeMethods.RegisterHotKey(Handle, HotKeyId, modifiers | NativeMethods.MOD_NOREPEAT, vk))
            throw new InvalidOperationException($"Could not register {HotkeyFormat.Describe(modifiers, vk)}. Another application may already use it.");
        _registered = true;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HotKeyId)
            HotKeyPressed?.Invoke(this, EventArgs.Empty);
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (_registered) NativeMethods.UnregisterHotKey(Handle, HotKeyId);
        DestroyHandle();
    }
}
