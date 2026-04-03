using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace WinXCorners.App;

internal sealed class CountdownOverlay : Form
{
    private const int ScreenMargin = 8;
    private const int CountdownWidth = 40;
    private const int CountdownHeight = 40;
    private string _text = "1";

    internal CountdownOverlay()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        BackColor = ThemeHelper.Colors.GetCountdownBackgroundColor();
        Size = new Size(CountdownWidth, CountdownHeight);
        Font = ThemeHelper.Colors.GetCountdownFont(compact: false);
        DoubleBuffered = true;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int wsExToolWindow = 0x00000080;
            const int wsExNoActivate = 0x08000000;

            var createParams = base.CreateParams;
            createParams.ExStyle |= wsExToolWindow | wsExNoActivate;
            return createParams;
        }
    }

    internal void ShowCountdown(Screen screen, HotCornerArea area, TimeSpan remaining)
    {
        _text = remaining.TotalSeconds >= 1
            ? Math.Ceiling(remaining.TotalSeconds).ToString("0")
            : remaining.TotalSeconds.ToString("0.0");

        ApplyTheme();
        Size = GetBadgeSize();

        Location = GetLocation(screen.Bounds, area, Size);
        if (!Visible)
        {
            Show();
        }

        Invalidate();
    }

    internal void HideCountdown()
    {
        if (Visible)
        {
            Hide();
        }
    }

    internal void RefreshTheme()
    {
        ApplyTheme();
        if (Visible)
        {
            Invalidate();
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        Region?.Dispose();
        using var path = new GraphicsPath();
        path.AddEllipse(ClientRectangle);
        Region = new Region(path);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var bounds = Rectangle.Inflate(ClientRectangle, -1, -1);
        using var fillBrush = new SolidBrush(ThemeHelper.Colors.GetCountdownBackgroundColor());
        using var borderPen = new Pen(Color.FromArgb(48, ThemeHelper.Colors.GetCountdownForegroundColor()));

        e.Graphics.FillEllipse(fillBrush, bounds);
        e.Graphics.DrawEllipse(borderPen, bounds);
        TextRenderer.DrawText(
            e.Graphics,
            _text,
            Font,
            bounds,
            ThemeHelper.Colors.GetCountdownForegroundColor(),
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding |
            TextFormatFlags.SingleLine);
    }

    private void ApplyTheme()
    {
        BackColor = ThemeHelper.Colors.GetCountdownBackgroundColor();
        Font = ThemeHelper.Colors.GetCountdownFont(_text.Length > 1);
    }

    private Size GetBadgeSize()
    {
        return new Size(CountdownWidth, CountdownHeight);
    }

    private static Point GetLocation(Rectangle bounds, HotCornerArea area, Size overlaySize)
    {
        return area switch
        {
            HotCornerArea.TopLeft => new Point(bounds.Left + ScreenMargin, bounds.Top + ScreenMargin),
            HotCornerArea.TopRight => new Point(bounds.Right - overlaySize.Width - ScreenMargin, bounds.Top + ScreenMargin),
            HotCornerArea.BottomLeft => new Point(bounds.Left + ScreenMargin, bounds.Bottom - overlaySize.Height - ScreenMargin),
            HotCornerArea.BottomRight => new Point(bounds.Right - overlaySize.Width - ScreenMargin, bounds.Bottom - overlaySize.Height - ScreenMargin),
            _ => new Point(bounds.Left + ScreenMargin, bounds.Top + ScreenMargin)
        };
    }

}