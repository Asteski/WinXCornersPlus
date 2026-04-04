namespace WinXCorners.App;

internal static class WindowIconLoader
{
    private const string ResourcePrefix = "Icons.";

    internal static Icon? TryLoadAppIcon()
    {
        return TryLoadEmbedded("app.ico") ?? TryLoadExecutableIcon();
    }

    internal static Icon? TryLoadTrayIcon(bool isLightTheme, bool hotCornersEnabled)
    {
        var theme = isLightTheme ? "light" : "dark";
        var state = hotCornersEnabled ? "enabled" : "disabled";
        return TryLoadEmbedded($"tray.ICON_{theme}_{state}.ico");
    }

    internal static Icon? TryLoadAboutIcon()
    {
        return TryLoadEmbedded("about.ico") ?? TryLoadAppIcon();
    }

    internal static Icon LoadFallbackTrayIcon()
    {
        return TryLoadEmbedded("WinXCornersPlus.ico") ?? TryLoadAppIcon() ?? SystemIcons.Application;
    }

    private static Icon? TryLoadEmbedded(string resourceName)
    {
        using var resourceStream = typeof(WindowIconLoader).Assembly.GetManifestResourceStream(ResourcePrefix + resourceName);
        if (resourceStream is null)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        resourceStream.CopyTo(buffer);
        buffer.Position = 0;

        using var icon = new Icon(buffer);
        return (Icon)icon.Clone();
    }

    private static Icon? TryLoadExecutableIcon()
    {
        try
        {
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch
        {
            return null;
        }
    }
}