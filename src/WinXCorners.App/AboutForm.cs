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
            AutoSize = false,
            Location = new Point(18, 16),
            Size = new Size(200, 28),
            BackColor = Color.Transparent,
            ForeColor = ThemeHelper.Colors.GetForegroundColor()
        };

        var descriptionLabel = new Label
        {
            Text = "Version: v1.4.0\r\n\r\nCreated by Adam Kamienski\r\nOriginally developed by vhanla\r\n\r\n© 2026 Asteski, 2019 - 2024 vhanla\r\n\r\nhttps://github.com/Asteski\r\nhttps://github.com/vhanla",
            AutoSize = false,
            Location = new Point(20, 48),
            Size = new Size(190, 72), // Match link widths and allow the extra blank line
            BackColor = Color.Transparent,
            ForeColor = ThemeHelper.Colors.GetForegroundColor()
        };

        var githubAsteskiLink = new LinkLabel
        {
            Text = "https://github.com/Asteski",
            AutoSize = false,
            Location = new Point(20, 120),
            Size = new Size(190, 16),
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
            AutoSize = false,
            Location = new Point(20, 140),
            Size = new Size(190, 16),
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

        // capture base bounds for runtime scaling
        _baseClientSize = ClientSize;
        foreach (var c in GetAllControls(this))
        {
            _baseControlBounds[c] = c.Bounds;
        }

        // apply initial scaling based on current DPI
        ApplyScalingForCurrentDpi();
    }

    private double _scaleFactor = 1.0;
    private Size _baseClientSize = Size.Empty;
    private readonly Dictionary<Control, Rectangle> _baseControlBounds = new();

    protected override void WndProc(ref Message m)
    {
        const int wmDpiChanged = 0x02E0;
        if (m.Msg == wmDpiChanged)
        {
            ApplyScalingForCurrentDpi();
        }

        base.WndProc(ref m);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetDpiForWindow(IntPtr hwnd);

    private double GetCurrentScaleFactor()
    {
        try
        {
            if (IsHandleCreated)
            {
                var dpi = GetDpiForWindow(Handle);
                if (dpi > 0)
                {
                    return dpi / 96.0;
                }
            }
        }
        catch
        {
        }

        using var g = CreateGraphics();
        return g.DpiX / 96.0;
    }

    private void ApplyScalingForCurrentDpi()
    {
        var scale = GetCurrentScaleFactor();
        if (Math.Abs(scale - _scaleFactor) < 0.01)
        {
            return;
        }

        _scaleFactor = scale;
        if (_baseClientSize == Size.Empty)
        {
            _baseClientSize = ClientSize;
        }

        ClientSize = new Size((int)Math.Round(_baseClientSize.Width * _scaleFactor), (int)Math.Round(_baseClientSize.Height * _scaleFactor));

        foreach (var kvp in _baseControlBounds)
        {
            var control = kvp.Key;
            var bounds = kvp.Value;
            control.Bounds = new Rectangle(
                (int)Math.Round(bounds.X * _scaleFactor),
                (int)Math.Round(bounds.Y * _scaleFactor),
                (int)Math.Round(bounds.Width * _scaleFactor),
                (int)Math.Round(bounds.Height * _scaleFactor));
        }
    }

    private static IEnumerable<Control> GetAllControls(Control root)
    {
        var list = new List<Control>();
        var stack = new Stack<Control>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var c = stack.Pop();
            list.Add(c);
            foreach (Control child in c.Controls)
            {
                stack.Push(child);
            }
        }

        return list;
    }
}