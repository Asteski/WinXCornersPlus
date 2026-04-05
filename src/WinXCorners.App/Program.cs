namespace WinXCorners.App;

static class Program
{
    private const string SingleInstanceMutexName = "Local\\WinXCornersPlus.SingleInstance";

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        var settings = SettingsStore.Load();
        if (settings.AlwaysRunAsAdministrator && !ElevationHelper.IsProcessElevated())
        {
            if (ElevationHelper.TryRestartElevated())
            {
                return;
            }
        }

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        ApplicationConfiguration.Initialize();

        using var singleInstanceMutex = new System.Threading.Mutex(true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            return;
        }

        var form = new Form1(settings);
        Application.ApplicationExit += (_, _) => form.Dispose();
        form.Show();
        form.Hide();
        Application.Run();
    }
}

internal static class ElevationHelper
{
    internal static bool IsProcessElevated()
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

    internal static bool TryRestartElevated()
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = Application.ExecutablePath,
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = true,
                Verb = "runas"
            };

            System.Diagnostics.Process.Start(startInfo);
            return true;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}