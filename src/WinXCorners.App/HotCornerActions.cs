using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WinXCorners.App;

internal enum HotCornerArea
{
    None,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

internal static class HotCornerActions
{
    private const int WmSysCommand = 0x0112;
    private const int ScScreenSave = 0xF140;
    private const int ScMonitorPower = 0xF170;
    private const int ScTaskList = 0xF130;

    internal static void Execute(Form owner, ApplicationSettings settings, string actionId)
    {
        switch (actionId)
        {
            case "file-explorer":
                LaunchShellTarget("explorer.exe");
                break;
            case "settings":
                LaunchShellTarget("ms-settings:");
                break;
            case "task-manager":
                LaunchShellTarget("taskmgr.exe");
                break;
            case "all-windows":
                if (FullScreenDetector.IsTaskViewForegroundWindow())
                {
                    HotkeyInvoker.Invoke("escape");
                }
                else
                {
                    HotkeyInvoker.Invoke("_win+tab");
                }

                break;
            case "desktop":
                HotkeyInvoker.Invoke("_win+d");
                break;
            case "screen-saver":
                SendMessage(owner.Handle, WmSysCommand, (IntPtr)ScScreenSave, IntPtr.Zero);
                break;
            case "monitors-off":
                SendMessage(owner.Handle, WmSysCommand, (IntPtr)ScMonitorPower, (IntPtr)2);
                break;
            case "action-center":
                HotkeyInvoker.Invoke("_win+a");
                break;
            case "notification-center":
                HotkeyInvoker.Invoke("_win+n");
                break;
            case "lock-screen":
                LockWorkStation();
                break;
            case "start-menu":
                SendMessage(owner.Handle, WmSysCommand, (IntPtr)ScTaskList, IntPtr.Zero);
                break;
            case "hide-other-windows":
                HotkeyInvoker.Invoke("_win+home");
                break;
            case "custom-1":
                RunCustomCommand(settings, 0);
                break;
            case "custom-2":
                RunCustomCommand(settings, 1);
                break;
            case "custom-3":
                RunCustomCommand(settings, 2);
                break;
            case "custom-4":
                RunCustomCommand(settings, 3);
                break;
        }
    }

    internal static TimeSpan GetDelay(ApplicationSettings settings, HotCornerArea area)
    {
        if (settings.GlobalDelayEnabled)
        {
            return GetDelayFromIndex(settings.GlobalDelayIndex);
        }

        return area switch
        {
            HotCornerArea.TopLeft when settings.TopLeftDelayEnabled => GetDelayFromIndex(settings.TopLeftDelayIndex),
            HotCornerArea.TopRight when settings.TopRightDelayEnabled => GetDelayFromIndex(settings.TopRightDelayIndex),
            HotCornerArea.BottomLeft when settings.BottomLeftDelayEnabled => GetDelayFromIndex(settings.BottomLeftDelayIndex),
            HotCornerArea.BottomRight when settings.BottomRightDelayEnabled => GetDelayFromIndex(settings.BottomRightDelayIndex),
            _ => TimeSpan.Zero
        };
    }

    private static TimeSpan GetDelayFromIndex(int index)
    {
        var seconds = index switch
        {
            0 => 0.25,
            1 => 0.50,
            2 => 0.75,
            3 => 1.00,
            4 => 1.25,
            5 => 1.50,
            _ => 1.00
        };

        return TimeSpan.FromSeconds(seconds);
    }

    private static void RunCustomCommand(ApplicationSettings settings, int index)
    {
        if (!settings.EnableCustomCommands)
        {
            AppLogger.Log($"Custom command {index + 1} skipped because custom commands are disabled");
            return;
        }

        var customCommand = settings.CustomCommands[index];
        if (string.IsNullOrWhiteSpace(customCommand.Command))
        {
            AppLogger.Log($"Custom command {index + 1} skipped because command text is empty");
            return;
        }

        try
        {
            var command = customCommand.Command.Trim();
            if (command.StartsWith('!'))
            {
                AppLogger.Log($"Executing custom hotkey command {index + 1}: {command}");
                HotkeyInvoker.Invoke(command[1..]);
                return;
            }

            if (command.StartsWith('#') || command.StartsWith('@'))
            {
                AppLogger.Log($"Executing conditional custom command {index + 1}: {command}");
                ConditionalHotkeyRunner.Execute(command);
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = customCommand.Parameters,
                UseShellExecute = true,
                WindowStyle = customCommand.LaunchHidden ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal
            };

            AppLogger.Log($"Launching custom command {index + 1}: {command} {customCommand.Parameters}".Trim());
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            AppLogger.Log($"Custom command {index + 1} failed: {ex.Message}");
        }
    }

    private static void LaunchShellTarget(string fileName, string arguments = "")
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            AppLogger.Log($"Failed to launch '{fileName} {arguments}'. {ex.Message}".Trim());
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool LockWorkStation();
}
