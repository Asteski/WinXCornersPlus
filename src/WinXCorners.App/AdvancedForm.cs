using System.Diagnostics;

namespace WinXCorners.App;

internal sealed class AdvancedForm : Form
{
    private static readonly string[] DelayValues = ["0.25", "0.50", "0.75", "1.00", "1.25", "1.50"];
    private const int DelayPanelLeftColumnX = 16;
    private const int DelayPanelRightColumnX = 225;
    private const int DelayPanelColumnLabelWidth = 132;
    private const int DelayPanelLeftComboX = 148;
    private const int DelayPanelRightComboX = 357;
    private const int DelayPanelComboWidth = 48;
    private const int DelayPanelFirstRowY = 18;
    private const int DelayPanelSecondRowY = 52;
    private const int DelayPanelGlobalRowY = 86;
    private const int DelayPanelFooterRowY = 118;

    private readonly ApplicationSettings _workingCopy;
    private readonly CheckBox _chkGlobalDelay;
    private readonly ComboBox _cbGlobalDelay;
    private readonly CheckBox _chkTopLeftDelay;
    private readonly ComboBox _cbTopLeftDelay;
    private readonly CheckBox _chkTopRightDelay;
    private readonly ComboBox _cbTopRightDelay;
    private readonly CheckBox _chkBottomLeftDelay;
    private readonly ComboBox _cbBottomLeftDelay;
    private readonly CheckBox _chkBottomRightDelay;
    private readonly ComboBox _cbBottomRightDelay;
    private readonly CheckBox _chkShowCountdown;
    private readonly CheckBox _chkStartWithWindows;
    private readonly CheckBox _chkFullScreen;
    private readonly CheckBox _chkAlwaysRunAsAdministrator;
    private readonly CheckBox _chkAlwaysHideTrayIcon;
    private readonly ComboBox _cbFlyoutAnimationDirection;
    private readonly CheckBox _chkCustom;
    private readonly TextBox _txtCommandName;
    private readonly TextBox _txtCommand;
    private readonly TextBox _txtParameters;
    private readonly Panel _topSectionHost;
    private readonly Panel _delayPanel;
    private readonly Panel _otherPanel;
    private readonly Button[] _topSectionTabs;
    private readonly Button[] _commandTabs;
    private readonly Button _btnApply;
    private bool _isDirty;
    private bool _suppressDirtyTracking;
    private int _selectedTopSectionTab;
    private int _selectedCommandIndex = -1;
    private double _scaleFactor = 1.0;
    private Size _baseClientSize = Size.Empty;
    private readonly Dictionary<Control, Rectangle> _baseControlBounds = new();

    private static readonly (string Text, FlyoutAnimationDirection Value)[] FlyoutAnimationDirections =
    [
        ("Top", FlyoutAnimationDirection.Top),
        ("Bottom", FlyoutAnimationDirection.Bottom),
        ("Left", FlyoutAnimationDirection.Left),
        ("Right", FlyoutAnimationDirection.Right)
    ];

    internal event Action<ApplicationSettings>? SettingsApplied;

    internal AdvancedForm(ApplicationSettings settings)
    {
        _workingCopy = settings.Clone();
        Settings = settings.Clone();
        HandleCreated += (_, _) => ThemeHelper.ApplyNativeWindowTheme(Handle);

        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(438, 452);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.Manual;
        Text = "WinXCornersPlus Settings";
        BackColor = ThemeHelper.Colors.GetSettingsBackgroundColor();
        ForeColor = ThemeHelper.Colors.GetForegroundColor();
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        Icon = WindowIconLoader.TryLoadAppIcon();

        var updatesLink = new LinkLabel
        {
            Text = "Check for updates",
            LinkColor = ThemeHelper.Colors.GetLinkColor(),
            ActiveLinkColor = Color.FromArgb(255, 160, 100),
            VisitedLinkColor = ThemeHelper.Colors.GetLinkColor(),
            Location = new Point(16, 423),
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = ThemeHelper.Colors.GetForegroundColor()
        };
        updatesLink.Click += (_, _) => OpenUrl("https://github.com/asteski/winxcornersplus/releases");

        var betaLabel = new Label
        {
            Text = "Beta build (20240630)",
            ForeColor = Color.Gray,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(307, 188),
            Visible = false
        };

        var btnOk = new SettingsFooterButton
        {
            Text = "&OK",
            Location = new Point(270, 419),
            Size = new Size(74, 23),
            DialogResult = DialogResult.OK
        };
        btnOk.Click += (_, _) => SaveAndClose();

        _btnApply = new SettingsFooterButton
        {
            Text = "&Apply",
            Location = new Point(189, 419),
            Size = new Size(74, 23),
            Enabled = false
        };
        _btnApply.Click += (_, _) => ApplyChanges();

        var btnCancel = new SettingsFooterButton
        {
            Text = "&Cancel",
            Location = new Point(351, 419),
            Size = new Size(74, 23),
            DialogResult = DialogResult.Cancel
        };
        btnCancel.Click += (_, _) => Close();

        _topSectionTabs = new Button[2];
        var topTabsHost = new Panel
        {
            Location = new Point(16, 7),
            Size = new Size(136, 24),
            BackColor = Color.Transparent
        };

        for (var index = 0; index < 2; index++)
        {
            var tabIndex = index;
            var tabButton = new Button
            {
                Text = index == 0 ? "Delays" : "Advanced",
                Location = new Point(index * 68, 0),
                Size = new Size(68, 24),
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeHelper.Colors.GetSettingsBackgroundColor(),
                ForeColor = ThemeHelper.Colors.GetSettingsUnselectedTabForegroundColor(),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleCenter,
                TabStop = false
            };
            tabButton.FlatAppearance.BorderSize = 0;
            tabButton.FlatAppearance.MouseOverBackColor = ThemeHelper.Colors.GetSettingsSelectedTabBackgroundColor();
            tabButton.FlatAppearance.MouseDownBackColor = ThemeHelper.Colors.GetSettingsSelectedTabBackgroundColor();
            tabButton.Click += (_, _) => SelectTopSectionTab(tabIndex);
            _topSectionTabs[index] = tabButton;
            topTabsHost.Controls.Add(tabButton);
        }

        _topSectionHost = new Panel
        {
            Location = new Point(8, 31),
            Size = new Size(422, 145),
            BackColor = Color.Transparent
        };

        _delayPanel = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(422, 145),
            BackColor = ThemeHelper.Colors.GetSettingsPanelBackgroundColor(),
            BorderStyle = BorderStyle.FixedSingle
        };

        _otherPanel = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(422, 145),
            BackColor = ThemeHelper.Colors.GetSettingsPanelBackgroundColor(),
            BorderStyle = BorderStyle.FixedSingle,
            Visible = false
        };

        _chkGlobalDelay = CreateCheckBox("Set a global delay in seconds:", new Point(16, DelayPanelGlobalRowY));
        _chkGlobalDelay.AutoSize = false;
        _chkGlobalDelay.Size = new Size(214, 20);
        _chkGlobalDelay.CheckedChanged += (_, _) => UpdateDelayState();
        _cbGlobalDelay = CreateDelayCombo(new Point(DelayPanelRightColumnX + 7, DelayPanelGlobalRowY - 4));

        _chkTopLeftDelay = CreateAlignedDelayCheckBox("Top Left", DelayPanelLeftColumnX, DelayPanelFirstRowY);
        _chkTopLeftDelay.CheckedChanged += (_, _) => UpdateDelayState();
        _cbTopLeftDelay = CreateDelayCombo(new Point(DelayPanelLeftComboX, DelayPanelFirstRowY - 3));
        _chkTopRightDelay = CreateAlignedDelayCheckBox("Top Right", DelayPanelRightColumnX, DelayPanelFirstRowY);
        _chkTopRightDelay.CheckedChanged += (_, _) => UpdateDelayState();
        _cbTopRightDelay = CreateDelayCombo(new Point(DelayPanelRightComboX, DelayPanelFirstRowY - 3));
        _chkBottomLeftDelay = CreateAlignedDelayCheckBox("Bottom Left", DelayPanelLeftColumnX, DelayPanelSecondRowY);
        _chkBottomLeftDelay.CheckedChanged += (_, _) => UpdateDelayState();
        _cbBottomLeftDelay = CreateDelayCombo(new Point(DelayPanelLeftComboX, DelayPanelSecondRowY - 3));
        _chkBottomRightDelay = CreateAlignedDelayCheckBox("Bottom Right", DelayPanelRightColumnX, DelayPanelSecondRowY);
        _chkBottomRightDelay.CheckedChanged += (_, _) => UpdateDelayState();
        _cbBottomRightDelay = CreateDelayCombo(new Point(DelayPanelRightComboX, DelayPanelSecondRowY - 3));
        _chkShowCountdown = CreateCheckBox("Show Countdown", new Point(16, DelayPanelFooterRowY));

        _delayPanel.Controls.AddRange(
        [
            _chkGlobalDelay,
            _cbGlobalDelay,
            _chkTopLeftDelay,
            _cbTopLeftDelay,
            _chkTopRightDelay,
            _cbTopRightDelay,
            _chkBottomLeftDelay,
            _cbBottomLeftDelay,
            _chkBottomRightDelay,
            _cbBottomRightDelay,
            _chkShowCountdown
        ]);

        _chkStartWithWindows = CreateCheckBox("Run at startup", new Point(16, 19));
        _chkFullScreen = CreateCheckBox("Do nothing on Full Screen", new Point(16, 47));
        _chkAlwaysRunAsAdministrator = CreateCheckBox("Always run as administrator", new Point(16, 75));
        _chkAlwaysHideTrayIcon = CreateCheckBox("Always hide tray icon", new Point(16, 103));
        var lblFlyoutAnimationDirection = new Label
        {
            Text = "Flyout animation direction",
            Location = new Point(228, 21),
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = ThemeHelper.Colors.GetForegroundColor()
        };
        _cbFlyoutAnimationDirection = new ComboBox
        {
            Location = new Point(228, 45),
            Size = new Size(150, 23),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = ThemeHelper.Colors.GetControlBackgroundColor(),
            ForeColor = ThemeHelper.Colors.GetForegroundColor(),
            FlatStyle = FlatStyle.Flat
        };
        _cbFlyoutAnimationDirection.Items.AddRange(FlyoutAnimationDirections.Select(static direction => direction.Text).ToArray());
        _otherPanel.Controls.AddRange([
            _chkStartWithWindows,
            _chkFullScreen,
            _chkAlwaysRunAsAdministrator,
            _chkAlwaysHideTrayIcon,
            lblFlyoutAnimationDirection,
            _cbFlyoutAnimationDirection
        ]);

        _topSectionHost.Controls.AddRange([_delayPanel, _otherPanel]);

        _commandTabs = new Button[4];
        var tabsHost = new Panel
        {
            Location = new Point(16, 196),
            Size = new Size(352, 24),
            BackColor = Color.Transparent
        };
        for (var index = 0; index < 4; index++)
        {
            var tabIndex = index;
            var tabButton = new Button
            {
                Text = $"Command {index + 1}",
                Location = new Point(index * 88, 0),
                Size = new Size(88, 24),
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeHelper.Colors.GetSettingsBackgroundColor(),
                ForeColor = ThemeHelper.Colors.GetSettingsUnselectedTabForegroundColor(),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleCenter,
                TabStop = false
            };
            tabButton.FlatAppearance.BorderSize = 0;
            tabButton.FlatAppearance.MouseOverBackColor = ThemeHelper.Colors.GetSettingsSelectedTabBackgroundColor();
            tabButton.FlatAppearance.MouseDownBackColor = ThemeHelper.Colors.GetSettingsSelectedTabBackgroundColor();
            tabButton.Click += (_, _) => SelectCommandTab(tabIndex);
            _commandTabs[index] = tabButton;
            tabsHost.Controls.Add(tabButton);
        }

        var customPanel = new Panel
        {
            Location = new Point(8, 220),
            Size = new Size(422, 180),
            BackColor = ThemeHelper.Colors.GetSettingsPanelBackgroundColor(),
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblCommandName = new Label
        {
            Text = "Custom command name",
            Location = new Point(16, 10),
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = ThemeHelper.Colors.GetForegroundColor()
        };

        _txtCommandName = new TextBox
        {
            Location = new Point(16, 27),
            Size = new Size(368, 23),
            BackColor = ThemeHelper.Colors.GetControlBackgroundColor(),
            ForeColor = ThemeHelper.Colors.GetForegroundColor(),
            BorderStyle = BorderStyle.FixedSingle,
            MaxLength = 12,
            PlaceholderText = "Command 1"
        };
        _txtCommandName.TextChanged += (_, _) => SaveCurrentTab();

        var lblCommand = new Label
        {
            Text = "Command path or hotkey sequence",
            Location = new Point(16, 55),
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = ThemeHelper.Colors.GetForegroundColor()
        };

        _txtCommand = new TextBox
        {
            Location = new Point(16, 72),
            Size = new Size(368, 23),
            BackColor = ThemeHelper.Colors.GetControlBackgroundColor(),
            ForeColor = ThemeHelper.Colors.GetForegroundColor(),
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Examples: notepad.exe | !_win+tab | #[Notepad,readme]:(escape)?(_win+d)"
        };
        _txtCommand.TextChanged += (_, _) => SaveCurrentTab();

        var lblParameters = new Label
        {
            Text = "Parameters (optional)",
            Location = new Point(16, 98),
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = ThemeHelper.Colors.GetForegroundColor()
        };

        _txtParameters = new TextBox
        {
            Location = new Point(16, 115),
            Size = new Size(368, 23),
            BackColor = ThemeHelper.Colors.GetControlBackgroundColor(),
            ForeColor = ThemeHelper.Colors.GetForegroundColor(),
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Optional command line arguments"
        };
        _txtParameters.TextChanged += (_, _) => SaveCurrentTab();

        _chkCustom = CreateCheckBox("Enable Custom Commands", new Point(16, 145));

        customPanel.Controls.AddRange(
        [
            lblCommandName,
            _txtCommandName,
            lblCommand,
            _txtCommand,
            lblParameters,
            _txtParameters,
            _chkCustom
        ]);

        AcceptButton = btnOk;
        CancelButton = btnCancel;
        Controls.AddRange(new Control[] { updatesLink, betaLabel, btnOk, _btnApply, btnCancel, topTabsHost, _topSectionHost, tabsHost, customPanel });

        // capture base bounds and font sizes for controls so we can scale positions/sizes/fonts at runtime
        _baseClientSize = ClientSize;
        foreach (var c in GetAllControls(this))
        {
            _baseControlBounds[c] = c.Bounds;
        }

        ApplyScalingForCurrentDpi();

        HookDirtyTracking();

        LoadFromSettings();
    }
    internal ApplicationSettings Settings { get; private set; }

    private void LoadFromSettings()
    {
        _suppressDirtyTracking = true;
        _chkGlobalDelay.Checked = _workingCopy.GlobalDelayEnabled;
        _cbGlobalDelay.SelectedIndex = _workingCopy.GlobalDelayIndex;
        _chkTopLeftDelay.Checked = _workingCopy.TopLeftDelayEnabled;
        _cbTopLeftDelay.SelectedIndex = _workingCopy.TopLeftDelayIndex;
        _chkTopRightDelay.Checked = _workingCopy.TopRightDelayEnabled;
        _cbTopRightDelay.SelectedIndex = _workingCopy.TopRightDelayIndex;
        _chkBottomLeftDelay.Checked = _workingCopy.BottomLeftDelayEnabled;
        _cbBottomLeftDelay.SelectedIndex = _workingCopy.BottomLeftDelayIndex;
        _chkBottomRightDelay.Checked = _workingCopy.BottomRightDelayEnabled;
        _cbBottomRightDelay.SelectedIndex = _workingCopy.BottomRightDelayIndex;
        _chkShowCountdown.Checked = _workingCopy.ShowCountdown;
        _chkStartWithWindows.Checked = _workingCopy.StartWithWindows;
        _chkFullScreen.Checked = _workingCopy.IgnoreFullScreen;
        _chkAlwaysRunAsAdministrator.Checked = _workingCopy.AlwaysRunAsAdministrator;
        _chkAlwaysHideTrayIcon.Checked = _workingCopy.AlwaysHideTrayIcon;
        _cbFlyoutAnimationDirection.SelectedIndex = GetFlyoutAnimationDirectionIndex(_workingCopy.FlyoutAnimationDirection);
        _chkCustom.Checked = _workingCopy.EnableCustomCommands;
        SelectTopSectionTab(0);
        SelectCommandTab(0);
        UpdateDelayState();
        _suppressDirtyTracking = false;
        SetDirty(false);
    }

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
            // fallback
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

        // scale child controls positions and sizes proportionally (do not scale fonts)
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

    private void SaveAndClose()
    {
        ApplyChanges();
        DialogResult = DialogResult.OK;
        Close();
    }

    private void ApplyChanges()
    {
        if (!_isDirty)
        {
            return;
        }

        SaveCurrentTab();

        _workingCopy.GlobalDelayEnabled = _chkGlobalDelay.Checked;
        _workingCopy.GlobalDelayIndex = _cbGlobalDelay.SelectedIndex;
        _workingCopy.TopLeftDelayEnabled = _chkTopLeftDelay.Checked;
        _workingCopy.TopLeftDelayIndex = _cbTopLeftDelay.SelectedIndex;
        _workingCopy.TopRightDelayEnabled = _chkTopRightDelay.Checked;
        _workingCopy.TopRightDelayIndex = _cbTopRightDelay.SelectedIndex;
        _workingCopy.BottomLeftDelayEnabled = _chkBottomLeftDelay.Checked;
        _workingCopy.BottomLeftDelayIndex = _cbBottomLeftDelay.SelectedIndex;
        _workingCopy.BottomRightDelayEnabled = _chkBottomRightDelay.Checked;
        _workingCopy.BottomRightDelayIndex = _cbBottomRightDelay.SelectedIndex;
        _workingCopy.ShowCountdown = _chkShowCountdown.Checked;
        _workingCopy.StartWithWindows = _chkStartWithWindows.Checked;
        _workingCopy.IgnoreFullScreen = _chkFullScreen.Checked;
        _workingCopy.AlwaysRunAsAdministrator = _chkAlwaysRunAsAdministrator.Checked;
        _workingCopy.AlwaysHideTrayIcon = _chkAlwaysHideTrayIcon.Checked;
        _workingCopy.FlyoutAnimationDirection = GetSelectedFlyoutAnimationDirection();
        _workingCopy.EnableCustomCommands = _chkCustom.Checked;
        foreach (var customCommand in _workingCopy.CustomCommands)
        {
            customCommand.LaunchHidden = false;
        }

        Settings = _workingCopy.Clone();
        SettingsApplied?.Invoke(Settings.Clone());
        SetDirty(false);
    }

    private void UpdateDelayState()
    {
        _cbGlobalDelay.Enabled = _chkGlobalDelay.Checked;

        var enablePerCorner = !_chkGlobalDelay.Checked;
        _chkTopLeftDelay.Enabled = enablePerCorner;
        _chkTopRightDelay.Enabled = enablePerCorner;
        _chkBottomLeftDelay.Enabled = enablePerCorner;
        _chkBottomRightDelay.Enabled = enablePerCorner;

        _cbTopLeftDelay.Enabled = enablePerCorner && _chkTopLeftDelay.Checked;
        _cbTopRightDelay.Enabled = enablePerCorner && _chkTopRightDelay.Checked;
        _cbBottomLeftDelay.Enabled = enablePerCorner && _chkBottomLeftDelay.Checked;
        _cbBottomRightDelay.Enabled = enablePerCorner && _chkBottomRightDelay.Checked;
    }

    private void LoadCommandTab()
    {
        if (_selectedCommandIndex < 0)
        {
            return;
        }

        var command = _workingCopy.CustomCommands[_selectedCommandIndex];
        _suppressDirtyTracking = true;
        _txtCommandName.PlaceholderText = $"Command {_selectedCommandIndex + 1}";
        _txtCommandName.Text = command.Name;
        _txtCommand.Text = command.Command;
        _txtParameters.Text = command.Parameters;
        _suppressDirtyTracking = false;
    }

    private void SaveCurrentTab()
    {
        if (_selectedCommandIndex < 0 || _suppressDirtyTracking)
        {
            return;
        }

        var command = _workingCopy.CustomCommands[_selectedCommandIndex];
        command.Name = _txtCommandName.Text.Trim();
        command.Command = _txtCommand.Text;
        command.Parameters = _txtParameters.Text;
        command.LaunchHidden = false;
    }

    private void SelectTopSectionTab(int index)
    {
        if (index < 0 || index > 1)
        {
            return;
        }

        _selectedTopSectionTab = index;
        _delayPanel.Visible = index == 0;
        _otherPanel.Visible = index == 1;
        UpdateTopSectionTabStyles();
    }

    private void UpdateTopSectionTabStyles()
    {
        for (var i = 0; i < _topSectionTabs.Length; i++)
        {
            var selected = i == _selectedTopSectionTab;
            _topSectionTabs[i].Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            _topSectionTabs[i].BackColor = selected ? ThemeHelper.Colors.GetSettingsSelectedTabBackgroundColor() : ThemeHelper.Colors.GetSettingsBackgroundColor();
            _topSectionTabs[i].ForeColor = selected ? ThemeHelper.Colors.GetSettingsSelectedTabForegroundColor() : ThemeHelper.Colors.GetSettingsUnselectedTabForegroundColor();
        }
    }

    private void SelectCommandTab(int index)
    {
        if (index < 0 || index >= _commandTabs.Length)
        {
            return;
        }

        SaveCurrentTab();
        _selectedCommandIndex = index;
        UpdateCommandTabStyles();
        LoadCommandTab();
    }

    private void UpdateCommandTabStyles()
    {
        for (var i = 0; i < _commandTabs.Length; i++)
        {
            var selected = i == _selectedCommandIndex;
            _commandTabs[i].Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            _commandTabs[i].BackColor = selected ? ThemeHelper.Colors.GetSettingsSelectedTabBackgroundColor() : ThemeHelper.Colors.GetSettingsBackgroundColor();
            _commandTabs[i].ForeColor = selected ? ThemeHelper.Colors.GetSettingsSelectedTabForegroundColor() : ThemeHelper.Colors.GetSettingsUnselectedTabForegroundColor();
        }
    }

    private static ComboBox CreateDelayCombo(Point location)
    {
        var combo = new ComboBox
        {
            Location = location,
            Size = new Size(DelayPanelComboWidth, 23),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = ThemeHelper.Colors.GetControlBackgroundColor(),
            ForeColor = ThemeHelper.Colors.GetForegroundColor(),
            FlatStyle = FlatStyle.Flat
        };

        combo.Items.AddRange(DelayValues);
        combo.SelectedIndex = 3;
        return combo;
    }

    private static CheckBox CreateAlignedDelayCheckBox(string text, int x, int y)
    {
        var checkBox = CreateCheckBox(text, new Point(x, y));
        checkBox.AutoSize = false;
        checkBox.Size = new Size(DelayPanelColumnLabelWidth, 20);
        return checkBox;
    }

    private static CheckBox CreateCheckBox(string text, Point location)
    {
        return new CheckBox
        {
            Text = text,
            AutoSize = true,
            Location = location,
            BackColor = Color.Transparent,
            ForeColor = ThemeHelper.Colors.GetForegroundColor()
        };
    }

    private void HookDirtyTracking()
    {
        foreach (var checkBox in new[] { _chkGlobalDelay, _chkTopLeftDelay, _chkTopRightDelay, _chkBottomLeftDelay, _chkBottomRightDelay, _chkShowCountdown, _chkStartWithWindows, _chkFullScreen, _chkAlwaysRunAsAdministrator, _chkAlwaysHideTrayIcon, _chkCustom })
        {
            checkBox.CheckedChanged += (_, _) => MarkDirty();
        }

        foreach (var comboBox in new[] { _cbGlobalDelay, _cbTopLeftDelay, _cbTopRightDelay, _cbBottomLeftDelay, _cbBottomRightDelay, _cbFlyoutAnimationDirection })
        {
            comboBox.SelectedIndexChanged += (_, _) => MarkDirty();
        }

        _txtCommandName.TextChanged += (_, _) => MarkDirty();
        _txtCommand.TextChanged += (_, _) => MarkDirty();
        _txtParameters.TextChanged += (_, _) => MarkDirty();
    }

    private static int GetFlyoutAnimationDirectionIndex(FlyoutAnimationDirection direction)
    {
        for (var index = 0; index < FlyoutAnimationDirections.Length; index++)
        {
            if (FlyoutAnimationDirections[index].Value == direction)
            {
                return index;
            }
        }

        return 1;
    }
    private FlyoutAnimationDirection GetSelectedFlyoutAnimationDirection()
    {
        var selectedIndex = _cbFlyoutAnimationDirection.SelectedIndex;
        return selectedIndex >= 0 && selectedIndex < FlyoutAnimationDirections.Length
            ? FlyoutAnimationDirections[selectedIndex].Value
            : FlyoutAnimationDirection.Bottom;
    }

    internal void PositionNearTaskbarTray()
    {
        const int margin = 12;

        var screen = Screen.PrimaryScreen ?? Screen.FromPoint(Cursor.Position);
        var workArea = screen.WorkingArea;
        var taskbarInfo = TaskbarInfo.TryGetPrimaryTaskbar() ?? new TaskbarInfo(screen.Bounds, TaskbarEdge.Bottom);

        Location = taskbarInfo.Edge switch
        {
            TaskbarEdge.Left => new Point(workArea.Left + margin, workArea.Bottom - Height - margin),
            TaskbarEdge.Right => new Point(workArea.Right - Width - margin, workArea.Bottom - Height - margin),
            TaskbarEdge.Top => new Point(workArea.Right - Width - margin, workArea.Top + margin),
            _ => new Point(workArea.Right - Width - margin, workArea.Bottom - Height - margin)
        };
    }

    private void MarkDirty()
    {
        if (_suppressDirtyTracking)
        {
            return;
        }

        SetDirty(true);
    }

    private void SetDirty(bool isDirty)
    {
        _isDirty = isDirty;
        _btnApply.Enabled = isDirty;
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
