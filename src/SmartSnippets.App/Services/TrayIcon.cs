using System.Drawing;
using System.Runtime.InteropServices;
using SmartSnippets.Core.Logging;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace SmartSnippets.App.Services;

internal sealed unsafe class TrayIcon : IDisposable
{
    private const uint CallbackMessage = PInvoke.WM_APP + 1;
    private const uint IconId = 1;
    private const nuint CommandOpen = 1;
    private const nuint CommandSettings = 2;
    private const nuint CommandRestartAdmin = 3;
    private const nuint CommandExit = 5;
    private const nuint CommandShowPopup = 6;
    private const string Tooltip = "Smart Snippets — running in background";
    private const string TooltipHotkeyUnavailable = "Smart Snippets — hotkey unavailable";

    private readonly MessageWindow _window;
    private readonly Action _onOpen;
    private readonly Action<HWND> _onShowPopup;
    private readonly Action _onSettings;
    private readonly Action _onRestartAdmin;
    private readonly Action _onExit;
    private readonly bool _showRestartAdmin;
    private readonly Func<string?> _hotkeyDisplay;
    private readonly uint _taskbarCreatedMessage;
    private bool _added;
    private bool _lightTheme;
    private uint _dpi;
    private readonly Dictionary<(bool Light, TrayState State), HICON> _icons = [];
    private TrayState _state = TrayState.Ready;

    public TrayIcon(
        MessageWindow window,
        Action onOpen,
        Action<HWND> onShowPopup,
        Action onSettings,
        Action onRestartAdmin,
        Action onExit,
        bool showRestartAdmin,
        Func<string?> hotkeyDisplay)
    {
        _window = window;
        _onOpen = onOpen;
        _onShowPopup = onShowPopup;
        _onSettings = onSettings;
        _onRestartAdmin = onRestartAdmin;
        _onExit = onExit;
        _showRestartAdmin = showRestartAdmin;
        _hotkeyDisplay = hotkeyDisplay;
        RefreshThemeAndDpi();

        fixed (char* name = "TaskbarCreated")
        {
            _taskbarCreatedMessage = PInvoke.RegisterWindowMessage(name);
        }

        // An elevated process drops messages from the (medium-integrity) shell unless they are explicitly allowed.
        if (_taskbarCreatedMessage != 0)
        {
            PInvoke.ChangeWindowMessageFilterEx(_window.Handle, _taskbarCreatedMessage, WINDOW_MESSAGE_FILTER_ACTION.MSGFLT_ALLOW, null);
        }

        PInvoke.ChangeWindowMessageFilterEx(_window.Handle, 0x001A, WINDOW_MESSAGE_FILTER_ACTION.MSGFLT_ALLOW, null);
        _window.MessageReceived += OnMessage;
    }

    public TrayState State
    {
        get => _state;
        set
        {
            if (_state == value)
            {
                return;
            }

            _state = value;
            UpdateIcon();
        }
    }

    public bool Add()
    {
        var data = CreateData();
        data.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP;
        data.uCallbackMessage = CallbackMessage;
        data.hIcon = GetIcon();
        data.szTip = TooltipFor(_state);
        _added = PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, in data);
        AppLog.Info(_added ? "tray_added" : "tray_add_failed");
        LogIcon();
        return _added;
    }

    private static string TooltipFor(TrayState state) =>
        state == TrayState.HotkeyUnavailable ? TooltipHotkeyUnavailable : Tooltip;

    private HICON GetIcon()
    {
        var key = (_lightTheme, _state);
        if (_icons.TryGetValue(key, out var cached)) return cached;
        var name = $"Tray-{(_lightTheme ? "Light" : "Dark")}-{_state}.ico";
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", name);
        var width = TrayIconNative.GetSystemMetricsForDpi(49, _dpi); // SM_CXSMICON
        var height = TrayIconNative.GetSystemMetricsForDpi(50, _dpi); // SM_CYSMICON
        var handle = TrayIconNative.LoadImage(0, path, 1, width, height, 0x10); // IMAGE_ICON, LR_LOADFROMFILE
        if (handle == 0)
        {
            AppLog.Info("tray_icon_load_failed", $"name={name}");
            return PInvoke.LoadIcon(HINSTANCE.Null, PInvoke.IDI_APPLICATION);
        }
        var icon = new HICON(handle);
        _icons.Add(key, icon);
        return icon;
    }

    private void DestroyCachedIcons()
    {
        foreach (var icon in _icons.Values) TrayIconNative.DestroyIcon((nint)icon.Value);
        _icons.Clear();
    }

    private void RefreshThemeAndDpi()
    {
        _lightTheme = TaskbarTheme.IsLight();
        _dpi = TrayIconNative.GetDpiForSystem();
        if (_dpi == 0) _dpi = 96;
    }

    private void LogIcon() => AppLog.Info("tray_icon", $"state={_state} theme={(_lightTheme ? "light" : "dark")}");

    private void UpdateIcon()
    {
        if (!_added) return;
        var data = CreateData();
        data.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP;
        data.hIcon = GetIcon();
        data.szTip = TooltipFor(_state);
        if (!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, in data)) AppLog.Info("tray_icon_update_failed");
        LogIcon();
    }

    /// <summary>Shows a tray balloon notification (title up to 63 and text up to 255 characters).</summary>
    public void ShowBalloon(string title, string text)
    {
        if (!_added)
        {
            return;
        }

        var data = CreateData();
        data.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_INFO;
        data.szInfoTitle = Truncate(title, 63);
        data.szInfo = Truncate(text, 255);
        data.dwInfoFlags = NOTIFY_ICON_INFOTIP_FLAGS.NIIF_INFO;
        if (!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, in data))
        {
            AppLog.Info("tray_balloon_failed");
        }
    }

    public void Dispose()
    {
        _window.MessageReceived -= OnMessage;
        if (_added)
        {
            var data = CreateData();
            PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, in data);
            _added = false;
        }
        DestroyCachedIcons();
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    private NOTIFYICONDATAW CreateData()
    {
        var data = default(NOTIFYICONDATAW);
        data.cbSize = (uint)sizeof(NOTIFYICONDATAW);
        data.hWnd = _window.Handle;
        data.uID = IconId;
        return data;
    }

    private void OnMessage(uint message, nuint wParam, nint lParam)
    {
        if (_taskbarCreatedMessage != 0 && message == _taskbarCreatedMessage)
        {
            // Explorer restarted: its tray no longer holds our icon.
            _added = false;
            DestroyCachedIcons();
            RefreshThemeAndDpi();
            if (Add())
            {
                AppLog.Info("tray_readded");
            }

            return;
        }

        if (message == 0x001A && lParam != 0 && Marshal.PtrToStringUni(lParam) == "ImmersiveColorSet")
        {
            var light = TaskbarTheme.IsLight();
            if (light != _lightTheme)
            {
                DestroyCachedIcons();
                RefreshThemeAndDpi();
                AppLog.Info("tray_theme_changed", $"theme={(_lightTheme ? "light" : "dark")}");
            }
            UpdateIcon();
            return;
        }

        if (message != CallbackMessage)
        {
            return;
        }

        switch ((uint)lParam & 0xFFFF)
        {
            case PInvoke.WM_LBUTTONUP:
                _onOpen();
                break;
            case PInvoke.WM_RBUTTONUP:
            case PInvoke.WM_CONTEXTMENU:
                ShowMenu();
                break;
        }
    }

    private void ShowMenu()
    {
        Point cursor;
        PInvoke.GetCursorPos(&cursor);

        var menu = PInvoke.CreatePopupMenu();
        if (menu.IsNull)
        {
            return;
        }

        try
        {
            var hotkey = _hotkeyDisplay();
            var showPopupText = string.IsNullOrEmpty(hotkey)
                ? UiText.TrayShowPopup
                : UiText.TrayShowPopup + "\t" + hotkey;

            fixed (char* open = UiText.TrayOpen)
            fixed (char* showPopup = showPopupText)
            fixed (char* settings = UiText.TraySettings)
            fixed (char* restart = "Restart as administrator")
            fixed (char* exit = UiText.TrayExit)
            {
                PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_STRING, CommandOpen, open);
                PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_STRING, CommandShowPopup, showPopup);
                PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_STRING, CommandSettings, settings);
                PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_SEPARATOR, 0, null);
                if (_showRestartAdmin)
                {
                    PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_STRING, CommandRestartAdmin, restart);
                    PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_SEPARATOR, 0, null);
                }
                PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_STRING, CommandExit, exit);
            }

            var previousForeground = PInvoke.GetForegroundWindow();
            // Documented quirk: the menu only dismisses on outside click if our window is foreground.
            PInvoke.SetForegroundWindow(_window.Handle);
            var flags = (uint)(TRACK_POPUP_MENU_FLAGS.TPM_RIGHTBUTTON | TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD
                | TRACK_POPUP_MENU_FLAGS.TPM_BOTTOMALIGN | TRACK_POPUP_MENU_FLAGS.TPM_LEFTALIGN);
            var command = (nuint)PInvoke.TrackPopupMenuEx(menu, flags, cursor.X, cursor.Y, _window.Handle, null).Value;
            PInvoke.PostMessage(_window.Handle, PInvoke.WM_NULL, 0, 0);

            switch (command)
            {
                case CommandOpen:
                    _onOpen();
                    break;
                case CommandShowPopup:
                    // Capture the selected window directly, without racing a foreground switch.
                    _onShowPopup(PreviousAppWindow(previousForeground));
                    break;
                case CommandSettings:
                    _onSettings();
                    break;
                case CommandRestartAdmin:
                    _onRestartAdmin();
                    break;
                case CommandExit:
                    _onExit();
                    break;
            }
        }
        finally
        {
            PInvoke.DestroyMenu(menu);
        }
    }

    /// <summary>
    /// The window the user was working in before opening the tray menu: the foreground window if it is an
    /// app window, otherwise the first app window in Z-order.
    /// </summary>
    private static HWND PreviousAppWindow(HWND foreground)
    {
        if (IsAppWindow(foreground))
        {
            return foreground;
        }

        for (var hwnd = PInvoke.GetTopWindow(default); !hwnd.IsNull; hwnd = PInvoke.GetWindow(hwnd, GET_WINDOW_CMD.GW_HWNDNEXT))
        {
            if (IsAppWindow(hwnd))
            {
                return hwnd;
            }
        }

        return default;
    }

    /// <summary>Visible, not minimized, unowned, not ours, and not part of the taskbar or desktop.</summary>
    private static bool IsAppWindow(HWND hwnd)
    {
        if (hwnd.IsNull || !PInvoke.IsWindowVisible(hwnd) || PInvoke.IsIconic(hwnd)
            || !PInvoke.GetWindow(hwnd, GET_WINDOW_CMD.GW_OWNER).IsNull)
        {
            return false;
        }

        PInvoke.GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == 0 || pid == PInvoke.GetCurrentProcessId())
        {
            return false;
        }

        var buffer = stackalloc char[64];
        var length = PInvoke.GetClassName(hwnd, buffer, 64);
        var className = length > 0 ? new string(buffer, 0, length) : string.Empty;
        return className is not ("Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "NotifyIconOverflowWindow"
            or "TopLevelWindowForOverflowXamlIsland" or "Progman" or "WorkerW");
    }
}
