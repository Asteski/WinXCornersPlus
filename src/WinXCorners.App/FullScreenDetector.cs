using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WinXCorners.App;

internal static class FullScreenDetector
{
    internal static bool ShouldBlockAction(string actionId)
    {
        if (!IsFullScreenForegroundWindow())
        {
            return false;
        }

        return !string.Equals(actionId, "all-windows", StringComparison.Ordinal) || !IsTaskViewForegroundWindow();
    }

    internal static bool IsTaskViewForegroundWindow()
    {
        var handle = GetForegroundWindow();
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        var className = GetClassName(handle);
        return string.Equals(className, "XamlExplorerHostIslandWindow", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(className, "Windows.UI.Core.CoreWindow", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFullScreenForegroundWindow()
    {
        var handle = GetForegroundWindow();
        if (handle == IntPtr.Zero || handle == GetShellWindow() || !IsWindow(handle))
        {
            return false;
        }

        if (!IsWindowVisible(handle) || IsIconic(handle))
        {
            return false;
        }

        var className = GetClassName(handle);
        if (IsIgnoredForegroundClass(className))
        {
            return false;
        }

        if (IsExplorerProcessWindow(handle))
        {
            return false;
        }

        if (FindWindowEx(handle, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
        {
            return false;
        }

        var style = GetWindowLongPtr(handle, GwlStyle).ToInt64();
        if ((style & WsCaption) == WsCaption)
        {
            return false;
        }

        var exStyle = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        if ((exStyle & WsExTransparent) == WsExTransparent)
        {
            return false;
        }

        if (!GetWindowRect(handle, out var rect))
        {
            return false;
        }

        var placement = new WINDOWPLACEMENT();
        placement.length = Marshal.SizeOf<WINDOWPLACEMENT>();
        if (!GetWindowPlacement(handle, ref placement))
        {
            return false;
        }

        var screen = Screen.FromHandle(handle);
        var bounds = screen.Bounds;
        var sameSize = rect.Right - rect.Left == bounds.Width && rect.Bottom - rect.Top == bounds.Height;

        if (placement.showCmd == SwShowMaximized)
        {
            return sameSize;
        }

        return sameSize;
    }

    private static bool IsIgnoredForegroundClass(string className)
    {
        return string.Equals(className, "Progman", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(className, "WorkerW", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(className, "Shell_TrayWnd", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(className, "Shell_SecondaryTrayWnd", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(className, "Windows.UI.Core.CoreWindow", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(className, "XamlExplorerHostIslandWindow", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExplorerProcessWindow(IntPtr handle)
    {
        _ = GetWindowThreadProcessId(handle, out var pid);
        if (pid == 0)
        {
            return false;
        }

        try
        {
            using var process = Process.GetProcessById((int)pid);
            return string.Equals(process.ProcessName, "explorer", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string GetClassName(IntPtr handle)
    {
        var builder = new StringBuilder(256);
        return GetClassName(handle, builder, builder.Capacity) > 0 ? builder.ToString() : string.Empty;
    }

    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const int SwShowMaximized = 3;
    private const long WsCaption = 0x00C00000L;
    private const long WsExTransparent = 0x00000020L;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string? className, string? windowTitle);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        internal int X;
        internal int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPLACEMENT
    {
        internal int length;
        internal int flags;
        internal int showCmd;
        internal POINT ptMinPosition;
        internal POINT ptMaxPosition;
        internal RECT rcNormalPosition;
    }
}