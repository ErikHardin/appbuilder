using System.Runtime.InteropServices;

namespace EpicCameraScanner;

internal static class NativeMethods
{
    internal const int WM_HOTKEY = 0x0312;
    internal const uint MOD_ALT = 0x0001, MOD_CONTROL = 0x0002, MOD_SHIFT = 0x0004, MOD_NOREPEAT = 0x4000, VK_S = 0x53;
    internal const uint INPUT_KEYBOARD = 1, KEYEVENTF_KEYUP = 0x0002, KEYEVENTF_UNICODE = 0x0004, KEYEVENTF_SCANCODE = 0x0008;
    internal const uint MAPVK_VK_TO_VSC = 0;
    internal const ushort VK_RETURN = 0x0D, VK_TAB = 0x09, VK_SHIFT = 0x10;

    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll", SetLastError = true)] internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")] internal static extern short VkKeyScanEx(char ch, IntPtr dwhkl);
    [DllImport("user32.dll")] internal static extern IntPtr GetKeyboardLayout(uint idThread);
    [DllImport("user32.dll")] internal static extern uint MapVirtualKey(uint uCode, uint uMapType);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool DestroyIcon(IntPtr handle);

    // The native INPUT struct is { DWORD type; union { MOUSEINPUT; KEYBDINPUT; HARDWAREINPUT; } }.
    // The union must be sized to its LARGEST member (MOUSEINPUT = 32 bytes on x64), even though we
    // only ever populate the KEYBDINPUT (24 bytes) branch — otherwise Marshal.SizeOf<INPUT>() returns
    // a value smaller than what SendInput expects for cbSize, and the call fails with Win32 error 87.
    [StructLayout(LayoutKind.Sequential)] internal struct INPUT { internal uint type; internal InputUnion U; }
    [StructLayout(LayoutKind.Explicit, Size = 32)] internal struct InputUnion { [FieldOffset(0)] internal KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)] internal struct KEYBDINPUT { internal ushort wVk, wScan; internal uint dwFlags, time; internal UIntPtr dwExtraInfo; }

    internal static void SendScan(string text, SendAfterScan after, bool compatibilityMode)
    {
        var inputs = compatibilityMode ? BuildScancodeInputs(text, after) : BuildUnicodeInputs(text, after);
        var sent = SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
        if (sent != inputs.Count) throw new InvalidOperationException($"SendInput sent {sent} of {inputs.Count} events. Win32 error: {Marshal.GetLastWin32Error()}");
    }

    // Standard path: KEYEVENTF_UNICODE injects an arbitrary Unicode code point directly (as a VK_PACKET),
    // regardless of keyboard layout. Works for essentially every native Windows app.
    private static List<INPUT> BuildUnicodeInputs(string text, SendAfterScan after)
    {
        var inputs = new List<INPUT>(text.Length * 2 + 2);
        foreach (var ch in text) { inputs.Add(Key(0, ch, KEYEVENTF_UNICODE)); inputs.Add(Key(0, ch, KEYEVENTF_UNICODE | KEYEVENTF_KEYUP)); }
        var vk = after == SendAfterScan.Enter ? VK_RETURN : after == SendAfterScan.Tab ? VK_TAB : (ushort)0;
        if (vk != 0) { inputs.Add(Key(vk, 0, 0)); inputs.Add(Key(vk, 0, KEYEVENTF_KEYUP)); }
        return inputs;
    }

    // Compatibility path: VMware/RDP/Citrix keyboard redirection listens for real hardware scan codes and
    // silently drops VK_PACKET-based Unicode injection. This looks up the actual virtual key + shift state
    // for each character (US layout assumption) and sends real scancodes via KEYEVENTF_SCANCODE instead.
    private static List<INPUT> BuildScancodeInputs(string text, SendAfterScan after)
    {
        var inputs = new List<INPUT>(text.Length * 4 + 2);
        var layout = GetKeyboardLayout(0);
        var shiftScan = (ushort)MapVirtualKey(VK_SHIFT, MAPVK_VK_TO_VSC);
        foreach (var ch in text)
        {
            var vkScan = VkKeyScanEx(ch, layout);
            if (vkScan == -1)
            {
                // No mapping on this layout (e.g. an unusual symbol) — fall back to Unicode injection for just this char.
                inputs.Add(Key(0, ch, KEYEVENTF_UNICODE)); inputs.Add(Key(0, ch, KEYEVENTF_UNICODE | KEYEVENTF_KEYUP));
                continue;
            }
            var vk = (ushort)(vkScan & 0xFF);
            var needsShift = ((vkScan >> 8) & 1) != 0;
            var scan = (ushort)MapVirtualKey(vk, MAPVK_VK_TO_VSC);
            if (needsShift) inputs.Add(ScanKey(shiftScan, 0));
            inputs.Add(ScanKey(scan, 0));
            inputs.Add(ScanKey(scan, KEYEVENTF_KEYUP));
            if (needsShift) inputs.Add(ScanKey(shiftScan, KEYEVENTF_KEYUP));
        }
        var afterVk = after == SendAfterScan.Enter ? VK_RETURN : after == SendAfterScan.Tab ? VK_TAB : (ushort)0;
        if (afterVk != 0)
        {
            var scan = (ushort)MapVirtualKey(afterVk, MAPVK_VK_TO_VSC);
            inputs.Add(ScanKey(scan, 0)); inputs.Add(ScanKey(scan, KEYEVENTF_KEYUP));
        }
        return inputs;
    }

    private static INPUT Key(ushort vk, ushort scan, uint flags) => new() { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = vk, wScan = scan, dwFlags = flags } } };
    private static INPUT ScanKey(ushort scan, uint extraFlags) => new() { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = 0, wScan = scan, dwFlags = KEYEVENTF_SCANCODE | extraFlags } } };
}
