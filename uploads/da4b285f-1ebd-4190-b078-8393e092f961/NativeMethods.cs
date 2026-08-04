using System.Runtime.InteropServices;

namespace EpicCameraScanner;

internal static class NativeMethods
{
    internal const int WM_HOTKEY = 0x0312;
    internal const uint MOD_ALT = 0x0001, MOD_CONTROL = 0x0002, MOD_NOREPEAT = 0x4000, VK_S = 0x53;
    internal const uint INPUT_KEYBOARD = 1, KEYEVENTF_KEYUP = 0x0002, KEYEVENTF_UNICODE = 0x0004;
    internal const ushort VK_RETURN = 0x0D, VK_TAB = 0x09;

    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll", SetLastError = true)] internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)] internal struct INPUT { internal uint type; internal InputUnion U; }
    [StructLayout(LayoutKind.Explicit)] internal struct InputUnion { [FieldOffset(0)] internal KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)] internal struct KEYBDINPUT { internal ushort wVk, wScan; internal uint dwFlags, time; internal UIntPtr dwExtraInfo; }

    internal static void SendScan(string text, SendAfterScan after)
    {
        var inputs = new List<INPUT>(text.Length * 2 + 2);
        foreach (var ch in text)
        {
            inputs.Add(Key(0, ch, KEYEVENTF_UNICODE)); inputs.Add(Key(0, ch, KEYEVENTF_UNICODE | KEYEVENTF_KEYUP));
        }
        var vk = after == SendAfterScan.Enter ? VK_RETURN : after == SendAfterScan.Tab ? VK_TAB : (ushort)0;
        if (vk != 0) { inputs.Add(Key(vk, 0, 0)); inputs.Add(Key(vk, 0, KEYEVENTF_KEYUP)); }
        var sent = SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
        if (sent != inputs.Count) throw new InvalidOperationException($"SendInput sent {sent} of {inputs.Count} events. Win32 error: {Marshal.GetLastWin32Error()}");
    }
    private static INPUT Key(ushort vk, ushort scan, uint flags) => new() { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = vk, wScan = scan, dwFlags = flags } } };
}
