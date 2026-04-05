namespace WinXCorners.App;

internal enum FlyoutAnimationDirection
{
    Top,
    Bottom,
    Left,
    Right
}

internal sealed class ApplicationSettings
{
    public bool HotCornersEnabled { get; set; } = true;

    public string TopLeftActionId { get; set; } = "file-explorer";

    public string TopRightActionId { get; set; } = "hide-other-windows";

    public string BottomLeftActionId { get; set; } = "all-windows";

    public string BottomRightActionId { get; set; } = "action-center";

    public bool StartWithWindows { get; set; }

    public bool IgnoreFullScreen { get; set; } = true;

    public bool AlwaysRunAsAdministrator { get; set; }

    public bool AlwaysHideTrayIcon { get; set; }

    public FlyoutAnimationDirection FlyoutAnimationDirection { get; set; } = FlyoutAnimationDirection.Bottom;

    public bool GlobalDelayEnabled { get; set; }

    public int GlobalDelayIndex { get; set; } = 3;

    public bool TopLeftDelayEnabled { get; set; }

    public int TopLeftDelayIndex { get; set; } = 3;

    public bool TopRightDelayEnabled { get; set; }

    public int TopRightDelayIndex { get; set; } = 3;

    public bool BottomLeftDelayEnabled { get; set; }

    public int BottomLeftDelayIndex { get; set; } = 3;

    public bool BottomRightDelayEnabled { get; set; }

    public int BottomRightDelayIndex { get; set; } = 3;

    public bool ShowCountdown { get; set; }

    public bool EnableCustomCommands { get; set; }

    public CustomCommandSettings[] CustomCommands { get; set; } = CreateDefaultCommands();

    public enum ModifierKey
    {
        None,
        Ctrl,
        Shift,
        Alt
    }

    public ModifierKey HotCornerModifierKey { get; set; } = ModifierKey.None;

    internal ApplicationSettings Clone()
    {
        return new ApplicationSettings
        {
            HotCornersEnabled = HotCornersEnabled,
            TopLeftActionId = TopLeftActionId,
            TopRightActionId = TopRightActionId,
            BottomLeftActionId = BottomLeftActionId,
            BottomRightActionId = BottomRightActionId,
            StartWithWindows = StartWithWindows,
            IgnoreFullScreen = IgnoreFullScreen,
            AlwaysRunAsAdministrator = AlwaysRunAsAdministrator,
            AlwaysHideTrayIcon = AlwaysHideTrayIcon,
            FlyoutAnimationDirection = FlyoutAnimationDirection,
            GlobalDelayEnabled = GlobalDelayEnabled,
            GlobalDelayIndex = GlobalDelayIndex,
            TopLeftDelayEnabled = TopLeftDelayEnabled,
            TopLeftDelayIndex = TopLeftDelayIndex,
            TopRightDelayEnabled = TopRightDelayEnabled,
            TopRightDelayIndex = TopRightDelayIndex,
            BottomLeftDelayEnabled = BottomLeftDelayEnabled,
            BottomLeftDelayIndex = BottomLeftDelayIndex,
            BottomRightDelayEnabled = BottomRightDelayEnabled,
            BottomRightDelayIndex = BottomRightDelayIndex,
            ShowCountdown = ShowCountdown,
            EnableCustomCommands = EnableCustomCommands,
            CustomCommands = CustomCommands.Select(static command => command.Clone()).ToArray(),
            HotCornerModifierKey = HotCornerModifierKey
        };
    }

    internal static ApplicationSettings CreateDefault()
    {
        return new ApplicationSettings();
    }

    internal static CustomCommandSettings[] CreateDefaultCommands()
    {
        return Enumerable.Range(0, 4)
            .Select(static _ => new CustomCommandSettings())
            .ToArray();
    }
}

internal sealed class CustomCommandSettings
{
    public string Name { get; set; } = string.Empty;

    public string Command { get; set; } = string.Empty;

    public string Parameters { get; set; } = string.Empty;

    public bool LaunchHidden { get; set; }

    internal CustomCommandSettings Clone()
    {
        return new CustomCommandSettings
        {
            Name = Name,
            Command = Command,
            Parameters = Parameters,
            LaunchHidden = LaunchHidden
        };
    }
}
