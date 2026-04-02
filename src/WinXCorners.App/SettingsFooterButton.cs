using System.Windows.Forms.VisualStyles;

namespace WinXCorners.App;

internal sealed class SettingsFooterButton : Button
{
    private bool _isHovered;
    private bool _isPressed;

    internal SettingsFooterButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        UseVisualStyleBackColor = true;
        FlatStyle = FlatStyle.Standard;
        TabStop = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        ButtonRenderer.DrawParentBackground(e.Graphics, ClientRectangle, this);

        var state = Enabled
            ? _isPressed
                ? PushButtonState.Pressed
                : _isHovered
                    ? PushButtonState.Hot
                    : PushButtonState.Normal
            : PushButtonState.Disabled;

        ButtonRenderer.DrawButton(e.Graphics, ClientRectangle, Text, Font, Focused, state);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        _isPressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        if (mevent.Button == MouseButtons.Left)
        {
            _isPressed = true;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        _isPressed = false;
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }
}