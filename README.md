# WinXCornersPlus

## <a name="winxcornersplus"></a> WinXCornersPlus
WinXCornersPlus is a lightweight utility for Windows 10 and Windows 11 that enhances your desktop experience by allowing you to assign custom actions triggered when you hover your mouse cursor over the corners of your main monitor. Whether you're a power user, developer, or just someone who appreciates efficiency, WinXCornersPlus provides a seamless way to streamline your workflow.

## <a name="features"></a> Key Features <a href="#winxcornersplus"></a>

1. **Corner Actions**: Choose from a variety of predefined actions for each corner:
    - **Action Center**: Open the Action Center.
    - **File Explorer**: Open File Explorer window.
    - **Hide Other Windows**: Minimize all windows and keep the currently focused window in view.
    - **Lock Screen**: Lock your screen.
    - **Notification Center**: Open the Notification Center.
    - **Settings**: Open System Settings.
    - **Show All Windows**: Activate Windows Task View to manage your open applications.
    - **Show Desktop**: Minimize all windows to show the desktop.
    - **Start Menu**: Open the Start Menu.
    - **Start Screen Saver**: Trigger your screen saver for privacy or energy-saving purposes.
    - **Task Manager**: Open Task Manager.
    - **Turn Display Off**: Turn off your display when not in use.
    - **Custom**: Open other executables with command line params or custom hotkeys (sequence of key hold/release).

2. **Customization**: Tailor WinXCornersPlus to your preferences:
    - Assign specific actions to individual screen corners.
    - Configure hover sensitivity and activation delay.
    - Enable or disable application startup with Windows.
    - Set flyout animation direction.
    - Hide the tray icon persistently or run the application with elevated privileges.
    - Suppress actions while in full-screen mode.

3. **System Tray Integration**: WinXCornersPlus runs discreetly in the system tray, ensuring it doesn't clutter your desktop or Taskbar.

4. **Unobstrusive**: Its usage won't interfere with your common tasks, unless you decide to do so.
    - It won't trigger actions while dragging content with your mouse.
    - It won't trigger while using a **Full Screen application**, like games or media, for instance.
    - You can disable it temporarily right from the popup window with the switch toggle.
    - You can hide it from the taskbar tray while keeping it running in the background. 

5. **Visible Countdown Counter**: Helps you, visually, to know if a corner action is about to be triggered.

6. `[WIP]`~~**Windows 10/11 Theme aware**: Support for Windows 10 and 11 dark and light theme, so it will look like part of your OS.~~

## <a name="howto"></a> How To Use  <a href="#WinXCornersPlus"></a>
1. Launch WinXCornersPlus popup window from the system tray icon.
2. Configure your preferred actions for each corner.
3. Hover your mouse cursor over a corner to trigger the assigned action.
4. Right click the WinXCornersPlus tray icon and select *Settings* to open the more advanced options.

## <a name="howtohotkey"></a> Custom Hotkeys  <a href="#WinXCornersPlus"></a>
The hotkeys will be as follows:
`_control` or `control` or `control_` where `_` means hold or release (prefixed, appended) and without it, a full key press. This will be useful if you have a sequence of hotkeys to do, like `_control+k+control_+_control+_b` for VSCode for instance, that will do a `ctrl+k` then `ctrl+b` to toggle the sidebar.

There is more, it will check for windows on foreground/currently focused, or globally, whether by only its classname or with titlebar text too. The conditional pseudo script will be as follows:
```
! = follows sequence of hotkeys as mentioned above
# = follows [classname,title] there title is optional to match with current focused window
@ = follows [classname,title] there title is optional to match with any opened window
```

### Rule:

`#[classname,title]:(sequence of hotkeys)?(optional sequence of hotkeys in case condition is not met)`

For instance the following will check if current window is VSCode's and will invoke `ctrl+k` `ctrl+b` sequence of hotkeys, other wise if not on VSCode, just invoke the Start Menu.

`#[Chrome_WidgetWin_1]:(_control+k+control_+_control+_b)?(win)`

E.g. `#[conditional match]:(hotkey if match)?(hotkey if not)`

Another example for Windows 10: 

This will check if Alt+Tab's window is visible, if so, it will hide it, otherwise it will invoke it, as a faster alternative to Task View.
`#[MultitaskingViewFrame]:(escape)?(_control+_alt+tab)`

## Notes
- WinXCornersPlus works seamlessly on your primary monitor but secondary monitors haven't been tested throughfully, consider it partially supported.
- If you encounter issues with elevated privileges software, try restarting WinXCornersPlus as an administrator specially if you use those kind of elevated privileged software most of the time, otherwise triggering won't work due to the nature of separate privileges. Use *Elevate* option in tray context menu to restart the application into Administrator mode. Go to Settings and activate *Elevated* mode permanently in Advanced tab.
- If you encounter other unknown issues, please fill a bug report at the GitHub issues page.