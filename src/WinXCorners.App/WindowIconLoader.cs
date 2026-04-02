namespace WinXCorners.App;

internal static class WindowIconLoader
{
    internal static Icon? TryLoadAppIcon()
    {
        return TryLoad("app.ico");
    }

    internal static Icon? TryLoadSettingsIcon()
    {
        return TryLoad("settings.ico");
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