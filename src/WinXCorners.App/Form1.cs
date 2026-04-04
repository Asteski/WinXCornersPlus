using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WinXCorners.App;

public partial class Form1 : Form
{
    private const string CornerMenuSeparatorActionId = "__separator__";
    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new(-2);
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoOwnerZOrder = 0x0200;

    private enum FlyoutAnimationStyle
    {
        DirectionalFade,
        Windows11EdgeSlide,
        OriginalLegacy
    }

    private const int MonitorTileCornerRadius = 6;
    private const int FlyoutCornerRadius = MonitorTileCornerRadius;
    private const int HotCornerActivationSize = 1;
    private const int HotCornerRearmReleaseSize = 32;
    private static readonly TimeSpan FlyoutAnimationDuration = TimeSpan.FromMilliseconds(220);
    private static readonly TimeSpan LegacyFlyoutOpenDuration = TimeSpan.FromMilliseconds(211);
    private static readonly TimeSpan LegacyFlyoutCloseDuration = TimeSpan.FromMilliseconds(229);
    private const int FlyoutAnimationOffset = 18;
    private const double Windows11StartingOpacity = 0.92;
    private static readonly FlyoutAnimationStyle ActiveFlyoutAnimationStyle = FlyoutAnimationStyle.OriginalLegacy;

    private readonly NotifyIcon _notifyIcon;
    private readonly FlyoutActionControl _topLeftButton;
    private readonly FlyoutActionControl _topRightButton;
    private readonly FlyoutActionControl _bottomLeftButton;
    private readonly FlyoutActionControl _bottomRightButton;
    private readonly AccentTileControl _monitorTile;
    private readonly System.Windows.Forms.Timer _fadeTimer;
    private readonly System.Windows.Forms.Timer _hotCornerTimer;
    private readonly System.Windows.Forms.Timer _settingsSaveTimer;
    private readonly CountdownOverlay _countdownOverlay;
    private readonly GlobalMouseHook _globalMouseHook;
    private readonly Stopwatch _animationStopwatch = new();
    private Icon? _currentTrayIcon;
    private AdvancedForm? _advancedWindow;
    private LogWindowForm? _logWindow;
    private ApplicationSettings _settings;
    private FlyoutActionControl? _activeCornerButton;
    private bool _fadeOutPending;
    private DateTime _lastDeactivateCloseAtUtc = DateTime.MinValue;
    private HotCornerArea _currentHotCornerArea = HotCornerArea.None;
    private HotCornerArea _latchedHotCornerArea = HotCornerArea.None;
    private Rectangle _latchedHotCornerBounds = Rectangle.Empty;
    private DateTime _hotCornerEnteredAtUtc = DateTime.MinValue;
    private bool _hotCornerTriggered;
    private DateTime _lastFullScreenBlockedLogAtUtc = DateTime.MinValue;
    private bool _rawInputRegistered;
    private bool _isCornerMenuOpen;
    private bool _trayHiddenByUser;
    private bool _lastKnownLightTheme;
    private Color _lastKnownAccentColor;
    private Point _animationStartLocation;
    private Point _animationTargetLocation;
    private TaskbarEdge _animationEdge = TaskbarEdge.Bottom;
    private bool _highResolutionTimerRequested;
    private bool _animationFrameQueued;
    private bool _trayIconRefreshQueued;
    private double _scaleFactor = 1.0;
    private Size _baseClientSize = Size.Empty;
    private Size _baseMonitorTileSize = Size.Empty;
    private Size _baseCornerButtonSize = Size.Empty;
    

    internal Form1(ApplicationSettings? initialSettings = null)
    {
        InitializeComponent();
        _settings = initialSettings?.Clone() ?? SettingsStore.Load();
        _lastKnownLightTheme = ThemeHelper.IsLightTheme;
        _lastKnownAccentColor = ThemeHelper.Colors.GetSystemAccentColor();
        ThemeHelper.ApplyNativeMenuTheme();
        SystemEvents.UserPreferenceChanged += SystemEventsOnUserPreferenceChanged;
        AppLogger.EntryAdded += OnLogEntryAdded;
        AppLogger.Log("Application started");

        Text = "WinXCornersPlus";
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = ThemeHelper.Colors.GetFlyoutBackgroundColor();
        Padding = new Padding(0);
        DoubleBuffered = true;
        ClientSize = new Size(424, 228);
        Icon = WindowIconLoader.TryLoadAppIcon();
        Opacity = 0;
        Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;

            var bounds = GetFlyoutBounds();
            using var backgroundPath = RoundedRectangleGeometry.CreatePath(bounds, FlyoutCornerRadius);
            using var backgroundBrush = new SolidBrush(ThemeHelper.Colors.GetFlyoutBackgroundColor());
            e.Graphics.FillPath(backgroundBrush, backgroundPath);

            using var pen = new Pen(ThemeHelper.Colors.GetFlyoutBorderColor(), 1f)
            {
                Alignment = PenAlignment.Inset
            };
            using var borderPath = RoundedRectangleGeometry.CreatePath(bounds, FlyoutCornerRadius);
            e.Graphics.DrawPath(pen, borderPath);
        };
        UpdateFlyoutRegion();

        _notifyIcon = new NotifyIcon(components!)
        {
            Text = "WinXCornersPlus",
            Visible = false
        };
        UpdateTrayIcon();
        UpdateTrayIconVisibility();
        _notifyIcon.MouseUp += NotifyIconOnMouseUp;

        _fadeTimer = new System.Windows.Forms.Timer(components!)
        {
            Interval = 4
        };
        _fadeTimer.Tick += FadeTimerOnTick;

        _hotCornerTimer = new System.Windows.Forms.Timer(components!)
        {
            Interval = 50
        };
        _hotCornerTimer.Tick += HotCornerTimerOnTick;

        _settingsSaveTimer = new System.Windows.Forms.Timer(components!)
        {
            Interval = 180
        };
        _settingsSaveTimer.Tick += SettingsSaveTimerOnTick;

        _countdownOverlay = new CountdownOverlay();
        _globalMouseHook = new GlobalMouseHook();
        _globalMouseHook.MouseMoved += GlobalMouseHookOnMouseMoved;

        _topLeftButton = CreateCornerButton(string.Empty);
        _topLeftButton.Tag = _settings.TopLeftActionId;
        _topRightButton = CreateCornerButton(string.Empty);
        _topRightButton.Tag = _settings.TopRightActionId;
        _bottomLeftButton = CreateCornerButton("All Windows");
        _bottomLeftButton.Tag = _settings.BottomLeftActionId;
        _bottomRightButton = CreateCornerButton("Start Menu");
        _bottomRightButton.Tag = _settings.BottomRightActionId;
        _monitorTile = new AccentTileControl
        {
            Size = new Size(171, 96),
            CornerRadius = MonitorTileCornerRadius,
            AccentColor = _lastKnownAccentColor,
            Font = ThemeHelper.Colors.GetFlyoutNumberFont(),
            Text = "1",
            ForeColor = Color.White
        };

        _topLeftButton.Click += (_, _) => ShowCornerMenu(_topLeftButton, preferAbove: false);
        _topRightButton.Click += (_, _) => ShowCornerMenu(_topRightButton, preferAbove: false);
        _bottomLeftButton.Click += (_, _) => ShowCornerMenu(_bottomLeftButton, preferAbove: true);
        _bottomRightButton.Click += (_, _) => ShowCornerMenu(_bottomRightButton, preferAbove: true);

        Controls.AddRange([
            _topLeftButton,
            _topRightButton,
            _bottomLeftButton,
            _bottomRightButton,
            _monitorTile
        ]);

        // Capture base sizes and fonts for later scaling
        _baseClientSize = ClientSize;
        _baseMonitorTileSize = _monitorTile.Size;
        _baseCornerButtonSize = _topLeftButton.Size;
        

        ApplyScalingForCurrentDpi();

        Resize += (_, _) =>
        {
            LayoutCornerButtons();
            UpdateFlyoutRegion();
        };
        Shown += (_, _) => Hide();
        Deactivate += (_, _) => HideFlyoutIfNeeded();
        FormClosing += OnFormClosing;
        ApplySettings();
        LayoutCornerButtons();
        _hotCornerTimer.Start();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        UpdateFlyoutRegion();
        ThemeHelper.ApplyNativeWindowTheme(Handle);

        if (_rawInputRegistered)
        {
            return;
        }

        var device = new RawInputDevice
        {
            UsagePage = 0x01,
            Usage = 0x02,
            Flags = 0x00000100,
            Target = Handle
        };

        _rawInputRegistered = RegisterRawInputDevices([device], 1, (uint)Marshal.SizeOf<RawInputDevice>());
    }

    protected override void WndProc(ref Message m)
    {
        const int wmDpiChanged = 0x02E0;
        const int wmInput = 0x00FF;
        const int wmSettingChange = 0x001A;
        const int wmThemeChanged = 0x031A;
        const int wmDwmColorizationColorChanged = 0x0320;
        if (m.Msg == wmDpiChanged)
        {
            ApplyScalingForCurrentDpi();
        }
        if (m.Msg == wmInput)
        {
            EvaluateHotCorner(Cursor.Position);
        }
        else if (m.Msg == wmSettingChange || m.Msg == wmThemeChanged || m.Msg == wmDwmColorizationColorChanged)
        {
            RefreshThemeIfChanged();
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
            // fall through to fallback
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

        // Scale client size
        ClientSize = new Size((int)Math.Round(_baseClientSize.Width * _scaleFactor), (int)Math.Round(_baseClientSize.Height * _scaleFactor));

        // Scale corner buttons and monitor tile sizes
        _monitorTile.Size = new Size((int)Math.Round(_baseMonitorTileSize.Width * _scaleFactor), (int)Math.Round(_baseMonitorTileSize.Height * _scaleFactor));
        foreach (var b in new[] { _topLeftButton, _topRightButton, _bottomLeftButton, _bottomRightButton })
        {
            b.Size = new Size((int)Math.Round(_baseCornerButtonSize.Width * _scaleFactor), (int)Math.Round(_baseCornerButtonSize.Height * _scaleFactor));
        }

        LayoutCornerButtons();
        UpdateFlyoutRegion();
    }

    private void SystemEventsOnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(RefreshThemeIfChanged));
            return;
        }

        RefreshThemeIfChanged();
    }

    private void RefreshThemeIfChanged()
    {
        var isLightTheme = ThemeHelper.IsLightTheme;
        var accentColor = ThemeHelper.Colors.GetSystemAccentColor();
        if (isLightTheme == _lastKnownLightTheme && accentColor == _lastKnownAccentColor)
        {
            return;
        }

        _lastKnownLightTheme = isLightTheme;
        _lastKnownAccentColor = accentColor;
        ThemeHelper.ApplyNativeMenuTheme();
        ThemeHelper.ApplyNativeWindowTheme(Handle);
        BackColor = ThemeHelper.Colors.GetFlyoutBackgroundColor();
        _monitorTile.AccentColor = accentColor;
        _countdownOverlay.RefreshTheme();
        UpdateTrayIcon();
        Invalidate(true);
        if (Visible)
        {
            Refresh();
        }
    }

    private static IReadOnlyList<(string text, string actionId)> GetCornerMenuItems(ApplicationSettings settings, Func<int, string> customCommandNameFactory)
    {
        var builtInItems = new List<(string text, string actionId)>
        {
            ("File Explorer", "file-explorer"),
            ("System Settings", "settings"),
            ("Task Manager", "task-manager"),
            ("Show All Windows", "all-windows"),
            ("Show Desktop", "desktop"),
            ("Start Screen Saver", "screen-saver"),
            ("Turn Display Off", "monitors-off"),
            ("Action Center", "action-center"),
            ("Notification Center", "notification-center"),
            ("Lock Screen", "lock-screen"),
            ("Start Menu", "start-menu")
        };

        builtInItems.Add(("Hide Other Windows", "hide-other-windows"));
        builtInItems.Sort(static (left, right) => StringComparer.CurrentCultureIgnoreCase.Compare(left.text, right.text));

        var items = new List<(string text, string actionId)>
        {
            ("None", "none")
        };

        items.AddRange(builtInItems);

        if (settings.EnableCustomCommands)
        {
            var customItems = new List<(string text, string actionId)>
            {
                (customCommandNameFactory(0), "custom-1"),
                (customCommandNameFactory(1), "custom-2"),
                (customCommandNameFactory(2), "custom-3"),
                (customCommandNameFactory(3), "custom-4")
            };

            customItems.Sort(static (left, right) => StringComparer.CurrentCultureIgnoreCase.Compare(left.text, right.text));
            items.Add((string.Empty, CornerMenuSeparatorActionId));
            items.AddRange(customItems);
        }

        return items;
    }

    private static FlyoutActionControl CreateCornerButton(string text)
    {
        return new FlyoutActionControl
        {
            Text = text,
            Size = new Size(148, 28)
        };
    }

    private void UpdateFlyoutRegion()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        using var path = RoundedRectangleGeometry.CreatePath(GetFlyoutBounds(), FlyoutCornerRadius);
        var updatedRegion = new Region(path);
        var previousRegion = Region;
        Region = updatedRegion;
        previousRegion?.Dispose();
    }

    private RectangleF GetFlyoutBounds()
    {
        return new RectangleF(1.5f, 1.5f, Width - 3f, Height - 3f);
    }

    private void LayoutCornerButtons()
    {
        var leftX = (int)Math.Round(16 * _scaleFactor);
        var rightX = (int)Math.Round(258 * _scaleFactor);
        var topY = (int)Math.Round(17 * _scaleFactor);
        var monitorTop = (int)Math.Round(66 * _scaleFactor);

        _topLeftButton.Location = new Point(leftX, topY);
        _topRightButton.Location = new Point(rightX, topY);
        _monitorTile.Location = new Point((ClientSize.Width - _monitorTile.Width) / 2, monitorTop);

        var topSectionGap = _monitorTile.Top - (_topLeftButton.Bottom);
        var bottomRowY = _monitorTile.Bottom + topSectionGap;

        _bottomLeftButton.Location = new Point(leftX, bottomRowY);
        _bottomRightButton.Location = new Point(rightX, bottomRowY);
    }

    private void ApplySettings()
    {
        UpdateCornerButtonLabels();
        UpdateTrayIcon();
        UpdateTrayIconVisibility();
        SettingsStore.Save(_settings);
        UpdateAutorunRegistration();
    }

    private string GetCustomCommandDisplayName(int index)
    {
        var customCommand = _settings.CustomCommands[index];
        var name = customCommand.Name.Trim();

        if (string.IsNullOrWhiteSpace(name) ||
            string.Equals(name, $"Custom Command {index + 1}", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, $"Command {index + 1}", StringComparison.OrdinalIgnoreCase))
        {
            return $"Command {index + 1}";
        }

        return name;
    }

    private void UpdateCornerButtonLabels()
    {
        UpdateCornerButtonLabel(_topLeftButton);
        UpdateCornerButtonLabel(_topRightButton);
        UpdateCornerButtonLabel(_bottomLeftButton);
        UpdateCornerButtonLabel(_bottomRightButton);
    }

    private void UpdateCornerButtonLabel(FlyoutActionControl button)
    {
        if (button.Tag is not string actionId)
        {
            return;
        }

        button.Text = GetActionDisplayText(actionId);
    }

    private void ShowCornerMenu(FlyoutActionControl source, bool preferAbove)
    {
        if (!_settings.HotCornersEnabled)
        {
            return;
        }

        var cornerItems = GetCornerMenuItems(_settings, GetCustomCommandDisplayName);
        var menuHeight = 8 + (cornerItems.Count * 24);
        var menuWidth = 220;
        var sourceScreen = source.PointToScreen(Point.Empty);
        var workArea = Screen.FromControl(this).WorkingArea;

        var menuTop = PopupPlacementCalculator.CalculateTop(
            sourceScreen.Y,
            menuHeight,
            workArea.Top,
            workArea.Bottom,
            preferAbove);

        var menuLeft = sourceScreen.X;
        menuLeft = Math.Clamp(menuLeft, workArea.Left, Math.Max(workArea.Left, workArea.Right - menuWidth));

        _activeCornerButton = source;
        _isCornerMenuOpen = true;
        string? actionId;
        try
        {
            actionId = Win32PopupMenu.ShowCornerMenu(Handle, new Point(menuLeft, menuTop), cornerItems);
        }
        finally
        {
            _isCornerMenuOpen = false;
        }

        if (actionId is null || _activeCornerButton is null)
        {
            return;
        }

        _activeCornerButton.Tag = actionId;
        _activeCornerButton.Text = GetActionDisplayText(actionId);
        SaveCornerAssignment(_activeCornerButton, actionId);
        ScheduleSettingsSave();
    }

    private void ScheduleSettingsSave()
    {
        _settingsSaveTimer.Stop();
        _settingsSaveTimer.Start();
    }

    private void SettingsSaveTimerOnTick(object? sender, EventArgs e)
    {
        _settingsSaveTimer.Stop();
        SettingsStore.Save(_settings);
    }

    private void SaveCornerAssignment(FlyoutActionControl button, string actionId)
    {
        if (button == _topLeftButton)
        {
            _settings.TopLeftActionId = actionId;
        }
        else if (button == _topRightButton)
        {
            _settings.TopRightActionId = actionId;
        }
        else if (button == _bottomLeftButton)
        {
            _settings.BottomLeftActionId = actionId;
        }
        else if (button == _bottomRightButton)
        {
            _settings.BottomRightActionId = actionId;
        }
    }

    private string GetActionDisplayText(string actionId)
    {
        return actionId switch
        {
            "none" => string.Empty,
            "file-explorer" => "File Explorer",
            "settings" => "System Settings",
            "task-manager" => "Task Manager",
            "screen-saver" => "Screen Saver",
            "hide-other-windows" => "Hide Windows",
            "custom-1" => GetCustomCommandDisplayName(0),
            "custom-2" => GetCustomCommandDisplayName(1),
            "custom-3" => GetCustomCommandDisplayName(2),
            "custom-4" => GetCustomCommandDisplayName(3),
            "all-windows" => "All Windows",
            "desktop" => "Show Desktop",
            "monitors-off" => "Display Off",
            "action-center" => "Action Center",
            "notification-center" => "Notification Center",
            "lock-screen" => "Lock Screen",
            "start-menu" => "Start Menu",
            _ => actionId
        };
    }

    private void NotifyIconOnMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            var commandId = Win32PopupMenu.ShowTrayMenu(Handle, Cursor.Position, _settings.HotCornersEnabled, ElevationHelper.IsProcessElevated());
            HandleTrayMenuCommand(commandId);
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            // If this same click already caused deactivation-close, don't reopen immediately.
            if (!Visible && DateTime.UtcNow - _lastDeactivateCloseAtUtc < TimeSpan.FromMilliseconds(250))
            {
                return;
            }

            ToggleFlyout();
        }
    }

    private void RestartApplication()
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = Application.ExecutablePath,
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(startInfo);
        }
        catch
        {
            // ignore failures to start
        }

        // exit current process
        Environment.Exit(0);
    }

    private void HandleTrayMenuCommand(int commandId)
    {
        switch (commandId)
        {
            case Win32PopupMenu.TrayCommandReload:
                RestartApplication();
                break;
            case Win32PopupMenu.TrayCommandToggleHotCorners:
                _settings.HotCornersEnabled = !_settings.HotCornersEnabled;
                ApplySettings();
                break;
            case Win32PopupMenu.TrayCommandHideTray:
                HideTrayIcon();
                break;
            case Win32PopupMenu.TrayCommandElevate:
                ElevateApplication();
                break;
            case Win32PopupMenu.TrayCommandAdvanced:
                OpenAdvancedDialog();
                break;
            case Win32PopupMenu.TrayCommandLogWindow:
                ShowLogWindowPlaceholder();
                break;
            case Win32PopupMenu.TrayCommandAbout:
                ShowAbout();
                break;
            case Win32PopupMenu.TrayCommandExit:
                ExitApplication();
                break;
        }
    }

    private void ToggleFlyout()
    {
        if (IsSettingsWindowOpen())
        {
            return;
        }

        if (Visible)
        {
            BeginFadeOut();
            return;
        }

        BeginFadeIn();
        Activate();
    }

    private void BeginFadeIn()
    {
        if (IsSettingsWindowOpen())
        {
            return;
        }

        EnsureFlyoutTopMost(false);
        _fadeOutPending = false;
        _fadeTimer.Stop();
        var placement = GetFlyoutPlacement();
        _animationEdge = placement.edge;
        _animationTargetLocation = placement.location;
        _animationStartLocation = GetAnimatedOffsetLocation(placement.location, placement.taskbarInfo);
        RequestHighResolutionTimer();
        _animationStopwatch.Restart();
        Location = _animationStartLocation;
        Opacity = 0;
        Show();
        QueueAnimationFrame();
    }

    private void BeginFadeOut()
    {
        if (!Visible)
        {
            return;
        }

        if (_fadeOutPending && _fadeTimer.Enabled)
        {
            return;
        }

        _fadeOutPending = true;
        _fadeTimer.Stop();
        EnsureFlyoutTopMost(false);
        var placement = GetFlyoutPlacement();
        _animationEdge = placement.edge;
        _animationStartLocation = Location;
        _animationTargetLocation = GetAnimatedOffsetLocation(placement.location, placement.taskbarInfo);
        RequestHighResolutionTimer();
        _animationStopwatch.Restart();
        QueueAnimationFrame();
    }

    private void FadeTimerOnTick(object? sender, EventArgs e)
    {
        AdvanceAnimationFrame();
    }

    private void AdvanceAnimationFrame()
    {
        _animationFrameQueued = false;

        if (!_animationStopwatch.IsRunning)
        {
            _animationStopwatch.Restart();
        }

        var elapsed = _animationStopwatch.Elapsed;
        var duration = GetAnimationDuration(_fadeOutPending);
        var progress = Math.Clamp(elapsed.TotalMilliseconds / duration.TotalMilliseconds, 0, 1);
        var moveProgress = GetMoveProgress(progress, _fadeOutPending);
        var opacity = GetAnimatedOpacity(progress, _fadeOutPending);

        Location = InterpolatePoint(_animationStartLocation, _animationTargetLocation, moveProgress);
        Opacity = opacity;

        if (_fadeOutPending)
        {
            if (progress >= 1)
            {
                _fadeTimer.Stop();
                Location = _animationTargetLocation;
                Opacity = 0;
                Hide();
                ResetAnimationState();
                return;
            }

            QueueAnimationFrame();
            return;
        }

        if (progress >= 1)
        {
            _fadeTimer.Stop();
            Location = _animationTargetLocation;
            Opacity = 1;
            EnsureFlyoutTopMost(true);
            ResetAnimationState();
            return;
        }

        QueueAnimationFrame();
    }

    private void QueueAnimationFrame()
    {
        if (_animationFrameQueued || !IsHandleCreated)
        {
            return;
        }

        _animationFrameQueued = true;
        BeginInvoke(new Action(AdvanceAnimationFrame));
    }

    private void PositionFlyout()
    {
        Location = GetFlyoutPlacement().location;
    }

    private (Point location, TaskbarEdge edge, TaskbarInfo taskbarInfo) GetFlyoutPlacement()
    {
        const int margin = 12;
        const int horizontalOffset = 1;

        var screen = Screen.PrimaryScreen ?? Screen.FromPoint(Cursor.Position);
        var workArea = screen.WorkingArea;
        var taskbarInfo = TaskbarInfo.TryGetPrimaryTaskbar() ?? new TaskbarInfo(workArea, TaskbarEdge.Bottom);

        var location = taskbarInfo.Edge switch
        {
            TaskbarEdge.Top => new Point(workArea.Right - Width - margin + horizontalOffset, workArea.Top + margin),
            TaskbarEdge.Left => new Point(workArea.Left + margin + horizontalOffset, workArea.Bottom - Height - margin),
            TaskbarEdge.Right => new Point(workArea.Right - Width - margin + horizontalOffset, workArea.Bottom - Height - margin),
            _ => new Point(workArea.Right - Width - margin + horizontalOffset, workArea.Bottom - Height - margin)
        };

        return (location, taskbarInfo.Edge, taskbarInfo);
    }

    private Point GetAnimatedOffsetLocation(Point anchorLocation, TaskbarInfo taskbarInfo)
    {
        var direction = _settings.FlyoutAnimationDirection switch
        {
            FlyoutAnimationDirection.Top => FlyoutAnimationDirection.Top,
            FlyoutAnimationDirection.Left => FlyoutAnimationDirection.Left,
            FlyoutAnimationDirection.Right => FlyoutAnimationDirection.Right,
            FlyoutAnimationDirection.Bottom => FlyoutAnimationDirection.Bottom,
            _ => taskbarInfo.Edge switch
            {
                TaskbarEdge.Top => FlyoutAnimationDirection.Top,
                TaskbarEdge.Left => FlyoutAnimationDirection.Left,
                TaskbarEdge.Right => FlyoutAnimationDirection.Right,
                _ => FlyoutAnimationDirection.Bottom
            }
        };

        if (direction == FlyoutAnimationDirection.Bottom && taskbarInfo.Edge == TaskbarEdge.Bottom)
        {
            return new Point(anchorLocation.X, taskbarInfo.Bounds.Bottom);
        }

        if (direction == FlyoutAnimationDirection.Top && taskbarInfo.Edge == TaskbarEdge.Top)
        {
            return new Point(anchorLocation.X, taskbarInfo.Bounds.Top - Height);
        }

        if (direction == FlyoutAnimationDirection.Left && taskbarInfo.Edge == TaskbarEdge.Left)
        {
            return new Point(taskbarInfo.Bounds.Left - Width, anchorLocation.Y);
        }

        if (direction == FlyoutAnimationDirection.Right && taskbarInfo.Edge == TaskbarEdge.Right)
        {
            return new Point(taskbarInfo.Bounds.Right, anchorLocation.Y);
        }

        var offset = GetAnimationOffset(direction);

        return direction switch
        {
            FlyoutAnimationDirection.Top => new Point(anchorLocation.X, anchorLocation.Y - offset),
            FlyoutAnimationDirection.Left => new Point(anchorLocation.X - offset, anchorLocation.Y),
            FlyoutAnimationDirection.Right => new Point(anchorLocation.X + offset, anchorLocation.Y),
            _ => new Point(anchorLocation.X, anchorLocation.Y + offset)
        };
    }

    private TimeSpan GetAnimationDuration(bool closing)
    {
        return ActiveFlyoutAnimationStyle == FlyoutAnimationStyle.OriginalLegacy
            ? (closing ? LegacyFlyoutCloseDuration : LegacyFlyoutOpenDuration)
            : FlyoutAnimationDuration;
    }

    private int GetAnimationOffset(FlyoutAnimationDirection direction)
    {
        if (ActiveFlyoutAnimationStyle != FlyoutAnimationStyle.OriginalLegacy)
        {
            return FlyoutAnimationOffset;
        }

        return direction is FlyoutAnimationDirection.Top or FlyoutAnimationDirection.Bottom
            ? Height + 1
            : Width + 1;
    }

    private static double GetMoveProgress(double progress, bool closing)
    {
        return ActiveFlyoutAnimationStyle switch
        {
            FlyoutAnimationStyle.OriginalLegacy => closing ? EaseInQuad(progress) : EaseOutExpo(progress),
            FlyoutAnimationStyle.Windows11EdgeSlide => closing ? EaseInQuint(progress) : EaseOutQuint(progress),
            _ => closing ? EaseInCubic(progress) : EaseOutCubic(progress)
        };
    }

    private static double GetAnimatedOpacity(double progress, bool closing)
    {
        return ActiveFlyoutAnimationStyle switch
        {
            FlyoutAnimationStyle.OriginalLegacy => closing
                ? 1 - EaseInQuad(Math.Max(0, (progress - 0.45) / 0.55))
                : EaseOutQuad(Math.Min(1, progress / 0.7)),
            FlyoutAnimationStyle.Windows11EdgeSlide => closing
                ? Lerp(1, Windows11StartingOpacity, EaseInQuad(progress))
                : Lerp(Windows11StartingOpacity, 1, EaseOutQuad(Math.Min(1, progress / 0.55))),
            _ => closing ? 1 - EaseInCubic(progress) : EaseOutCubic(progress)
        };
    }

    private static Point InterpolatePoint(Point start, Point end, double progress)
    {
        var x = start.X + (int)Math.Round((end.X - start.X) * progress);
        var y = start.Y + (int)Math.Round((end.Y - start.Y) * progress);
        return new Point(x, y);
    }

    private static double Lerp(double start, double end, double progress)
    {
        return start + ((end - start) * progress);
    }

    private static double EaseOutQuad(double value)
    {
        return 1 - ((1 - value) * (1 - value));
    }

    private static double EaseInQuad(double value)
    {
        return value * value;
    }

    private static double EaseOutCubic(double value)
    {
        var inverse = 1 - value;
        return 1 - (inverse * inverse * inverse);
    }

    private static double EaseInCubic(double value)
    {
        return value * value * value;
    }

    private static double EaseOutQuint(double value)
    {
        var inverse = 1 - value;
        return 1 - (inverse * inverse * inverse * inverse * inverse);
    }

    private static double EaseOutExpo(double value)
    {
        return value >= 1 ? 1 : 1 - Math.Pow(2, -10 * value);
    }

    private static double EaseInQuint(double value)
    {
        return value * value * value * value * value;
    }

    private void ResetAnimationState()
    {
        _fadeTimer.Stop();
        _animationFrameQueued = false;
        _animationStopwatch.Reset();
        ReleaseHighResolutionTimer();
    }

    private void EnsureFlyoutTopMost(bool topMost)
    {
        if (TopMost == topMost && IsHandleCreated)
        {
            return;
        }

        TopMost = topMost;
        if (!IsHandleCreated)
        {
            return;
        }

        SetWindowPos(
            Handle,
            topMost ? HWND_TOPMOST : HWND_NOTOPMOST,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoActivate | SwpNoOwnerZOrder);
    }

    private void RequestHighResolutionTimer()
    {
        if (_highResolutionTimerRequested)
        {
            return;
        }

        try
        {
            _highResolutionTimerRequested = timeBeginPeriod(1) == 0;
        }
        catch
        {
            _highResolutionTimerRequested = false;
        }
    }

    private void ReleaseHighResolutionTimer()
    {
        if (!_highResolutionTimerRequested)
        {
            return;
        }

        try
        {
            timeEndPeriod(1);
        }
        catch
        {
        }

        _highResolutionTimerRequested = false;
    }

    private void HotCornerTimerOnTick(object? sender, EventArgs e)
    {
        EvaluateHotCorner(Cursor.Position);
    }

    private void GlobalMouseHookOnMouseMoved(Point cursor)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => EvaluateHotCorner(cursor)));
            return;
        }

        EvaluateHotCorner(cursor);
    }

    private void EvaluateHotCorner(Point cursor)
    {
        var screen = Screen.FromPoint(cursor);
        var area = GetHotCornerArea(cursor, screen.Bounds, HotCornerActivationSize);

        if (_latchedHotCornerArea != HotCornerArea.None)
        {
            if (IsWithinLatchedCornerReleaseZone(cursor, _latchedHotCornerArea, _latchedHotCornerBounds, HotCornerRearmReleaseSize))
            {
                ResetHotCornerState();
                return;
            }

            _latchedHotCornerArea = HotCornerArea.None;
            _latchedHotCornerBounds = Rectangle.Empty;
        }

        var flyoutIsOpen = Visible && Opacity > 0.05;
        if (!_settings.HotCornersEnabled || flyoutIsOpen || _isCornerMenuOpen)
        {
            ResetHotCornerState();
            return;
        }

        if ((Control.MouseButtons & (MouseButtons.Left | MouseButtons.Middle | MouseButtons.Right)) != MouseButtons.None)
        {
            ResetHotCornerState();
            return;
        }

        if (area == HotCornerArea.None)
        {
            ResetHotCornerState();
            return;
        }

        if (area != _currentHotCornerArea)
        {
            _currentHotCornerArea = area;
            _hotCornerEnteredAtUtc = DateTime.UtcNow;
            _hotCornerTriggered = false;
            return;
        }

        if (_hotCornerTriggered)
        {
            return;
        }

        var delay = HotCornerActions.GetDelay(_settings, area);
        var actionId = GetCornerActionId(area);
        if (string.IsNullOrWhiteSpace(actionId) || string.Equals(actionId, "none", StringComparison.Ordinal))
        {
            ResetHotCornerState();
            return;
        }

        if (_settings.IgnoreFullScreen && FullScreenDetector.ShouldBlockAction(actionId))
        {
            if (DateTime.UtcNow - _lastFullScreenBlockedLogAtUtc > TimeSpan.FromSeconds(1))
            {
                AppLogger.Log($"Blocked action '{GetActionDisplayText(actionId)}' due to fullscreen foreground window");
                _lastFullScreenBlockedLogAtUtc = DateTime.UtcNow;
            }

            ResetHotCornerState();
            return;
        }

        var elapsed = DateTime.UtcNow - _hotCornerEnteredAtUtc;
        var remaining = delay - elapsed;

        if (delay > TimeSpan.Zero && _settings.ShowCountdown && remaining > TimeSpan.Zero)
        {
            _countdownOverlay.ShowCountdown(screen, area, remaining);
        }
        else
        {
            _countdownOverlay.HideCountdown();
        }

        if (DateTime.UtcNow - _hotCornerEnteredAtUtc < delay)
        {
            return;
        }

        _hotCornerTriggered = true;
        _latchedHotCornerArea = area;
        _latchedHotCornerBounds = screen.Bounds;
        _countdownOverlay.HideCountdown();
        AppLogger.Log($"Executing action '{GetActionDisplayText(actionId)}' from {area}");
        HotCornerActions.Execute(this, _settings, actionId);
    }

    private void ResetHotCornerState()
    {
        _countdownOverlay.HideCountdown();
        _currentHotCornerArea = HotCornerArea.None;
        _hotCornerEnteredAtUtc = DateTime.MinValue;
        _hotCornerTriggered = false;
    }

    private static bool IsWithinLatchedCornerReleaseZone(Point cursor, HotCornerArea area, Rectangle bounds, int zone)
    {
        if (bounds == Rectangle.Empty)
        {
            return false;
        }

        return area switch
        {
            HotCornerArea.TopLeft => cursor.X <= bounds.Left + zone - 1 && cursor.Y <= bounds.Top + zone - 1,
            HotCornerArea.TopRight => cursor.X >= bounds.Right - zone && cursor.Y <= bounds.Top + zone - 1,
            HotCornerArea.BottomLeft => cursor.X <= bounds.Left + zone - 1 && cursor.Y >= bounds.Bottom - zone,
            HotCornerArea.BottomRight => cursor.X >= bounds.Right - zone && cursor.Y >= bounds.Bottom - zone,
            _ => false
        };
    }

    private string GetCornerActionId(HotCornerArea area)
    {
        return area switch
        {
            HotCornerArea.TopLeft => _settings.TopLeftActionId,
            HotCornerArea.TopRight => _settings.TopRightActionId,
            HotCornerArea.BottomLeft => _settings.BottomLeftActionId,
            HotCornerArea.BottomRight => _settings.BottomRightActionId,
            _ => string.Empty
        };
    }

    private static HotCornerArea GetHotCornerArea(Point cursor, Rectangle bounds, int hotArea)
    {
        if (cursor.X >= bounds.Left && cursor.X < bounds.Left + hotArea &&
            cursor.Y >= bounds.Top && cursor.Y < bounds.Top + hotArea)
        {
            return HotCornerArea.TopLeft;
        }

        if (cursor.X > bounds.Right - hotArea - 1 && cursor.X <= bounds.Right - 1 &&
            cursor.Y >= bounds.Top && cursor.Y < bounds.Top + hotArea)
        {
            return HotCornerArea.TopRight;
        }

        if (cursor.X >= bounds.Left && cursor.X < bounds.Left + hotArea &&
            cursor.Y <= bounds.Bottom - 1 && cursor.Y > bounds.Bottom - hotArea - 1)
        {
            return HotCornerArea.BottomLeft;
        }

        if (cursor.X <= bounds.Right - 1 && cursor.X > bounds.Right - hotArea - 1 &&
            cursor.Y <= bounds.Bottom - 1 && cursor.Y > bounds.Bottom - hotArea - 1)
        {
            return HotCornerArea.BottomRight;
        }

        return HotCornerArea.None;
    }

    private static bool IsWithinCornerZone(Point cursor, Rectangle bounds, HotCornerArea area, int zone)
    {
        return area switch
        {
            HotCornerArea.TopLeft => cursor.X >= bounds.Left && cursor.X < bounds.Left + zone &&
                                     cursor.Y >= bounds.Top && cursor.Y < bounds.Top + zone,
            HotCornerArea.TopRight => cursor.X > bounds.Right - zone - 1 && cursor.X <= bounds.Right - 1 &&
                                      cursor.Y >= bounds.Top && cursor.Y < bounds.Top + zone,
            HotCornerArea.BottomLeft => cursor.X >= bounds.Left && cursor.X < bounds.Left + zone &&
                                        cursor.Y <= bounds.Bottom - 1 && cursor.Y > bounds.Bottom - zone - 1,
            HotCornerArea.BottomRight => cursor.X <= bounds.Right - 1 && cursor.X > bounds.Right - zone - 1 &&
                                         cursor.Y <= bounds.Bottom - 1 && cursor.Y > bounds.Bottom - zone - 1,
            _ => false
        };
    }

    private void OpenAdvancedDialog()
    {
        if (Visible)
        {
            BeginFadeOut();
        }

        if (_advancedWindow is not null && !_advancedWindow.IsDisposed)
        {
            if (_advancedWindow.WindowState == FormWindowState.Minimized)
            {
                _advancedWindow.WindowState = FormWindowState.Normal;
            }

            _advancedWindow.PositionNearTaskbarTray();
            _advancedWindow.Show();
            _advancedWindow.BringToFront();
            _advancedWindow.Activate();
            return;
        }

        var dialog = new AdvancedForm(_settings);
        _advancedWindow = dialog;
        dialog.FormClosed += (_, _) => _advancedWindow = null;
        dialog.SettingsApplied += appliedSettings =>
        {
            _settings = appliedSettings;
            ApplySettings();
        };
        dialog.PositionNearTaskbarTray();

        try
        {
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            _settings = dialog.Settings;
            ApplySettings();
        }
        finally
        {
            dialog.Dispose();
            if (ReferenceEquals(_advancedWindow, dialog))
            {
                _advancedWindow = null;
            }
        }
    }

    private bool IsSettingsWindowOpen()
    {
        return _advancedWindow is not null && !_advancedWindow.IsDisposed && _advancedWindow.Visible;
    }

    private void UpdateAutorunRegistration()
    {
        StartupRegistration.SetEnabled(_settings.StartWithWindows);
    }

    private void UpdateTrayIcon()
    {
        AppLogger.Log($"UpdateTrayIcon called. HotCornersEnabled: {_settings.HotCornersEnabled}");
        if (!IsHandleCreated || Disposing || IsDisposed)
        {
            ApplyTrayIcon();
            return;
        }

        if (_trayIconRefreshQueued)
        {
            return;
        }

        _trayIconRefreshQueued = true;
        BeginInvoke(new Action(() =>
        {
            _trayIconRefreshQueued = false;

            if (IsDisposed || Disposing)
            {
                return;
            }

            ApplyTrayIcon();
        }));
    }

    private void ApplyTrayIcon()
    {
        var loadedIcon = WindowIconLoader.TryLoadTrayIcon(ThemeHelper.IsLightTheme, _settings.HotCornersEnabled);
        var logMessage = $"Loaded icon: {(loadedIcon != null ? "Success" : "Fallback")}, Theme: {(ThemeHelper.IsLightTheme ? "Light" : "Dark")}, HotCornersEnabled: {_settings.HotCornersEnabled}";
        File.AppendAllText("tray_icon_debug.log", logMessage + Environment.NewLine);
        AppLogger.Log(logMessage);
        loadedIcon ??= WindowIconLoader.LoadFallbackTrayIcon();

        var wasVisible = _notifyIcon.Visible;
        var previousIcon = _currentTrayIcon;
        _currentTrayIcon = loadedIcon;

        // Explicitly reset the NotifyIcon properties to force a refresh
        _notifyIcon.Visible = false;
        _notifyIcon.Icon = null;
        _notifyIcon.Icon = loadedIcon;
        _notifyIcon.Visible = wasVisible;

        previousIcon?.Dispose();
    }

    private void HideFlyoutIfNeeded()
    {
        if (!_isCornerMenuOpen)
        {
            _lastDeactivateCloseAtUtc = DateTime.UtcNow;
            BeginFadeOut();
        }
    }

    private void ShowAbout()
    {
        using var dialog = new AboutForm();
        dialog.ShowDialog(this);
    }

    private void HideTrayIcon()
    {
        _trayHiddenByUser = true;
        _notifyIcon.Visible = false;
        AppLogger.Log("Tray icon hidden. Restart the application to restore it.");
    }

    private void ElevateApplication()
    {
        if (ElevationHelper.IsProcessElevated())
        {
            return;
        }

        if (ElevationHelper.TryRestartElevated())
        {
            ExitApplication();
        }
        else
        {
            AppLogger.Log("Elevation was canceled or failed to start.");
        }
    }

    private void UpdateTrayIconVisibility()
    {
        _notifyIcon.Visible = !_settings.AlwaysHideTrayIcon && !_trayHiddenByUser;
    }

    private void ShowLogWindowPlaceholder()
    {
        if (_logWindow is null || _logWindow.IsDisposed)
        {
            _logWindow = new LogWindowForm();
        }

        _logWindow.Show();
        _logWindow.BringToFront();
    }

    private void OnLogEntryAdded(string entry)
    {
        if (_logWindow is null || _logWindow.IsDisposed)
        {
            return;
        }

        if (_logWindow.InvokeRequired)
        {
            _logWindow.BeginInvoke(new Action(() => _logWindow.AppendLog(entry)));
            return;
        }

        _logWindow.AppendLog(entry);
    }

    private void ExitApplication()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        Application.Exit();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        AppLogger.EntryAdded -= OnLogEntryAdded;
        SystemEvents.UserPreferenceChanged -= SystemEventsOnUserPreferenceChanged;
        _settingsSaveTimer.Stop();
        SettingsStore.Save(_settings);
        _globalMouseHook.MouseMoved -= GlobalMouseHookOnMouseMoved;
        _globalMouseHook.Dispose();
        _fadeTimer.Stop();
        _hotCornerTimer.Stop();
        _countdownOverlay.HideCountdown();
        _countdownOverlay.Dispose();
        _advancedWindow?.Close();
        _logWindow?.Close();
        _notifyIcon.Visible = false;
        _currentTrayIcon?.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputDevice
    {
        internal ushort UsagePage;
        internal ushort Usage;
        internal uint Flags;
        internal IntPtr Target;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices(RawInputDevice[] rawInputDevices, uint numberOfDevices, uint sizeOfRawInputDevice);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("winmm.dll")]
    private static extern uint timeBeginPeriod(uint uPeriod);

    [DllImport("winmm.dll")]
    private static extern uint timeEndPeriod(uint uPeriod);
}
