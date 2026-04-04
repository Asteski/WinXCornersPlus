using System.Runtime.InteropServices;

namespace WinXCorners.App;

internal static class Win32PopupMenu
{
    internal const int TrayCommandToggleHotCorners = 1001;
    internal const int TrayCommandReload = 1000;
    internal const int TrayCommandHideTray = 1002;
    internal const int TrayCommandElevate = 1003;
    internal const int TrayCommandAdvanced = 1004;
    internal const int TrayCommandToggleIgnoreFullscreen = 1005;
    internal const int TrayCommandLogWindow = 1007;
    internal const int TrayCommandAbout = 1008;
    internal const int TrayCommandExit = 1009;

    private const uint MF_STRING = 0x00000000;
    private const uint MF_GRAYED = 0x00000001;
    private const uint MF_DISABLED = 0x00000002;
    private const uint MF_SEPARATOR = 0x00000800;
    private const uint TPM_LEFTALIGN = 0x0000;
    private const uint TPM_TOPALIGN = 0x0000;
    private const uint TPM_RETURNCMD = 0x0100;
    private const uint TPM_RIGHTBUTTON = 0x0002;

    internal static int ShowTrayMenu(IntPtr ownerHandle, Point screenPoint, bool hotCornersEnabled, bool isElevated)
    {
        ThemeHelper.ApplyNativeWindowTheme(ownerHandle);

        var menuHandle = CreatePopupMenu();
        if (menuHandle == IntPtr.Zero)
            return 0;

        try
        {
            var toggleHotCornersText = hotCornersEnabled ? "Disable" : "Enable";
            var elevateText = isElevated ? "Elevated" : "Elevate";
            var elevateFlags = MF_STRING | (isElevated ? MF_DISABLED | MF_GRAYED : 0);

            AppendMenuW(menuHandle, MF_STRING, TrayCommandToggleHotCorners, toggleHotCornersText);
            AppendMenuW(menuHandle, MF_STRING, TrayCommandReload, "Reload");
            AppendMenuW(menuHandle, MF_STRING, TrayCommandHideTray, "Hide tray");
            AppendMenuW(menuHandle, elevateFlags, TrayCommandElevate, elevateText);
            AppendMenuW(menuHandle, MF_SEPARATOR, 0, null);
            AppendMenuW(menuHandle, MF_STRING, TrayCommandAdvanced, "Settings");
            AppendMenuW(menuHandle, MF_STRING, TrayCommandLogWindow, "Log Window");
            AppendMenuW(menuHandle, MF_SEPARATOR, 0, null);
            AppendMenuW(menuHandle, MF_STRING, TrayCommandAbout, "About");
            AppendMenuW(menuHandle, MF_STRING, TrayCommandExit, "Exit");

            SetForegroundWindow(ownerHandle);
            return TrackPopupMenuEx(menuHandle, TPM_LEFTALIGN | TPM_TOPALIGN | TPM_RETURNCMD | TPM_RIGHTBUTTON, screenPoint.X, screenPoint.Y, ownerHandle, IntPtr.Zero);
        }
        finally
        {
            DestroyMenu(menuHandle);
        }
    }

    internal static string? ShowCornerMenu(IntPtr ownerHandle, Point screenPoint, IReadOnlyList<(string text, string actionId)> items)
    {
        ThemeHelper.ApplyNativeWindowTheme(ownerHandle);

        var menuHandle = CreatePopupMenu();
        if (menuHandle == IntPtr.Zero)
            return null;

        var commandToAction = new Dictionary<int, string>(items.Count);

        try
        {
            var commandId = 2000;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (string.Equals(item.actionId, "__separator__", StringComparison.Ordinal))
                {
                    AppendMenuW(menuHandle, MF_SEPARATOR, 0, null);
                    continue;
                }

                commandToAction[commandId] = item.actionId;
                AppendMenuW(menuHandle, MF_STRING, commandId, item.text);
                commandId++;
            }

            SetForegroundWindow(ownerHandle);
            var selectedId = TrackPopupMenuEx(menuHandle, TPM_LEFTALIGN | TPM_TOPALIGN | TPM_RETURNCMD | TPM_RIGHTBUTTON, screenPoint.X, screenPoint.Y, ownerHandle, IntPtr.Zero);
            return commandToAction.TryGetValue(selectedId, out var actionId) ? actionId : null;
        }
        finally
        {
            DestroyMenu(menuHandle);
        }
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool AppendMenuW(IntPtr hMenu, uint uFlags, int uIDNewItem, string? lpNewItem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int TrackPopupMenuEx(IntPtr hmenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
