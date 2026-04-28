using System.Runtime.InteropServices;

namespace LiveWallpaperStudio.Engine.DesktopHost;

internal static partial class NativeMethods
{
    internal const int GwlStyle = -16;
    internal const int GwlExStyle = -20;
    internal const int SwpNoMove = 0x0002;
    internal const int SwpNoSize = 0x0001;
    internal const int SwpNoZOrder = 0x0004;
    internal const int SwpNoActivate = 0x0010;
    internal const int SwpFrameChanged = 0x0020;
    internal const int SwpShowWindow = 0x0040;
    internal const int WsCaption = 0x00C00000;
    internal const int WsThickFrame = 0x00040000;
    internal const int WsSysMenu = 0x00080000;
    internal const int WsMinimizeBox = 0x00020000;
    internal const int WsMaximizeBox = 0x00010000;
    internal const int WsChild = 0x40000000;
    internal const int WsPopup = unchecked((int)0x80000000);
    internal const int WsExAppWindow = 0x00040000;
    internal const int WsExToolWindow = 0x00000080;
    internal const int SmXVirtualScreen = 76;
    internal const int SmYVirtualScreen = 77;
    internal const int SmCxVirtualScreen = 78;
    internal const int SmCyVirtualScreen = 79;
    internal static readonly IntPtr HwndTop = new(0);
    internal static readonly IntPtr HwndBottom = new(1);

    internal delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint msg,
        IntPtr wParam,
        IntPtr lParam,
        uint fuFlags,
        uint uTimeout,
        out IntPtr lpdwResult);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    internal static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    internal static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll")]
    internal static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindow(IntPtr hWnd);

    internal static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64(hWnd, nIndex)
            : new IntPtr(GetWindowLong(hWnd, nIndex));
    }

    internal static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
            : new IntPtr(SetWindowLong(hWnd, nIndex, dwNewLong.ToInt32()));
    }
}
