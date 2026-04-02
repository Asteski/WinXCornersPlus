using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace WinXCorners.App;

internal static class SystemInfoHelper
{
    internal static bool IsWindows11 => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);
}

internal static class ThemeHelper
{
    private enum PreferredAppMode
    {
        Default = 0,
        AllowDark = 1,
        ForceDark = 2,
        ForceLight = 3,
        Max = 4
    }

    internal static bool IsLightTheme
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int appValue)
                {
                    return appValue == 1;
                }

                if (key?.GetValue("SystemUsesLightTheme") is int systemValue)
                {
                    return systemValue == 1;
                }
            }
            catch
            {
                // Default to dark mode if registry read fails
            }

            return false;
        }
    }

    internal static void ApplyNativeMenuTheme()
    {
        try
        {
            var preferredMode = IsLightTheme ? PreferredAppMode.ForceLight : PreferredAppMode.ForceDark;
            SetPreferredAppMode(preferredMode);
            RefreshImmersiveColorPolicyState();
            FlushMenuThemes();
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
        catch
        {
            // Ignore unsupported platform/theme API errors.
        }
    }

    internal static void ApplyNativeWindowTheme(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return;
        }

        ApplyNativeMenuTheme();

        try
        {
            var useDarkMode = !IsLightTheme;
            AllowDarkModeForWindow(handle, useDarkMode);
            DwmSetWindowAttribute(handle, DwmWindowAttributeUseImmersiveDarkMode, ref useDarkMode, sizeof(int));

            var subAppName = useDarkMode ? "DarkMode_Explorer" : "Explorer";
            SetWindowTheme(handle, subAppName, null);
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
        catch
        {
            // Ignore unsupported platform/theme API errors.
        }
    }

    internal static class Colors
    {
        internal static Color GetSystemAccentColor()
        {
            try
            {
                if (DwmGetColorizationColor(out var colorValue, out _) == 0)
                {
                    var r = (byte)((colorValue >> 16) & 0xFF);
                    var g = (byte)((colorValue >> 8) & 0xFF);
                    var b = (byte)(colorValue & 0xFF);
                    return Color.FromArgb(r, g, b);
                }
            }
            catch
            {
                // Fall through to default accent
            }

            return Color.FromArgb(0, 120, 215);
        }

        internal static Color GetFlyoutBackgroundColor()
        {
            if (SystemInfoHelper.IsWindows11)
            {
                return IsLightTheme ? Color.FromArgb(243, 243, 243) : Color.FromArgb(43, 43, 43);
            }

            return IsLightTheme ? Color.FromArgb(221, 221, 221) : Color.FromArgb(34, 34, 34);
        }

        internal static Color GetFlyoutButtonBackgroundColor()
        {
            if (SystemInfoHelper.IsWindows11)
            {
                return IsLightTheme ? Color.FromArgb(251, 251, 251) : Color.FromArgb(53, 53, 53);
            }

            return IsLightTheme ? Color.FromArgb(251, 251, 251) : Color.FromArgb(45, 45, 45);
        }

        internal static Color GetFlyoutButtonHoverBackgroundColor()
        {
            if (SystemInfoHelper.IsWindows11)
            {
                return IsLightTheme ? Color.FromArgb(245, 245, 245) : Color.FromArgb(61, 61, 61);
            }

            return IsLightTheme ? Color.FromArgb(245, 245, 245) : Color.FromArgb(50, 50, 50);
        }

        internal static Color GetFlyoutButtonPressedBackgroundColor()
        {
            if (SystemInfoHelper.IsWindows11)
            {
                return IsLightTheme ? Color.FromArgb(236, 236, 236) : Color.FromArgb(68, 68, 68);
            }

            return IsLightTheme ? Color.FromArgb(236, 236, 236) : Color.FromArgb(58, 58, 58);
        }

        internal static Color GetFlyoutBorderColor()
        {
            if (SystemInfoHelper.IsWindows11)
            {
                return IsLightTheme ? Color.FromArgb(193, 193, 193) : Color.FromArgb(82, 82, 82);
            }

            return IsLightTheme ? Color.FromArgb(193, 193, 193) : Color.FromArgb(85, 85, 85);
        }

        internal static Color GetFlyoutButtonForegroundColor()
        {
            return IsLightTheme ? Color.FromArgb(32, 32, 32) : Color.FromArgb(245, 245, 245);
        }

        internal static Color GetFlyoutButtonArrowColor()
        {
            return IsLightTheme ? Color.FromArgb(99, 99, 99) : Color.FromArgb(190, 190, 190);
        }

        internal static Font GetFlyoutButtonFont()
        {
            return SystemInfoHelper.IsWindows11
                ? new Font("Segoe UI Variable Text", 9F, FontStyle.Regular, GraphicsUnit.Point)
                : new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        }

        internal static Font GetFlyoutNumberFont()
        {
            return SystemInfoHelper.IsWindows11
                ? new Font("Segoe UI Variable Text", 22F, FontStyle.Regular, GraphicsUnit.Point)
                : new Font("Segoe UI", 23F, FontStyle.Regular, GraphicsUnit.Point);
        }

        internal static Color GetCountdownBackgroundColor()
        {
            var accentColor = GetSystemAccentColor();
            return IsLightTheme
                ? BlendColors(accentColor, Color.White, 0.18)
                : BlendColors(accentColor, Color.Black, 0.18);
        }

        internal static Color GetCountdownForegroundColor()
        {
            var countdownBackgroundColor = GetCountdownBackgroundColor();
            return GetRelativeLuminance(countdownBackgroundColor) >= 0.45
                ? Color.FromArgb(20, 20, 20)
                : Color.FromArgb(255, 255, 255);
        }

        internal static Font GetCountdownFont(bool compact)
        {
            if (SystemInfoHelper.IsWindows11)
            {
                return new Font("Segoe UI Variable Text Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point);
            }

            return new Font("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point);
        }

        internal static Color GetBackgroundColor()
        {
            return IsLightTheme ? Color.FromArgb(255, 255, 255) : Color.FromArgb(45, 45, 45);
        }

        internal static Color GetSettingsBackgroundColor()
        {
            return IsLightTheme ? Color.FromArgb(243, 243, 243) : Color.FromArgb(32, 32, 32);
        }

        internal static Color GetForegroundColor()
        {
            return IsLightTheme ? Color.FromArgb(0, 0, 0) : Color.FromArgb(255, 255, 255);
        }

        internal static Color GetPanelBackgroundColor()
        {
            return IsLightTheme ? Color.FromArgb(240, 240, 240) : Color.FromArgb(45, 45, 45);
        }

        internal static Color GetSettingsPanelBackgroundColor()
        {
            return IsLightTheme ? Color.FromArgb(249, 249, 249) : Color.FromArgb(45, 45, 45);
        }

        internal static Color GetSettingsSelectedTabBackgroundColor()
        {
            return IsLightTheme ? GetSettingsPanelBackgroundColor() : Color.FromArgb(45, 45, 45);
        }

        internal static Color GetSettingsSelectedTabForegroundColor()
        {
            return IsLightTheme ? Color.FromArgb(0, 92, 197) : Color.FromArgb(240, 240, 240);
        }

        internal static Color GetSettingsUnselectedTabForegroundColor()
        {
            return IsLightTheme ? Color.FromArgb(60, 60, 60) : Color.FromArgb(184, 184, 184);
        }

        internal static Color GetSettingsFooterButtonBorderColor()
        {
            return IsLightTheme ? Color.FromArgb(182, 182, 182) : Color.FromArgb(89, 89, 89);
        }

        internal static Color GetSettingsFooterButtonBackgroundColor()
        {
            return IsLightTheme ? Color.FromArgb(248, 248, 248) : Color.FromArgb(64, 64, 64);
        }

        internal static Color GetSettingsFooterButtonHoverBackgroundColor()
        {
            return IsLightTheme ? Color.FromArgb(242, 242, 242) : Color.FromArgb(70, 70, 70);
        }

        internal static Color GetSettingsFooterButtonPressedBackgroundColor()
        {
            return IsLightTheme ? Color.FromArgb(236, 236, 236) : Color.FromArgb(52, 52, 52);
        }

        internal static Color GetSettingsDisabledButtonBackgroundColor()
        {
            return IsLightTheme ? Color.FromArgb(244, 244, 244) : Color.FromArgb(54, 54, 54);
        }

        internal static Color GetSettingsDisabledButtonBorderColor()
        {
            return IsLightTheme ? Color.FromArgb(182, 182, 182) : Color.FromArgb(72, 72, 72);
        }

        internal static Color GetSettingsDisabledButtonForegroundColor()
        {
            return Color.FromArgb(75, 75, 75);
        }

        internal static Color GetControlBackgroundColor()
        {
            return IsLightTheme ? Color.FromArgb(255, 255, 255) : Color.FromArgb(55, 55, 55);
        }

        internal static Color GetAccentColor()
        {
            return Color.FromArgb(238, 121, 59);
        }

        internal static Color GetLinkColor()
        {
            return IsLightTheme ? Color.FromArgb(0, 120, 215) : Color.FromArgb(238, 121, 59);
        }

        private static Color BlendColors(Color baseColor, Color overlayColor, double overlayAmount)
        {
            var clampedOverlayAmount = Math.Clamp(overlayAmount, 0d, 1d);
            var baseAmount = 1d - clampedOverlayAmount;
            return Color.FromArgb(
                (int)Math.Round((baseColor.R * baseAmount) + (overlayColor.R * clampedOverlayAmount)),
                (int)Math.Round((baseColor.G * baseAmount) + (overlayColor.G * clampedOverlayAmount)),
                (int)Math.Round((baseColor.B * baseAmount) + (overlayColor.B * clampedOverlayAmount)));
        }

        private static double GetRelativeLuminance(Color color)
        {
            static double ToLinear(byte channel)
            {
                var normalized = channel / 255d;
                return normalized <= 0.03928
                    ? normalized / 12.92
                    : Math.Pow((normalized + 0.055) / 1.055, 2.4);
            }

            return (0.2126 * ToLinear(color.R)) +
                   (0.7152 * ToLinear(color.G)) +
                   (0.0722 * ToLinear(color.B));
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetColorizationColor(out uint pcrColorization, out bool pfOpaqueBlend);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref bool pvAttribute, int cbAttribute);

    [DllImport("uxtheme.dll", EntryPoint = "#104", SetLastError = true)]
    private static extern void RefreshImmersiveColorPolicyState();

    [DllImport("uxtheme.dll", EntryPoint = "#133", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllowDarkModeForWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool allow);

    [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true)]
    private static extern PreferredAppMode SetPreferredAppMode(PreferredAppMode appMode);

    [DllImport("uxtheme.dll", EntryPoint = "#136", SetLastError = true)]
    private static extern void FlushMenuThemes();

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SetWindowTheme(IntPtr hWnd, string? pszSubAppName, string? pszSubIdList);

    private const int DwmWindowAttributeUseImmersiveDarkMode = 20;
}
