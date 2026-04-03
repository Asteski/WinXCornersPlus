using System.Text.Json;

namespace WinXCorners.App;

internal static class SettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "settings.json");

    internal static ApplicationSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return CreateDefaultFromSystem();
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<ApplicationSettings>(json, SerializerOptions) ?? ApplicationSettings.CreateDefault();
            Normalize(settings);
            settings.StartWithWindows = StartupRegistration.IsEnabled();
            return settings;
        }
        catch
        {
            return CreateDefaultFromSystem();
        }
    }

    internal static void Save(ApplicationSettings settings)
    {
        Normalize(settings);
        var json = JsonSerializer.Serialize(settings, SerializerOptions);
        File.WriteAllText(SettingsPath, json);
    }

    private static void Normalize(ApplicationSettings settings)
    {
        settings.TopLeftActionId = string.IsNullOrWhiteSpace(settings.TopLeftActionId) ? "file-explorer" : settings.TopLeftActionId;
        settings.TopRightActionId = string.IsNullOrWhiteSpace(settings.TopRightActionId) ? "hide-other-windows" : settings.TopRightActionId;
        settings.BottomLeftActionId = string.IsNullOrWhiteSpace(settings.BottomLeftActionId) ? "all-windows" : settings.BottomLeftActionId;
        settings.BottomRightActionId = string.IsNullOrWhiteSpace(settings.BottomRightActionId) ? "action-center" : settings.BottomRightActionId;

        if (!Enum.IsDefined(settings.FlyoutAnimationDirection))
        {
            settings.FlyoutAnimationDirection = FlyoutAnimationDirection.Bottom;
        }

        settings.CustomCommands ??= ApplicationSettings.CreateDefaultCommands();

        if (settings.CustomCommands.Length == 4)
        {
            return;
        }

        var normalized = ApplicationSettings.CreateDefaultCommands();
        for (var index = 0; index < Math.Min(settings.CustomCommands.Length, normalized.Length); index++)
        {
            normalized[index] = settings.CustomCommands[index] ?? new CustomCommandSettings();
        }

        settings.CustomCommands = normalized;
    }

    private static ApplicationSettings CreateDefaultFromSystem()
    {
        var settings = ApplicationSettings.CreateDefault();
        settings.StartWithWindows = StartupRegistration.IsEnabled();
        return settings;
    }
}
