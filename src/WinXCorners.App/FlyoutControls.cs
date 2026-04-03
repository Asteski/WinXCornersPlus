using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace WinXCorners.App;

internal static class RoundedRectangleGeometry
{
    internal static GraphicsPath CreatePath(RectangleF rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Max(1f, radius * 2f);
        var arc = new RectangleF(rect.Location, new SizeF(diameter, diameter));

        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    internal static GraphicsPath CreatePath(Rectangle rect, int radius)
    {
        return CreatePath(new RectangleF(rect.X, rect.Y, rect.Width, rect.Height), radius);
    }
}

internal sealed class FlyoutActionControl : Control
{
    private bool _hovered;
    private bool _pressed;

    internal FlyoutActionControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor,
            true);
        Size = new Size(148, 28);
        Font = ThemeHelper.Colors.GetFlyoutButtonFont();
        ForeColor = ThemeHelper.Colors.GetFlyoutButtonForegroundColor();
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Invalidate();
        Update();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        ForeColor = ThemeHelper.Colors.GetFlyoutButtonForegroundColor();

        var fill = _pressed
            ? ThemeHelper.Colors.GetFlyoutButtonPressedBackgroundColor()
            : _hovered ? ThemeHelper.Colors.GetFlyoutButtonHoverBackgroundColor() : ThemeHelper.Colors.GetFlyoutButtonBackgroundColor();
        var border = ThemeHelper.Colors.GetFlyoutBorderColor();
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);

        using var path = RoundedRectangleGeometry.CreatePath(rect, 5);
        using var brush = new SolidBrush(fill);
        using var pen = new Pen(border);
        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(pen, path);

        var textRect = new Rectangle(10, 0, Width - 34, Height);
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            textRect,
            ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        var arrowX = Width - 17;
        var arrowY = (Height / 2) - 2;
        using var arrowPen = new Pen(ThemeHelper.Colors.GetFlyoutButtonArrowColor(), 1.4f);
        e.Graphics.DrawLines(arrowPen,
        [
            new Point(arrowX - 5, arrowY),
            new Point(arrowX, arrowY + 5),
            new Point(arrowX + 5, arrowY)
        ]);
    }

}

internal sealed class AccentTileControl : Control
{
    private int _cornerRadius = 6;
    private Color _accentColor = Color.FromArgb(0, 120, 215);

    internal AccentTileControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor,
            true);

        BackColor = Color.Transparent;
        ForeColor = Color.White;
        Font = ThemeHelper.Colors.GetFlyoutNumberFont();
        Text = "1";
        Size = new Size(171, 96);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal int CornerRadius
    {
        get => _cornerRadius;
        set
        {
            if (_cornerRadius == value)
            {
                return;
            }

            _cornerRadius = Math.Max(0, value);
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal Color AccentColor
    {
        get => _accentColor;
        set
        {
            if (_accentColor == value)
            {
                return;
            }

            _accentColor = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;

        var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using var path = RoundedRectangleGeometry.CreatePath(rect, CornerRadius);
        using var brush = new SolidBrush(AccentColor);
        e.Graphics.FillPath(brush, path);

        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            Rectangle.Ceiling(rect),
            ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }
}

internal sealed class ToggleSwitchControl : Control
{
    private bool _checked;

    internal ToggleSwitchControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        Size = new Size(44, 20);
        Cursor = Cursors.Hand;
    }

    [DefaultValue(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    internal bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
            {
                return;
            }

            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal event EventHandler? CheckedChanged;

    protected override void OnClick(EventArgs e)
    {
        Checked = !Checked;
        base.OnClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var trackColor = Checked ? Color.FromArgb(0, 120, 215) : Color.FromArgb(166, 166, 166);
        using var trackBrush = new SolidBrush(trackColor);
        using var thumbBrush = new SolidBrush(Color.White);
        using var path = new GraphicsPath();
        path.AddArc(0, 0, Height - 1, Height - 1, 90, 180);
        path.AddArc(Width - Height, 0, Height - 1, Height - 1, 270, 180);
        path.CloseFigure();
        e.Graphics.FillPath(trackBrush, path);

        var thumbDiameter = Height - 8;
        var thumbX = Checked ? Width - thumbDiameter - 4 : 4;
        e.Graphics.FillEllipse(thumbBrush, thumbX, 4, thumbDiameter, thumbDiameter);
    }
}
