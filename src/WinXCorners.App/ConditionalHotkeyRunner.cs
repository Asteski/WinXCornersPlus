using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace WinXCorners.App;

internal static partial class ConditionalHotkeyRunner
{
    internal static bool Execute(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return false;
        }

        var hotkeyMatch = HotkeyRegex().Match(expression);
        if (!hotkeyMatch.Success)
        {
            return false;
        }

        var elseMatch = ElseRegex().Match(expression);
        var hotkey = hotkeyMatch.Groups[1].Value;
        var elseHotkey = elseMatch.Success ? elseMatch.Groups[1].Value : string.Empty;
        var conditionsSegment = expression[..hotkeyMatch.Index];
        var conditions = ConditionRegex().Matches(conditionsSegment)
            .Select(match => new WindowCondition(
                match.Groups[1].Value[0],
                match.Groups[2].Value,
                match.Groups[3].Success ? match.Groups[3].Value : string.Empty))
            .ToArray();

        var matched = conditions.Any(CheckCondition);
        if (matched)
        {
            return HotkeyInvoker.Invoke(hotkey);
        }

        return !string.IsNullOrWhiteSpace(elseHotkey) && HotkeyInvoker.Invoke(elseHotkey);
    }

    private static bool CheckCondition(WindowCondition condition)
    {
        if (condition.Type == '#')
        {
            var handle = GetForegroundWindow();
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            var className = GetWindowClassName(handle);
            var title = GetWindowText(handle);
            return string.Equals(className, condition.ClassName, StringComparison.OrdinalIgnoreCase) &&
                   (string.IsNullOrEmpty(condition.TitleText) || title.Contains(condition.TitleText, StringComparison.OrdinalIgnoreCase));
        }

        return condition.TitleText.Length > 0
            ? FindWindow(condition.ClassName, condition.TitleText) != IntPtr.Zero
            : FindWindow(condition.ClassName, null) != IntPtr.Zero;
    }

    private static string GetWindowText(IntPtr handle)
    {
        var length = GetWindowTextLength(handle);
        if (length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length + 1);
        _ = GetWindowText(handle, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string GetWindowClassName(IntPtr handle)
    {
        var builder = new StringBuilder(256);
        return GetClassName(handle, builder, builder.Capacity) > 0 ? builder.ToString() : string.Empty;
    }

    [GeneratedRegex(@":\(([^)]*)\)", RegexOptions.Compiled)]
    private static partial Regex HotkeyRegex();

    [GeneratedRegex(@"\?\(([^)]*)\)", RegexOptions.Compiled)]
    private static partial Regex ElseRegex();

    [GeneratedRegex(@"([#@])\[([^,\]]*)(?:,([^\]]*))?\]", RegexOptions.Compiled)]
    private static partial Regex ConditionRegex();

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    private readonly record struct WindowCondition(char Type, string ClassName, string TitleText);
}