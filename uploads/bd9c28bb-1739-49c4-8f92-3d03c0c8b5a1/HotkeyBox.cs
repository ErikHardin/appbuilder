namespace EpicCameraScanner;

// A read-only text box that captures a key combination when focused, instead of accepting typed text.
// Click into it, then press the desired combo (must include Ctrl, Alt, and/or Shift). Backspace/Delete clears it.
internal sealed class HotkeyBox : TextBox
{
    public uint Modifiers { get; private set; }
    public uint VirtualKey { get; private set; }

    public HotkeyBox()
    {
        ReadOnly = true;
        Cursor = Cursors.Hand;
        Text = "Click here, then press a key combo";
    }

    public void SetHotkey(uint modifiers, uint vk)
    {
        Modifiers = modifiers; VirtualKey = vk;
        Text = HotkeyFormat.Describe(modifiers, vk);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        e.SuppressKeyPress = true; e.Handled = true;

        if (e.KeyCode is Keys.ControlKey or Keys.Menu or Keys.ShiftKey or Keys.LWin or Keys.RWin) return;

        if (e.KeyCode is Keys.Back or Keys.Delete)
        {
            Modifiers = 0; VirtualKey = 0; Text = "Click here, then press a key combo";
            return;
        }

        uint mods = 0;
        if (e.Control) mods |= NativeMethods.MOD_CONTROL;
        if (e.Alt) mods |= NativeMethods.MOD_ALT;
        if (e.Shift) mods |= NativeMethods.MOD_SHIFT;

        if (mods == 0)
        {
            Text = "Hold Ctrl, Alt, and/or Shift while pressing a key";
            return;
        }

        Modifiers = mods; VirtualKey = (uint)e.KeyCode;
        Text = HotkeyFormat.Describe(Modifiers, VirtualKey);
    }

    protected override bool IsInputKey(Keys keyData) => true;
}
