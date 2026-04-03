namespace WinXCorners.App;

internal static class WindowIconLoader
{
    internal static Icon? TryLoadAppIcon()
    {
        return TryLoad("app.ico");
    }

    internal static Icon? TryLoadTrayIcon(bool isLightTheme, bool hotCornersEnabled)
    {
        var theme = isLightTheme ? "light" : "dark";
        var state = hotCornersEnabled ? "enabled" : "disabled";
        return TryLoad(Path.Combine("tray", $"ICON_{theme}_{state}.ico"));
    }

    internal static Icon? TryLoadAboutIcon()
    {
        return TryLoad("about.ico");
    }

    private static Icon? TryLoad(string fileName)
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, fileName);
        return File.Exists(iconPath) ? new Icon(iconPath) : null;
    }
}