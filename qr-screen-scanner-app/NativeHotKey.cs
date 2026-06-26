using System.Runtime.InteropServices;

namespace qr_screen_scanner_app;

[Flags]
internal enum GlobalHotKeyModifiers : uint
{
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Windows = 0x0008,
    NoRepeat = 0x4000
}

internal static class NativeHotKey
{
    internal const int WmHotKey = 0x0312;

    internal static bool RegisterHotKey(IntPtr windowHandle, int id, GlobalHotKeyModifiers modifiers, Keys key)
    {
        return RegisterHotKey(windowHandle, id, (uint)modifiers, (uint)key);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
