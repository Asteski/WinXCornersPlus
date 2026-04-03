namespace WinXCorners.App;

internal sealed class LogWindowForm : Form
{
    private readonly TextBox _logText;

    internal LogWindowForm()
    {
        Text = "WinXCornersPlus Log Window";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(760, 420);
        MinimumSize = new Size(640, 320);
        BackColor = ThemeHelper.Colors.GetBackgroundColor();

        _logText = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point),
            BackColor = ThemeHelper.Colors.GetControlBackgroundColor(),
            ForeColor = ThemeHelper.Colors.GetForegroundColor(),
            Dock = DockStyle.Fill
        };

        var clearButton = new Button
        {
            Text = "Clear",
            Dock = DockStyle.Right,
            Width = 90,
            BackColor = ThemeHelper.Colors.GetControlBackgroundColor(),
            ForeColor = ThemeHelper.Colors.GetForegroundColor()
        };
        clearButton.Click += (_, _) => _logText.Clear();

        var closeButton = new Button
        {
            Text = "Close",
            Dock = DockStyle.Right,
            Width = 90,
            BackColor = ThemeHelper.Colors.GetControlBackgroundColor(),
            ForeColor = ThemeHelper.Colors.GetForegroundColor()
        };
        closeButton.Click += (_, _) => Hide();

        var topPanel = new Panel
        {
            Height = 38,
            Dock = DockStyle.Top,
            Padding = new Padding(8, 6, 8, 6),
            BackColor = ThemeHelper.Colors.GetPanelBackgroundColor()
        };

        var title = new Label
        {
            Text = "Runtime events",
            AutoSize = true,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = ThemeHelper.Colors.GetForegroundColor(),
            Padding = new Padding(0, 6, 0, 0)
        };

        topPanel.Controls.Add(closeButton);
        topPanel.Controls.Add(clearButton);
        topPanel.Controls.Add(title);

        Controls.Add(_logText);
        Controls.Add(topPanel);

        foreach (var entry in AppLogger.GetEntries())
        {
            AppendLog(entry);
        }
    }

    internal void AppendLog(string entry)
    {
        if (_logText.TextLength > 0)
        {
            _logText.AppendText(Environment.NewLine);
        }

        _logText.AppendText(entry);
        _logText.SelectionStart = _logText.TextLength;
        _logText.ScrollToCaret();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
