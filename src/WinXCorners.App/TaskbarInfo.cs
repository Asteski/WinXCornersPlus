using System.Runtime.InteropServices;

namespace WinXCorners.App;

internal enum TaskbarEdge
{
    Left = 0,
    Top = 1,
    Right = 2,
    Bottom = 3
}

internal readonly record struct TaskbarInfo(Rectangle Bounds, TaskbarEdge Edge)
{
    internal static TaskbarInfo? TryGetPrimaryTaskbar()
    {
        var data = new APPBARDATA
        {
            cbSize = Marshal.SizeOf<APPBARDATA>()
        };

        var result = SHAppBarMessage(ABM_GETTASKBARPOS, ref data);
        if (result == 0)
        {
            return null;
        }

        var bounds = Rectangle.FromLTRB(data.rc.left, data.rc.top, data.rc.right, data.rc.bottom);
        return new TaskbarInfo(bounds, (TaskbarEdge)data.uEdge);
    }

    private const uint ABM_GETTASKBARPOS = 0x00000005;

    [DllImport("shell32.dll")]
    private static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

    [StructLayout(LayoutKind.Sequential)]
    private struct APPBARDATA
    {
        internal int cbSize;
        internal nint hWnd;
        internal uint uCallbackMessage;
        internal uint uEdge;
        internal RECT rc;
        internal nint lParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        internal int left;
        internal int top;
        internal int right;
        internal int bottom;
    }
}