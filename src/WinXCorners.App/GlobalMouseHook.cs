using System.Runtime.InteropServices;

namespace WinXCorners.App;

internal sealed class GlobalMouseHook : IDisposable
{
    private const int WhMouseLl = 14;
    private const int WmMouseMove = 0x0200;

    private readonly HookProc _hookProc;
    private IntPtr _hookHandle;

    internal event Action<Point>? MouseMoved;

    internal GlobalMouseHook()
    {
        _hookProc = HandleHook;
        _hookHandle = SetWindowsHookEx(WhMouseLl, _hookProc, IntPtr.Zero, 0);
    }

    public void Dispose()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            _ = UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }

        GC.SuppressFinalize(this);
    }

    ~GlobalMouseHook()
    {
        Dispose();
    }

    private IntPtr HandleHook(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0 && wParam == (IntPtr)WmMouseMove && lParam != IntPtr.Zero)
        {
            var data = Marshal.PtrToStructure<MsLlHookStruct>(lParam);
            MouseMoved?.Invoke(new Point(data.pt.x, data.pt.y));
        }

        return CallNextHookEx(_hookHandle, code, wParam, lParam);
    }

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct PointStruct
    {
        internal int x;
        internal int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MsLlHookStruct
    {
        internal PointStruct pt;
        internal uint mouseData;
        internal uint flags;
        internal uint time;
        internal IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
}