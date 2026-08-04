namespace EpicCameraScanner;

// Shared formatter so the hotkey picker, the tray icon tooltip, and the tray menu
// all describe the current hotkey combination identically.
internal static class HotkeyFormat
{
    public static string Describe(uint modifiers, uint vk)
    {
        if (vk == 0) return "(not set)";
        var parts = new List<string>();
        if ((modifiers & NativeMethods.MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((modifiers & NativeMethods.MOD_ALT) != 0) parts.Add("Alt");
        if ((modifiers & NativeMethods.MOD_SHIFT) != 0) parts.Add("Shift");
        parts.Add(new KeysConverter().ConvertToString((Keys)vk) ?? vk.ToString());
        return string.Join(" + ", parts);
    }
}
