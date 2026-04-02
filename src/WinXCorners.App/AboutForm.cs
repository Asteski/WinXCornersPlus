namespace WinXCorners.App;

internal sealed class AboutForm : Form
{
    internal AboutForm()
    {
        Text = "About WinXCornersPlus";
        StartPosition = FormStartPosition.CenterScreen; // Changed to make it independent
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true; // Allow interaction with other windows
        ClientSize = new Size(228, 178); // Increased width by 15px
        BackColor = ThemeHelper.Colors.GetBackgroundColor();
        ForeColor = ThemeHelper.Colors.GetForegroundColor();
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        Icon = WindowIconLoader.TryLoadAboutIcon();

        var titleLabel = new Label
        {
            Text = "WinXCornersPlus",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point),
            AutoSize = true,
            Location = new Point(18, 16),
            BackColor = Color.Transparent,
            ForeColor = ThemeHelper.Colors.GetForegroundColor()
        };

        var descriptionLabel = new Label
        {
            Text = "Version: v1.4.0\r\n\r\nCreated by Adam Kamienski\r\nOriginally developed by vhanla\r\n\r\n© 2026 Asteski, 2019 - 2024 vhanla\r\n\r\nhttps://github.com/Asteski\r\nhttps://github.com/vhanla",
            AutoSize = false,
            Location = new Point(20, 48),
            Size = new Size(192, 68), // Adjusted width to fit the text
            BackColor = Color.Transparent,
            ForeColor = ThemeHelper.Colors.GetForegroundColor()
        };

        var githubAsteskiLink = new LinkLabel
        {
            Text = "https://github.com/Asteski",
            AutoSize = true,
            Location = new Point(20, 120),
            BackColor = Color.Transparent,
            ForeColor = ThemeHelper.Colors.GetAccentColor() // Use active accent color
        };
        githubAsteskiLink.Click += (sender, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://github.com/Asteski",
            UseShellExecute = true
        });

        var githubVhanlaLink = new LinkLabel
        {
            Text = "https://github.com/vhanla",
            AutoSize = true,
            Location = new Point(20, 140),
            BackColor = Color.Transparent,
            ForeColor = ThemeHelper.Colors.GetAccentColor() // Use active accent color
        };
        githubVhanlaLink.Click += (sender, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://github.com/vhanla",
            UseShellExecute = true
        });

        Controls.Add(titleLabel);
        Controls.Add(descriptionLabel);
        Controls.Add(githubAsteskiLink);
        Controls.Add(githubVhanlaLink);
    }
}